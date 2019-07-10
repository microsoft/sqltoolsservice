//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility.SqlScriptFormatters;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    /// <summary>
    /// An update to apply to a row of a result set. This will generate an UPDATE statement.
    /// </summary>
    public sealed class RowUpdate : RowEditBase
    {
        private const string DeclareStatement = "DECLARE {0} TABLE ({1})";
        private const string UpdateOutput = "UPDATE {0} SET {1} OUTPUT {2} INTO {3} {4}";
        private const string UpdateOutputMemOptimized = "UPDATE {0} WITH (SNAPSHOT) SET {1} OUTPUT {2} INTO {3} {4}";
        private const string UpdateScript = "UPDATE {0} SET {1} {2}";
        private const string UpdateScriptMemOptimized = "UPDATE {0} WITH (SNAPSHOT) SET {1} {2}";
        private const string SelectStatement = "SELECT {0} FROM {1}";
        private string validateUpdateOnlyOneRow = "DECLARE @numberOfRows int = 0;" + Environment.NewLine +
                                                          "Select @numberOfRows = count(*) FROM {0} {1} " + Environment.NewLine +
                                                          "IF (@numberOfRows > 1) " + Environment.NewLine +
                                                          "Begin" + Environment.NewLine +
                                                           " DECLARE @error NVARCHAR(100) = N'The row value(s) updated do not make the row unique or they alter multiple rows(' + CAST(@numberOfRows as varchar(10)) + ' rows)';" + Environment.NewLine +
                                                           " RAISERROR (@error, 16, 1) " + Environment.NewLine +
                                                          "End" + Environment.NewLine +
                                                          "ELSE BEGIN" + Environment.NewLine;

        internal readonly ConcurrentDictionary<int, CellUpdate> cellUpdates;
        private readonly IList<DbCellValue> associatedRow;

        /// <summary>
        /// Constructs a new RowUpdate to be added to the cache.
        /// </summary>
        /// <param name="rowId">Internal ID of the row that will be updated with this object</param>
        /// <param name="associatedResultSet">Result set for the rows of the object to update</param>
        /// <param name="associatedMetadata">Metadata provider for the object to update</param>
        public RowUpdate(long rowId, ResultSet associatedResultSet, EditTableMetadata associatedMetadata)
            : base(rowId, associatedResultSet, associatedMetadata)
        {
            cellUpdates = new ConcurrentDictionary<int, CellUpdate>();
            associatedRow = AssociatedResultSet.GetRow(rowId);
        }

        /// <summary>
        /// Sort order property. Sorts to same position as RowCreate
        /// </summary>
        protected override int SortId => 1;

        #region Public Methods

        /// <summary>
        /// Applies the changes to the associated result set after successfully executing the
        /// change on the database
        /// </summary>
        /// <param name="dataReader">
        /// Reader returned from the execution of the command to update a row. Should contain
        /// a single row that represents all the values of the row.
        /// </param>
        public override Task ApplyChanges(DbDataReader dataReader)
        {
            Validate.IsNotNull(nameof(dataReader), dataReader);
            return AssociatedResultSet.UpdateRow(RowId, dataReader);
        }

        /// <summary>
        /// Generates a command that can be executed to update a row -- and return the contents of
        /// the updated row.
        /// </summary>
        /// <param name="connection">The connection the command should be associated with</param>
        /// <returns>Command to update the row</returns>
        public override DbCommand GetCommand(DbConnection connection)
        {
            Validate.IsNotNull(nameof(connection), connection);

            // Process the cells and columns
            List<string> declareColumns = new List<string>();
            List<SqlParameter> inParameters = new List<SqlParameter>();
            List<string> setComponents = new List<string>();
            List<string> outClauseColumns = new List<string>();
            List<string> selectColumns = new List<string>();
            for (int i = 0; i < AssociatedObjectMetadata.Columns.Length; i++)
            {
                EditColumnMetadata metadata = AssociatedObjectMetadata.Columns[i];

                // Add the output columns regardless of whether the column is read only
                declareColumns.Add($"{metadata.EscapedName} {ToSqlScript.FormatColumnType(metadata.DbColumn, useSemanticEquivalent: true)}");
                if (metadata.IsHierarchyId)
                {
                    outClauseColumns.Add($"inserted.{metadata.EscapedName}.ToString() {metadata.EscapedName}");
                }
                else
                {
                    outClauseColumns.Add($"inserted.{metadata.EscapedName}");
                }
                selectColumns.Add(metadata.EscapedName);

                // If we have a new value for the column, proccess it now
                CellUpdate cellUpdate;
                if (cellUpdates.TryGetValue(i, out cellUpdate))
                {
                    string paramName = $"@Value{RowId}_{i}";
                    if (metadata.IsHierarchyId)
                    {
                        setComponents.Add($"{metadata.EscapedName} = CONVERT(hierarchyid,{paramName})");
                    }
                    else
                    {
                        setComponents.Add($"{metadata.EscapedName} = {paramName}");
                    }
                    inParameters.Add(new SqlParameter(paramName, AssociatedResultSet.Columns[i].SqlDbType) { Value = cellUpdate.Value });
                }
            }

            // Put everything together into a single query
            // Step 1) Build a temp table for inserting output values into
            string tempTableName = $"@Update{RowId}Output";
            string declareStatement = string.Format(DeclareStatement, tempTableName, string.Join(", ", declareColumns));

            // Step 2) Build the update statement
            WhereClause whereClause = GetWhereClause(true);

            string updateStatementFormat = AssociatedObjectMetadata.IsMemoryOptimized
                ? UpdateOutputMemOptimized
                : UpdateOutput;
            string updateStatement = string.Format(updateStatementFormat,
                AssociatedObjectMetadata.EscapedMultipartName,
                string.Join(", ", setComponents),
                string.Join(", ", outClauseColumns),
                tempTableName,
                whereClause.CommandText);


            string validateScript = string.Format(CultureInfo.InvariantCulture, validateUpdateOnlyOneRow,
                AssociatedObjectMetadata.EscapedMultipartName,
                whereClause.CommandText);

            // Step 3) Build the select statement
            string selectStatement = string.Format(SelectStatement, string.Join(", ", selectColumns), tempTableName);

            // Step 4) Put it all together into a results object
            StringBuilder query = new StringBuilder();
            query.AppendLine(declareStatement);
            query.AppendLine(validateScript);
            query.AppendLine(updateStatement);
            query.AppendLine(selectStatement);
            query.Append("END");

            // Build the command
            DbCommand command = connection.CreateCommand();
            command.CommandText = query.ToString();
            command.CommandType = CommandType.Text;
            command.Parameters.AddRange(inParameters.ToArray());
            command.Parameters.AddRange(whereClause.Parameters.ToArray());

            return command;
        }

        /// <summary>
        /// Generates a edit row that represents a row with pending update. The cells pending
        /// updates are merged into the unchanged cells.
        /// </summary>
        /// <param name="cachedRow">Original, cached cell contents</param>
        /// <returns>EditRow with pending updates</returns>
        public override EditRow GetEditRow(DbCellValue[] cachedRow)
        {
            Validate.IsNotNull(nameof(cachedRow), cachedRow);

            // Treat all the cells as clean initially
            EditCell[] editCells = cachedRow.Select(cell => new EditCell(cell, false)).ToArray();

            // For each cell that is pending update, replace the db cell value with a dirty one
            foreach (var cellUpdate in cellUpdates)
            {
                editCells[cellUpdate.Key] = cellUpdate.Value.AsEditCell;
            }

            return new EditRow
            {
                Id = RowId,
                Cells = editCells,
                State = EditRow.EditRowState.DirtyUpdate
            };
        }

        /// <summary>
        /// Constructs an update statement to change the associated row.
        /// </summary>
        /// <returns>An UPDATE statement</returns>
        public override string GetScript()
        {
            // Build the "SET" portion of the statement
            var setComponents = cellUpdates.Values.Select(cellUpdate =>
            {
                string formattedColumnName = ToSqlScript.FormatIdentifier(cellUpdate.Column.ColumnName);
                string formattedValue = ToSqlScript.FormatValue(cellUpdate.Value, cellUpdate.Column);
                return $"{formattedColumnName} = {formattedValue}";
            });
            string setClause = string.Join(", ", setComponents);

            // Put everything together into a single query
            string whereClause = GetWhereClause(false).CommandText;
            string updateStatementFormat = AssociatedObjectMetadata.IsMemoryOptimized
                ? UpdateScriptMemOptimized
                : UpdateScript;

            return string.Format(updateStatementFormat,
                AssociatedObjectMetadata.EscapedMultipartName,
                setClause,
                whereClause
            );
        }

        /// <summary>
        /// Reverts the value of a cell to its original value
        /// </summary>
        /// <param name="columnId">Ordinal of the column to revert</param>
        /// <returns>The value that was </returns>
        public override EditRevertCellResult RevertCell(int columnId)
        {
            Validate.IsWithinRange(nameof(columnId), columnId, 0, associatedRow.Count - 1);

            // Remove the cell update
            // NOTE: This is best effort. The only way TryRemove can fail is if it is already
            //       removed. If this happens, it is OK.
            CellUpdate cellUpdate;
            cellUpdates.TryRemove(columnId, out cellUpdate);

            return new EditRevertCellResult
            {
                IsRowDirty = cellUpdates.Count > 0,
                Cell = new EditCell(associatedRow[columnId], false)
            };
        }

        /// <summary>
        /// Sets the value of the cell in the associated row. If <paramref name="newValue"/> is
        /// identical to the original value, this will remove the cell update from the row update.
        /// </summary>
        /// <param name="columnId">Ordinal of the columns that will be set</param>
        /// <param name="newValue">String representation of the value the user input</param>
        /// <returns>
        /// The string representation of the new value (after conversion to target object) if the
        /// a change is made. <c>null</c> is returned if the cell is reverted to it's original value.
        /// </returns>
        public override EditUpdateCellResult SetCell(int columnId, string newValue)
        {
            // Validate the value and convert to object
            ValidateColumnIsUpdatable(columnId);
            CellUpdate update = new CellUpdate(AssociatedResultSet.Columns[columnId], newValue);

            // If the value is the same as the old value, we shouldn't make changes
            // NOTE: We must use .Equals in order to ignore object to object comparisons
            if (update.Value.Equals(associatedRow[columnId].RawObject))
            {
                // Remove any pending change and stop processing this (we don't care if we fail to remove something)
                CellUpdate cu;
                cellUpdates.TryRemove(columnId, out cu);
                return new EditUpdateCellResult
                {
                    IsRowDirty = cellUpdates.Count > 0,
                    Cell = new EditCell(associatedRow[columnId], false)
                };
            }

            // The change is real, so set it
            cellUpdates.AddOrUpdate(columnId, update, (i, cu) => update);
            return new EditUpdateCellResult
            {
                IsRowDirty = true,
                Cell = update.AsEditCell
            };
        }

        #endregion
    }
}
