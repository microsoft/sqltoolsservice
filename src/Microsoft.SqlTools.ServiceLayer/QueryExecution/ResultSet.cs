// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Class that represents a resultset the was generated from a query. Contains logic for
    /// storing and retrieving results. Is contained by a Batch class.
    /// </summary>
    public class ResultSet : IDisposable
    {
        #region Constants

        // Column names of 'for xml' and 'for json' queries
        private const string NameOfForXmlColumn = "XML_F52E2B61-18A1-11d1-B105-00805F49916B";
        private const string NameOfForJsonColumn = "JSON_F52E2B61-18A1-11d1-B105-00805F49916B";
        private const string YukonXmlShowPlanColumn = "Microsoft SQL Server 2005 XML Showplan";

        #endregion

        #region Member Variables

        /// <summary>
        /// For IDisposable pattern, whether or not object has been disposed
        /// </summary>
        private bool disposed;

        /// <summary>
        /// A list of offsets into the buffer file that correspond to where rows start
        /// </summary>
        private readonly LongList<long> fileOffsets;

        /// <summary>
        /// The factory to use to get reading/writing handlers
        /// </summary>
        private readonly IFileStreamFactory fileStreamFactory;

        /// <summary>
        /// Whether or not the result set has been read in from the database,
        /// set as internal in order to fake value in unit tests
        /// </summary>
        internal bool hasBeenRead;

        /// <summary>
        /// Whether resultSet is a 'for xml' or 'for json' result
        /// </summary>
        private bool isSingleColumnXmlJsonResultSet;

        /// <summary>
        /// The name of the temporary file we're using to output these results in
        /// </summary>
        private readonly string outputFileName;

        /// <summary>
        /// Row count to use in special scenarios where we want to override the number of rows.
        /// </summary>
        private long? rowCountOverride;

        /// <summary>
        /// The special action which applied to this result set
        /// </summary>
        private readonly SpecialAction specialAction;

        /// <summary>
        /// Total number of bytes written to the file. Used to jump to end of the file for append
        /// scenarios. Internal for unit test validation.
        /// </summary>
        internal long totalBytesWritten;

        #endregion

        /// <summary>
        /// Creates a new result set and initializes its state
        /// </summary>
        /// <param name="ordinal">The ID of the resultset, the ordinal of the result within the batch</param>
        /// <param name="batchOrdinal">The ID of the batch, the ordinal of the batch within the query</param>
        /// <param name="factory">Factory for creating a reader/writer</param>
        public ResultSet(int ordinal, int batchOrdinal, IFileStreamFactory factory)
        {
            Id = ordinal;
            BatchId = batchOrdinal;

            // Initialize the storage
            totalBytesWritten = 0;
            outputFileName = factory.CreateFile();
            fileOffsets = new LongList<long>();
            specialAction = new SpecialAction();

            // Store the factory
            fileStreamFactory = factory;
            hasBeenRead = false;
            SaveTasks = new ConcurrentDictionary<string, Task>();
        }

        #region Eventing

        /// <summary>
        /// Asynchronous handler for when saving query results succeeds
        /// </summary>
        /// <param name="parameters">Request parameters for identifying the request</param>
        public delegate Task SaveAsAsyncEventHandler(SaveResultsRequestParams parameters);

        /// <summary>
        /// Asynchronous handler for when saving query results fails
        /// </summary>
        /// <param name="parameters">Request parameters for identifying the request</param>
        /// <param name="message">Message to send back describing why the request failed</param>
        public delegate Task SaveAsFailureAsyncEventHandler(SaveResultsRequestParams parameters, string message);

        /// <summary>
        /// Asynchronous handler for when a resultset has completed
        /// </summary>
        /// <param name="resultSet">The result set that completed</param>
        public delegate Task ResultSetAsyncEventHandler(ResultSet resultSet);

        /// <summary>
        /// Event that will be called when the result set has completed execution
        /// </summary>
        public event ResultSetAsyncEventHandler ResultCompletion;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the resultSet is in the process of being disposed
        /// </summary>
        /// <returns></returns>
        internal bool IsBeingDisposed { get; private set; }

        /// <summary>
        /// The columns for this result set
        /// </summary>
        public DbColumnWrapper[] Columns { get; private set; }

        /// <summary>
        /// ID of the result set, relative to the batch
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// ID of the batch set, relative to the query
        /// </summary>
        public int BatchId { get; private set; }

        /// <summary>
        /// The number of rows for this result set
        /// </summary>
        public long RowCount => rowCountOverride ?? fileOffsets.Count;

        /// <summary>
        /// All save tasks currently saving this ResultSet
        /// </summary>
        internal ConcurrentDictionary<string, Task> SaveTasks { get; set; }

        /// <summary>
        /// Generates a summary of this result set
        /// </summary>
        public ResultSetSummary Summary
        {
            get
            {
                return new ResultSetSummary
                {
                    ColumnInfo = Columns,
                    Id = Id,
                    BatchId = BatchId,
                    RowCount = RowCount,
                    SpecialAction = hasBeenRead ? ProcessSpecialAction() : null
                };
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a specific row from the result set.
        /// </summary>
        /// <remarks>
        /// Creates a new file reader for a single reader. This method should only be used for one
        /// off requests, not for requesting a large subset of the results.
        /// </remarks>
        /// <param name="rowId">The internal ID of the row to read</param>
        /// <returns>The requested row</returns>
        public IList<DbCellValue> GetRow(long rowId)
        {
            // Sanity check to make sure that results have been read beforehand
            if (!hasBeenRead)
            {
                throw new InvalidOperationException(SR.QueryServiceResultSetNotRead);
            }

            // Sanity check to make sure that the row exists
            if (rowId >= RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(rowId), SR.QueryServiceResultSetStartRowOutOfRange);
            }

            using (IFileStreamReader fileStreamReader = fileStreamFactory.GetReader(outputFileName))
            {
                return fileStreamReader.ReadRow(fileOffsets[rowId], rowId, Columns);
            }
        }

        /// <summary>
        /// Generates a subset of the rows from the result set
        /// </summary>
        /// <param name="startRow">The starting row of the results</param>
        /// <param name="rowCount">How many rows to retrieve</param>
        /// <returns>A subset of results</returns>
        public Task<ResultSetSubset> GetSubset(long startRow, int rowCount)
        {
            // Sanity check to make sure that the results have been read beforehand
            if (!hasBeenRead)
            {
                throw new InvalidOperationException(SR.QueryServiceResultSetNotRead);
            }

            // Sanity check to make sure that the row and the row count are within bounds
            if (startRow < 0 || startRow >= RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(startRow), SR.QueryServiceResultSetStartRowOutOfRange);
            }
            if (rowCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowCount), SR.QueryServiceResultSetRowCountOutOfRange);
            }

            return Task.Factory.StartNew(() =>
            {

                DbCellValue[][] rows;

                using (IFileStreamReader fileStreamReader = fileStreamFactory.GetReader(outputFileName))
                {
                    // If result set is 'for xml' or 'for json',
                    // Concatenate all the rows together into one row
                    if (isSingleColumnXmlJsonResultSet)
                    {
                        // Iterate over all the rows and process them into a list of string builders
                        // ReSharper disable once AccessToDisposedClosure   The lambda is used immediately in string.Join call
                        IEnumerable<string> rowValues = fileOffsets.Select(rowOffset => fileStreamReader.ReadRow(rowOffset, 0, Columns)[0].DisplayValue);
                        string singleString = string.Join(string.Empty, rowValues);
                        DbCellValue cellValue = new DbCellValue
                        {
                            DisplayValue = singleString,
                            IsNull = false,
                            RawObject = singleString,
                            RowId = 0
                        };
                        rows = new[] { new[] { cellValue } };
                    }
                    else
                    {
                        // Figure out which rows we need to read back
                        IEnumerable<long> rowOffsets = fileOffsets.LongSkip(startRow).Take(rowCount);

                        // Iterate over the rows we need and process them into output
                        // ReSharper disable once AccessToDisposedClosure   The lambda is used immediately in .ToArray call
                        rows = rowOffsets.Select((offset, id) => fileStreamReader.ReadRow(offset, id, Columns).ToArray()).ToArray();
                    }
                }
                // Retrieve the subset of the results as per the request
                return new ResultSetSubset
                {
                    Rows = rows,
                    RowCount = rows.Length
                };
            });
        }

        /// <summary>
        /// Generates the execution plan from the table returned 
        /// </summary>
        /// <returns>An execution plan object</returns>
        public Task<ExecutionPlan> GetExecutionPlan()
        {
            // Process the action just incase is hasn't been yet 
            ProcessSpecialAction();

            // Sanity check to make sure that the results have been read beforehand
            if (!hasBeenRead)
            {
                throw new InvalidOperationException(SR.QueryServiceResultSetNotRead);
            }
            // Check that we this result set contains a showplan 
            if (!specialAction.ExpectYukonXMLShowPlan)
            {
                throw new Exception(SR.QueryServiceExecutionPlanNotFound);
            }


            return Task.Factory.StartNew(() =>
            { 
                string content;
                string format = null;

                using (IFileStreamReader fileStreamReader = fileStreamFactory.GetReader(outputFileName))
                {
                    // Determine the format and get the first col/row of XML
                    content = fileStreamReader.ReadRow(0, 0, Columns)[0].DisplayValue;

                    if (specialAction.ExpectYukonXMLShowPlan) 
                    {
                        format = "xml";
                    }
                }
                    
                return new ExecutionPlan
                {
                    Format = format,
                    Content = content
                };
            });
        }

        /// <summary>
        /// Reads from the reader until there are no more results to read
        /// </summary>
        /// <param name="dbDataReader">The data reader for getting results from the db</param>
        /// <param name="cancellationToken">Cancellation token for cancelling the query</param>
        public async Task ReadResultToEnd(DbDataReader dbDataReader, CancellationToken cancellationToken)
        {
            // Sanity check to make sure we got a reader
            Validate.IsNotNull(nameof(dbDataReader), dbDataReader);

            try
            {
                // Verify the request hasn't been cancelled
                cancellationToken.ThrowIfCancellationRequested();

                // Mark that result has been read
                hasBeenRead = true;

                StorageDataReader dataReader = new StorageDataReader(dbDataReader);

                // Open a writer for the file
                var fileWriter = fileStreamFactory.GetWriter(outputFileName);
                using (fileWriter)
                {
                    // If we can initialize the columns using the column schema, use that
                    if (!dataReader.DbDataReader.CanGetColumnSchema())
                    {
                        throw new InvalidOperationException(SR.QueryServiceResultSetNoColumnSchema);
                    }
                    Columns = dataReader.Columns;
                    while (await dataReader.ReadAsync(cancellationToken))
                    {
                        fileOffsets.Add(totalBytesWritten);
                        totalBytesWritten += fileWriter.WriteRow(dataReader);
                    }
                }
                // Check if resultset is 'for xml/json'. If it is, set isJson/isXml value in column metadata
                SingleColumnXmlJsonResultSet();
            }
            finally
            {
                // Fire off a result set completion event if we have one
                if (ResultCompletion != null)
                {
                    await ResultCompletion(this);
                }
            }
        }

        /// <summary>
        /// Removes a row from the result set cache
        /// </summary>
        /// <param name="internalId">Internal ID of the row</param>
        public void RemoveRow(long internalId)
        {
            // Make sure that the results have been read
            if (!hasBeenRead)
            {
                throw new InvalidOperationException(SR.QueryServiceResultSetNotRead);
            }

            // Simply remove the row from the list of row offsets
            fileOffsets.RemoveAt(internalId);
        }

        /// <summary>
        /// Adds a new row to the result set by reading the row from the provided db data reader
        /// </summary>
        /// <param name="dbDataReader">The result of a command to insert a new row should be UNREAD</param>
        public async Task AddRow(DbDataReader dbDataReader)
        {
            // Write the new row to the end of the file
            long newOffset = await AppendRowToBuffer(dbDataReader);

            // Add the row to file offset list
            fileOffsets.Add(newOffset);
        }

        /// <summary>
        /// Updates the values in a row with the 
        /// </summary>
        /// <param name="rowId"></param>
        /// <param name="dbDataReader"></param>
        /// <returns></returns>
        public async Task UpdateRow(long rowId, DbDataReader dbDataReader)
        {
            // Write the updated row to the end of the file
            long newOffset = await AppendRowToBuffer(dbDataReader);

            // Update the file offset of the row in question
            fileOffsets[rowId] = newOffset;
        }

        /// <summary>
        /// Saves the contents of this result set to a file using the IFileStreamFactory provided
        /// </summary>
        /// <param name="saveParams">Parameters for saving the results to a file</param>
        /// <param name="fileFactory">
        /// Factory for creating a stream reader/writer combo for writing results to disk
        /// </param>
        /// <param name="successHandler">Handler for a successful write of all rows</param>
        /// <param name="failureHandler">Handler for unsuccessful write of all rows</param>
        public void SaveAs(SaveResultsRequestParams saveParams, IFileStreamFactory fileFactory,
            SaveAsAsyncEventHandler successHandler, SaveAsFailureAsyncEventHandler failureHandler)
        {
            // Sanity check the save params and file factory
            Validate.IsNotNull(nameof(saveParams), saveParams);
            Validate.IsNotNull(nameof(fileFactory), fileFactory);

            // Make sure the resultset has finished being read
            if (!hasBeenRead)
            {
                throw new InvalidOperationException(SR.QueryServiceSaveAsResultSetNotComplete);
            }

            // Make sure there isn't a task for this file already
            Task existingTask;
            if (SaveTasks.TryGetValue(saveParams.FilePath, out existingTask))
            {
                if (existingTask.IsCompleted)
                {
                    // The task has completed, so let's attempt to remove it
                    if (!SaveTasks.TryRemove(saveParams.FilePath, out existingTask))
                    {
                        throw new InvalidOperationException(SR.QueryServiceSaveAsMiscStartingError);
                    }
                }
                else
                {
                    // The task hasn't completed, so we shouldn't continue
                    throw new InvalidOperationException(SR.QueryServiceSaveAsInProgress);
                }
            }

            // Create the new task
            Task saveAsTask = new Task(async () =>
            {
                try
                {
                    // Set row counts depending on whether save request is for entire set or a subset
                    long rowEndIndex = RowCount;
                    int rowStartIndex = 0;
                    if (saveParams.IsSaveSelection)
                    {
                        // ReSharper disable PossibleInvalidOperationException  IsSaveSelection verifies these values exist
                        rowEndIndex = saveParams.RowEndIndex.Value + 1;
                        rowStartIndex = saveParams.RowStartIndex.Value;
                        // ReSharper restore PossibleInvalidOperationException
                    }

                    using (var fileReader = fileFactory.GetReader(outputFileName))
                    using (var fileWriter = fileFactory.GetWriter(saveParams.FilePath))
                    {
                        // Iterate over the rows that are in the selected row set
                        for (long i = rowStartIndex; i < rowEndIndex; ++i)
                        {
                            var row = fileReader.ReadRow(fileOffsets[i], i, Columns);
                            fileWriter.WriteRow(row, Columns);
                        }
                        if (successHandler != null)
                        {
                            await successHandler(saveParams);
                        }
                    }
                }
                catch (Exception e)
                {
                    fileFactory.DisposeFile(saveParams.FilePath);
                    if (failureHandler != null)
                    {
                        await failureHandler(saveParams, e.Message);
                    }
                }
            });

            // If saving the task fails, return a failure
            if (!SaveTasks.TryAdd(saveParams.FilePath, saveAsTask))
            {
                throw new InvalidOperationException(SR.QueryServiceSaveAsMiscStartingError);
            }

            // Task was saved, so start up the task
            saveAsTask.Start();
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            IsBeingDisposed = true;
            // Check if saveTasks are running for this ResultSet
            if (!SaveTasks.IsEmpty)
            {
                // Wait for tasks to finish before disposing ResultSet
                Task.WhenAll(SaveTasks.Values.ToArray()).ContinueWith((antecedent) =>
                {
                    if (disposing)
                    {
                        fileStreamFactory.DisposeFile(outputFileName);
                    }
                    disposed = true;
                    IsBeingDisposed = false;
                });
            }
            else
            {
                // If saveTasks is empty, continue with dispose
                if (disposing)
                {
                    fileStreamFactory.DisposeFile(outputFileName);
                }
                disposed = true;
                IsBeingDisposed = false;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// If the result set represented by this class corresponds to a single XML
        /// column that contains results of "for xml" query, set isXml = true 
        /// If the result set represented by this class corresponds to a single JSON
        /// column that contains results of "for json" query, set isJson = true
        /// </summary>
        private void SingleColumnXmlJsonResultSet()
        {

            if (Columns?.Length == 1 && RowCount != 0)
            {
                if (Columns[0].ColumnName.Equals(NameOfForXmlColumn, StringComparison.Ordinal))
                {
                    Columns[0].IsXml = true;
                    isSingleColumnXmlJsonResultSet = true;
                    rowCountOverride = 1;
                }
                else if (Columns[0].ColumnName.Equals(NameOfForJsonColumn, StringComparison.Ordinal))
                {
                    Columns[0].IsJson = true;
                    isSingleColumnXmlJsonResultSet = true;
                    rowCountOverride = 1;
                }
            }
        }

        /// <summary>
        /// Determine the special action, if any, for this result set
        /// </summary>
        private SpecialAction ProcessSpecialAction() 
        {           

            // Check if this result set is a showplan 
            if (Columns.Length == 1 && string.Compare(Columns[0].ColumnName, YukonXmlShowPlanColumn, StringComparison.OrdinalIgnoreCase) == 0)
            {
                specialAction.ExpectYukonXMLShowPlan = true;
            }

            return specialAction;
        }

        /// <summary>
        /// Adds a single row to the end of the buffer file. INTENDED FOR SINGLE ROW INSERTION ONLY.
        /// </summary>
        /// <param name="dbDataReader">An UNREAD db data reader</param>
        /// <returns>The offset into the file where the row was inserted</returns>
        private async Task<long> AppendRowToBuffer(DbDataReader dbDataReader)
        {
            Validate.IsNotNull(nameof(dbDataReader), dbDataReader);
            if (!hasBeenRead)
            {
                throw new InvalidOperationException(SR.QueryServiceResultSetNotRead);
            }
            if (!dbDataReader.HasRows)
            {
                throw new InvalidOperationException(SR.QueryServiceResultSetAddNoRows);
            }

            StorageDataReader dataReader = new StorageDataReader(dbDataReader);

            using (IFileStreamWriter writer = fileStreamFactory.GetWriter(outputFileName))
            {
                // Write the row to the end of the file
                long currentFileOffset = totalBytesWritten;
                writer.Seek(currentFileOffset);
                await dataReader.ReadAsync(CancellationToken.None);
                totalBytesWritten += writer.WriteRow(dataReader);
                return currentFileOffset;
            }
        }

        #endregion
    }
}
