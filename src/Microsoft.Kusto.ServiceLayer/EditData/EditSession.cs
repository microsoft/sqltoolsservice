//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility.SqlScriptFormatters;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    /// <summary>
    /// Represents an edit "session" bound to the results of a query, containing a cache of edits
    /// that are pending. Provides logic for performing edit operations.
    /// </summary>
    public class EditSession
    {

        private ResultSet associatedResultSet;

        private readonly IEditMetadataFactory metadataFactory;
        private EditTableMetadata objectMetadata;

        /// <summary>
        /// Constructs a new edit session bound to the result set and metadat object provided
        /// </summary>
        /// <param name="metaFactory">Factory for creating metadata</param>
        public EditSession(IEditMetadataFactory metaFactory)
        {
            Validate.IsNotNull(nameof(metaFactory), metaFactory);

            // Setup the internal state
            metadataFactory = metaFactory;
        }

        #region Properties

        public delegate Task<DbConnection> Connector();

        public delegate Task<EditSessionQueryExecutionState> QueryRunner(string query);

        /// <summary>
        /// The task that is running to commit the changes to the db
        /// Internal for unit test purposes.
        /// </summary>
        internal Task CommitTask { get; set; }

        /// <summary>
        /// The internal ID for the next row in the table. Internal for unit testing purposes only.
        /// </summary>
        internal long NextRowId { get; private set; }

        /// <summary>
        /// The cache of pending updates. Internal for unit test purposes only
        /// </summary>
        internal ConcurrentDictionary<long, RowEditBase> EditCache { get; private set; }

        /// <summary>
        /// The task that is running to initialize the edit session
        /// </summary>
        internal Task InitializeTask { get; set; }

        /// <summary>
        /// Whether or not the session has been initialized
        /// </summary>
        public bool IsInitialized { get; internal set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the edit session, asynchronously, by retrieving metadata about the table to
        /// edit and querying the table for the rows of the table.
        /// </summary>
        /// <param name="initParams">Parameters for initializing the edit session</param>
        /// <param name="connector">Delegate that will return a DbConnection when executed</param>
        /// <param name="queryRunner">
        /// Delegate that will run the requested query and return a
        /// <see cref="EditSessionQueryExecutionState"/> object on execution
        /// </param>
        /// <param name="successHandler">Func to call when initialization has completed successfully</param>
        /// <param name="errorHandler">Func to call when initialization has completed with errors</param>
        /// <exception cref="InvalidOperationException">
        /// When session is already initialized or in progress of initializing
        /// </exception>
        public void Initialize(EditInitializeParams initParams, Connector connector, QueryRunner queryRunner, Func<Task> successHandler, Func<Exception, Task> errorHandler)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException(SR.EditDataSessionAlreadyInitialized);
            }

            if (InitializeTask != null)
            {
                throw new InvalidOperationException(SR.EditDataSessionAlreadyInitializing);
            }

            Validate.IsNotNullOrWhitespaceString(nameof(initParams.ObjectName), initParams.ObjectName);
            Validate.IsNotNullOrWhitespaceString(nameof(initParams.ObjectType), initParams.ObjectType);
            Validate.IsNotNull(nameof(initParams.Filters), initParams.Filters);

            Validate.IsNotNull(nameof(connector), connector);
            Validate.IsNotNull(nameof(queryRunner), queryRunner);
            Validate.IsNotNull(nameof(successHandler), successHandler);
            Validate.IsNotNull(nameof(errorHandler), errorHandler);

            // Start up the initialize process
            InitializeTask = InitializeInternal(initParams, connector, queryRunner, successHandler, errorHandler);
        }

        /// <summary>
        /// Validates that a query can be used for an edit session. The target result set is returned
        /// </summary>
        /// <param name="query">The query to validate</param>
        /// <returns>The result set to use</returns>
        public static ResultSet ValidateQueryForSession(Query query)
        {
            Validate.IsNotNull(nameof(query), query);

            // Determine if the query is valid for editing
            // Criterion 1) Query has finished executing
            if (!query.HasExecuted)
            {
                throw new InvalidOperationException(SR.EditDataQueryNotCompleted);
            }

            // Criterion 2) Query only has a single result set
            ResultSet[] queryResultSets = query.Batches.SelectMany(b => b.ResultSets).ToArray();
            if (queryResultSets.Length != 1)
            {
                throw new InvalidOperationException(SR.EditDataQueryImproperResultSets);
            }

            return query.Batches[0].ResultSets[0];
        }

        /// <summary>
        /// If the results contain any results that conflict with the table metadata, then
        /// make all columns readonly so that the user cannot make an invalid update.
        /// </summary>
        public static void CheckResultsForInvalidColumns(ResultSet results, string tableName)
        {
            if (SchemaContainsMultipleItems(results.Columns, col => col.BaseCatalogName)
                || SchemaContainsMultipleItems(results.Columns, col => col.BaseSchemaName)
                || SchemaContainsMultipleItems(results.Columns, col => col.BaseTableName))
            {
                throw new InvalidOperationException(SR.EditDataMultiTableNotSupported);
            }

            // Check if any of the columns are invalid
            HashSet<string> colNameTracker = new HashSet<string>();
            foreach (DbColumnWrapper col in results.Columns)
            {
                if (col.IsAliased.HasTrue())
                {
                    throw new InvalidOperationException(SR.EditDataAliasesNotSupported);
                }

                // We have changed HierarchyId column to an expression so that it can be displayed properly
                if (!col.IsHierarchyId && col.IsExpression.HasTrue())
                {
                    throw new InvalidOperationException(SR.EditDataExpressionsNotSupported);
                }

                if (colNameTracker.Contains(col.ColumnName))
                {
                    throw new InvalidOperationException(SR.EditDataDuplicateColumnsNotSupported);
                }
                else
                {
                    colNameTracker.Add(col.ColumnName);
                }
            }

            // Only one source table in the metadata, but check if results are from the original table.
            if (results.Columns.Length > 0)
            {
                string resultTableName = results.Columns[0].BaseTableName;
                if (!string.IsNullOrEmpty(resultTableName) && !string.Equals(resultTableName, tableName))
                {
                    throw new InvalidOperationException(SR.EditDataIncorrectTable(tableName));
                }
            }
        }

        private static bool SchemaContainsMultipleItems(DbColumn[] columns, Func<DbColumn, string> filter)
        {
            return columns
                .Select(column => filter(column))
                .Where(name => name != null)
                .ToHashSet().Count > 1;
        }

        /// <summary>
        /// Creates a new row update and adds it to the update cache
        /// </summary>
        /// <exception cref="InvalidOperationException">If inserting into cache fails</exception>
        /// <returns>The internal ID of the newly created row</returns>
        public EditCreateRowResult CreateRow()
        {
            ThrowIfNotInitialized();

            // Create a new row ID (atomically, since this could be accesses concurrently)
            long newRowId = NextRowId++;

            // Create a new row create update and add to the update cache
            RowCreate newRow = new RowCreate(newRowId, associatedResultSet, objectMetadata);
            if (!EditCache.TryAdd(newRowId, newRow))
            {
                // Revert the next row ID
                NextRowId--;
                throw new InvalidOperationException(SR.EditDataFailedAddRow);
            }

            EditCreateRowResult output = new EditCreateRowResult
            {
                NewRowId = newRow.RowId,
                DefaultValues = newRow.DefaultValues
            };
            return output;
        }

        /// <summary>
        /// Commits the edits in the cache to the database and then to the associated result set of
        /// this edit session. This is launched asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use for executing the query</param>
        /// <param name="successHandler">Callback to perform when the commit process has finished</param>
        /// <param name="errorHandler">Callback to perform if the commit process has failed at some point</param>
        public void CommitEdits(DbConnection connection, Func<Task> successHandler, Func<Exception, Task> errorHandler)
        {
            ThrowIfNotInitialized();

            Validate.IsNotNull(nameof(connection), connection);
            Validate.IsNotNull(nameof(successHandler), successHandler);
            Validate.IsNotNull(nameof(errorHandler), errorHandler);

            // Make sure that there isn't a commit task in progress
            if (CommitTask != null && !CommitTask.IsCompleted)
            {
                throw new InvalidOperationException(SR.EditDataCommitInProgress);
            }

            // Start up the commit process
            CommitTask = CommitEditsInternal(connection, successHandler, errorHandler);
        }

        /// <summary>
        /// Creates a delete row update and adds it to the update cache
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If row requested to delete already has a pending change in the cache
        /// </exception>
        /// <param name="rowId">The internal ID of the row to delete</param>
        public void DeleteRow(long rowId)
        {
            ThrowIfNotInitialized();

            // Sanity check the row ID
            if (rowId >= NextRowId || rowId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowId), SR.EditDataRowOutOfRange);
            }

            // Create a new row delete update and add to cache
            RowDelete deleteRow = new RowDelete(rowId, associatedResultSet, objectMetadata);
            if (!EditCache.TryAdd(rowId, deleteRow))
            {
                throw new InvalidOperationException(SR.EditDataUpdatePending);
            }
        }

        /// <summary>
        /// Retrieves a subset of rows with the pending updates applied. If more rows than exist
        /// are requested, only the rows that exist will be returned.
        /// </summary>
        /// <param name="startIndex">Index to start returning rows from</param>
        /// <param name="rowCount">The number of rows to return.</param>
        /// <returns>An array of rows with pending edits applied</returns>
        public async Task<EditRow[]> GetRows(long startIndex, int rowCount)
        {
            ThrowIfNotInitialized();

            // Get the cached rows from the result set
            ResultSetSubset cachedRows = startIndex < associatedResultSet.RowCount
                ? await associatedResultSet.GetSubset(startIndex, rowCount)
                : new ResultSetSubset
                {
                    RowCount = 0,
                    Rows = new DbCellValue[][] { }
                };

            // Convert the rows into EditRows and apply the changes we have
            List<EditRow> editRows = new List<EditRow>();
            for (int i = 0; i < cachedRows.RowCount; i++)
            {
                long rowId = i + startIndex;
                RowEditBase edr;
                if (EditCache.TryGetValue(rowId, out edr))
                {
                    // Ask the edit object to generate an edit row
                    editRows.Add(edr.GetEditRow(cachedRows.Rows[i]));
                }
                else
                {
                    // Package up the existing row into a clean edit row
                    EditRow er = new EditRow
                    {
                        Id = rowId,
                        Cells = cachedRows.Rows[i].Select(cell => new EditCell(cell, false)).ToArray(),
                        State = EditRow.EditRowState.Clean
                    };
                    editRows.Add(er);
                }
            }

            // If the requested range of rows was at the end of the original cell set and we have
            // added new rows, we need to reflect those changes
            if (rowCount > cachedRows.RowCount)
            {
                long endIndex = startIndex + cachedRows.RowCount;
                var newRows = EditCache.Where(edit => edit.Key >= endIndex).Take(rowCount - cachedRows.RowCount);
                editRows.AddRange(newRows.Select(newRow => newRow.Value.GetEditRow(null)));
            }

            return editRows.ToArray();
        }

        /// <summary>
        /// Reverts a cell in a pending edit
        /// </summary>
        /// <param name="rowId">Internal ID of the row to have its edits reverted</param>
        /// <param name="columnId">Ordinal ID of the column to revert</param>
        /// <returns>String version of the old value for the cell</returns>
        public EditRevertCellResult RevertCell(long rowId, int columnId)
        {
            ThrowIfNotInitialized();

            // Attempt to get the row edit with the given ID
            RowEditBase pendingEdit;
            if (!EditCache.TryGetValue(rowId, out pendingEdit))
            {
                throw new ArgumentOutOfRangeException(nameof(rowId), SR.EditDataUpdateNotPending);
            }

            // Update the row
            EditRevertCellResult revertResult = pendingEdit.RevertCell(columnId);
            CleanupEditIfRowClean(rowId, revertResult);

            // Have the edit base revert the cell
            return revertResult;
        }

        /// <summary>
        /// Removes a pending row update from the update cache.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If a pending row update with the given row ID does not exist.
        /// </exception>
        /// <param name="rowId">The internal ID of the row to reset</param>
        public void RevertRow(long rowId)
        {
            ThrowIfNotInitialized();

            // Attempt to remove the row with the given ID
            RowEditBase removedEdit;
            if (!EditCache.TryRemove(rowId, out removedEdit))
            {
                throw new ArgumentOutOfRangeException(nameof(rowId), SR.EditDataUpdateNotPending);
            }
        }

        /// <summary>
        /// Generates a single script file with all the pending edits scripted.
        /// </summary>
        /// <param name="outputPath">The path to output the script to</param>
        /// <returns></returns>
        public string ScriptEdits(string outputPath)
        {
            ThrowIfNotInitialized();

            // Validate the output path
            // @TODO: Reinstate this code once we have an interface around file generation
            //if (outputPath == null)
            //{
            //    // If output path isn't provided, we'll use a temporary location
            //    outputPath = Path.GetTempFileName();
            //}
            //else 
            if (outputPath == null || outputPath.Trim() == string.Empty)
            {
                // If output path is empty, that's an error
                throw new ArgumentNullException(nameof(outputPath), SR.EditDataScriptFilePathNull);
            }

            // Open a handle to the output file
            using (FileStream outputStream = File.OpenWrite(outputPath))
            using (TextWriter outputWriter = new StreamWriter(outputStream))
            {

                // Convert each update in the cache into an insert/update/delete statement
                foreach (RowEditBase rowEdit in EditCache.Values)
                {
                    outputWriter.WriteLine(rowEdit.GetScript());
                }
            }

            // Return the location of the generated script
            return outputPath;
        }

        /// <summary>
        /// Performs an update to a specific cell in a row. If the row has not already been
        /// initialized with a record in the update cache, one is created.
        /// </summary>
        /// <exception cref="InvalidOperationException">If adding a new update row fails</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the row that is requested to be edited is beyond the rows in the results and the
        /// rows that are being added.
        /// </exception>
        /// <param name="rowId">The internal ID of the row to edit</param>
        /// <param name="columnId">The ordinal of the column to edit in the row</param>
        /// <param name="newValue">The new string value of the cell to update</param>
        public EditUpdateCellResult UpdateCell(long rowId, int columnId, string newValue)
        {
            ThrowIfNotInitialized();

            // Sanity check to make sure that the row ID is in the range of possible values
            if (rowId >= NextRowId || rowId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowId), SR.EditDataRowOutOfRange);
            }

            // Attempt to get the row that is being edited, create a new update object if one
            // doesn't exist
            // NOTE: This *must* be done as a lambda. RowUpdate creation requires that the row
            // exist in the result set. We only want a new RowUpdate to be created if the edit
            // doesn't already exist in the cache
            RowEditBase editRow = EditCache.GetOrAdd(rowId, key => new RowUpdate(rowId, associatedResultSet, objectMetadata));

            // Update the row
            EditUpdateCellResult result = editRow.SetCell(columnId, newValue);
            CleanupEditIfRowClean(rowId, result);

            return result;
        }

        #endregion

        #region Private Helpers

        private async Task InitializeInternal(EditInitializeParams initParams, Connector connector,
            QueryRunner queryRunner, Func<Task> successHandler, Func<Exception, Task> failureHandler)
        {
            try
            {
                // Step 1) Look up the SMO metadata
                string[] namedParts = GetEditTargetName(initParams);
                objectMetadata = metadataFactory.GetObjectMetadata(await connector(), namedParts,
                    initParams.ObjectType);

                // Step 2) Get and execute a query for the rows in the object we're looking up
                EditSessionQueryExecutionState state = await queryRunner(initParams.QueryString ?? ConstructInitializeQuery(objectMetadata, initParams.Filters));
                if (state.Query == null)
                {
                    string message = state.Message ?? SR.EditDataQueryFailed;
                    throw new Exception(message);
                }

                // Step 3) Setup the internal state
                associatedResultSet = ValidateQueryForSession(state.Query);
                UpdateColumnInformationWithMetadata(associatedResultSet.Columns);
                CheckResultsForInvalidColumns(associatedResultSet, initParams.ObjectName);

                NextRowId = associatedResultSet.RowCount;
                EditCache = new ConcurrentDictionary<long, RowEditBase>();
                IsInitialized = true;
                objectMetadata.Extend(associatedResultSet.Columns);

                // Step 4) Return our success
                await successHandler();
            }
            catch (Exception e)
            {
                await failureHandler(e);
            }
        }

        public static string[] GetEditTargetName(EditInitializeParams initParams)
        {
            return initParams.SchemaName != null
                ? new[] { initParams.SchemaName, initParams.ObjectName }
                : FromSqlScript.DecodeMultipartIdentifier(initParams.ObjectName);
        }

        private async Task CommitEditsInternal(DbConnection connection, Func<Task> successHandler, Func<Exception, Task> errorHandler)
        {
            try
            {
                // @TODO: Add support for transactional commits

                // Trust the RowEdit to sort itself appropriately
                var editOperations = EditCache.Values.ToList();
                editOperations.Sort();
                foreach (var editOperation in editOperations)
                {
                    // Get the command from the edit operation and execute it
                    using (DbCommand editCommand = editOperation.GetCommand(connection))
                    using (DbDataReader reader = await editCommand.ExecuteReaderAsync())
                    {
                        // Apply the changes of the command to the result set
                        await editOperation.ApplyChanges(reader);
                    }

                    // If we succeeded in applying the changes, then remove this from the cache
                    // @TODO: Prevent edit sessions from being modified while a commit is in progress
                    RowEditBase re;
                    EditCache.TryRemove(editOperation.RowId, out re);
                }
                await successHandler();
            }
            catch (Exception e)
            {
                await errorHandler(e);
            }
        }

        /// <summary>
        /// Constructs a query for selecting rows in a table based on the filters provided.
        /// Internal for unit testing purposes only.
        /// </summary>
        internal static string ConstructInitializeQuery(EditTableMetadata metadata, EditInitializeFiltering initFilters)
        {
            StringBuilder queryBuilder = new StringBuilder("SELECT ");

            // If there is a filter for top n rows, then apply it
            if (initFilters.LimitResults.HasValue)
            {
                if (initFilters.LimitResults < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(initFilters.LimitResults), SR.EditDataFilteringNegativeLimit);
                }
                queryBuilder.AppendFormat("TOP {0} ", initFilters.LimitResults.Value);
            }

            // Using the columns we know, add them to the query
            var columns = metadata.Columns.Select(col => col.ExpressionForSelectStatement);
            var columnClause = string.Join(", ", columns);
            queryBuilder.Append(columnClause);

            // Add the FROM
            queryBuilder.AppendFormat(" FROM {0}", metadata.EscapedMultipartName);

            return queryBuilder.ToString();
        }

        private void ThrowIfNotInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(SR.EditDataSessionNotInitialized);
            }
        }

        /// <summary>
        /// Removes the edit from the edit cache if the row is no longer dirty
        /// </summary>
        /// <param name="rowId">ID of the update to cleanup</param>
        /// <param name="editCellResult">Result with row dirty flag</param>
        private void CleanupEditIfRowClean(long rowId, EditCellResult editCellResult)
        {
            // If the row is still dirty, don't do anything
            if (editCellResult.IsRowDirty)
            {
                return;
            }

            // Make an attempt to remove the clean row edit. If this fails, it'll be handled on commit attempt.
            RowEditBase removedRow;
            EditCache.TryRemove(rowId, out removedRow);
        }

        internal void UpdateColumnInformationWithMetadata(DbColumnWrapper[] columns)
        {
            if (columns == null || this.objectMetadata == null)
            {
                return;
            }

            foreach (DbColumnWrapper col in columns)
            {
                var columnMetadata = objectMetadata.Columns.FirstOrDefault(cm => { return cm.EscapedName == ToSqlScript.FormatIdentifier(col.ColumnName); });
                col.IsHierarchyId = columnMetadata != null && columnMetadata.IsHierarchyId;
            }
        }

        #endregion

        /// <summary>
        /// State object to return upon completion of an edit session intialization query
        /// </summary>
        public class EditSessionQueryExecutionState
        {
            /// <summary>
            /// The query object that was used to execute the edit initialization query. If
            /// <c>null</c> the query was not successfully executed.
            /// </summary>
            public Query Query { get; set; }

            /// <summary>
            /// Any message that may have occurred during execution of the query (ie, exceptions).
            /// If this is and <see cref="Query"/> are <c>null</c> then the error messages were
            /// returned via message events.
            /// </summary>
            public string Message { get; set; }

            /// <summary>
            /// Constructs a new instance. Sets the values of the properties.
            /// </summary>
            public EditSessionQueryExecutionState(Query query, string message = null)
            {
                Query = query;
                Message = message;
            }
        }
    }
}
