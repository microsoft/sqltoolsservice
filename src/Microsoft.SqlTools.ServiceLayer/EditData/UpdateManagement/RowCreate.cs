//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
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
    /// Represents a row that should be added to the result set. Generates an INSERT statement.
    /// </summary>
    public sealed class RowCreate : RowEditBase
    {
        private const string DeclareStatement = "DECLARE {0} TABLE ({1})";
        private const string InsertOutputDefaultStatement = "INSERT INTO {0} OUTPUT {1} INTO {2} DEFAULT VALUES";
        private const string InsertOutputValuesStatement = "INSERT INTO {0}({1}) OUTPUT {2} INTO {3} VALUES ({4})";
        private const string InsertScriptDefaultStatement = "INSERT INTO {0} DEFAULT VALUES";
        private const string InsertScriptValuesStatement = "INSERT INTO {0}({1}) VALUES ({2})";
        private const string SelectStatement = "SELECT {0} FROM {1}";

        internal readonly CellUpdate[] newCells;

        /// <summary>
        /// Creates a new Row Creation edit to the result set
        /// </summary>
        /// <param name="rowId">Internal ID of the row that is being created</param>
        /// <param name="associatedResultSet">The result set for the rows in the table we're editing</param>
        /// <param name="associatedMetadata">The metadata for table we're editing</param>
        public RowCreate(long rowId, ResultSet associatedResultSet, EditTableMetadata associatedMetadata)
            : base(rowId, associatedResultSet, associatedMetadata)
        {
            newCells = new CellUpdate[associatedResultSet.Columns.Length];
            
            // Process the default cell values. If the column is calculated, then the value is a placeholder
            DefaultValues = associatedMetadata.Columns.Select((col, index) => col.IsCalculated.HasTrue()
                ? SR.EditDataComputedColumnPlaceholder
                : col.DefaultValue).ToArray();
        }

        /// <summary>
        /// Sort ID for a RowCreate object. Setting to 1 ensures that these are the first changes 
        /// to be committed
        /// </summary>
        protected override int SortId => 1;

        /// <summary>
        /// Default values for the row, will be applied as cell updates if there isn't a user-
        /// provided cell update during commit
        /// </summary>
        public string[] DefaultValues { get; }
        
        #region Public Methods

        /// <summary>
        /// Applies the changes to the associated result set after successfully executing the
        /// change on the database
        /// </summary>
        /// <param name="dataReader">
        /// Reader returned from the execution of the command to insert a new row. Should contain
        /// a single row that represents the newly added row.
        /// </param>
        public override Task ApplyChanges(DbDataReader dataReader)
        {
            Validate.IsNotNull(nameof(dataReader), dataReader);

            return AssociatedResultSet.AddRow(dataReader);
        }

        /// <summary>
        /// Generates a command that can be executed to insert a new row -- and return the newly
        /// inserted row.
        /// </summary>
        /// <param name="connection">The connection the command should be associated with</param>
        /// <returns>Command to insert the new row</returns>
        public override DbCommand GetCommand(DbConnection connection)
        {
            Validate.IsNotNull(nameof(connection), connection);

            // Process the cells and columns
            List<string> declareColumns = new List<string>();
            List<string> inColumnNames = new List<string>();
            List<string> outClauseColumnNames = new List<string>();
            List<string> inValues = new List<string>();
            List<SqlParameter> inParameters = new List<SqlParameter>();
            List<string> selectColumns = new List<string>();
            for(int i = 0; i < AssociatedObjectMetadata.Columns.Length; i++)
            {
                DbColumnWrapper column = AssociatedResultSet.Columns[i];
                EditColumnMetadata metadata = AssociatedObjectMetadata.Columns[i];
                CellUpdate cell = newCells[i];
                
                // Add the output columns regardless of whether the column is read only
                outClauseColumnNames.Add($"inserted.{metadata.EscapedName}");
                declareColumns.Add($"{metadata.EscapedName} {ToSqlScript.FormatColumnType(column, useSemanticEquivalent: true)}");
                selectColumns.Add(metadata.EscapedName);

                // Continue if we're not inserting a value for this column
                if (!IsCellValueProvided(column, cell, DefaultValues[i]))
                {
                    continue;
                }
                
                // Add the input column
                inColumnNames.Add(metadata.EscapedName);
                
                // Add the input values as parameters
                string paramName = $"@Value{RowId}_{i}";
                inValues.Add(paramName);
                inParameters.Add(new SqlParameter(paramName, column.SqlDbType) {Value = cell.Value});
            }
            
            // Put everything together into a single query            
            // Step 1) Build a temp table for inserting output values into
            string tempTableName = $"@Insert{RowId}Output";
            string declareStatement = string.Format(DeclareStatement, tempTableName, string.Join(", ", declareColumns));

            // Step 2) Build the insert statement
            string joinedOutClauseNames = string.Join(", ", outClauseColumnNames);
            string insertStatement = inValues.Count > 0
                ? string.Format(InsertOutputValuesStatement, 
                    AssociatedObjectMetadata.EscapedMultipartName,
                    string.Join(", ", inColumnNames), 
                    joinedOutClauseNames,
                    tempTableName,
                    string.Join(", ", inValues))
                : string.Format(InsertOutputDefaultStatement, 
                    AssociatedObjectMetadata.EscapedMultipartName,
                    joinedOutClauseNames,
                    tempTableName);

            // Step 3) Build the select statement
            string selectStatement = string.Format(SelectStatement, string.Join(", ", selectColumns), tempTableName);
            
            // Step 4) Put it all together into a results object
            StringBuilder query = new StringBuilder();
            query.AppendLine(declareStatement);
            query.AppendLine(insertStatement);
            query.Append(selectStatement);
            
            // Build the command
            DbCommand command = connection.CreateCommand();
            command.CommandText = query.ToString();
            command.CommandType = CommandType.Text;
            command.Parameters.AddRange(inParameters.ToArray());
                
            return command;
        }

        /// <summary>
        /// Generates a edit row that represents a row pending insertion
        /// </summary>
        /// <param name="cachedRow">Original, cached cell contents. (Should be null in this case)</param>
        /// <returns>EditRow of pending update</returns>
        public override EditRow GetEditRow(DbCellValue[] cachedRow)
        {
            // Get edit cells for each 
            EditCell[] editCells = newCells.Select(GetEditCell).ToArray();
            
            return new EditRow
            {
                Id = RowId,
                Cells = editCells,
                State = EditRow.EditRowState.DirtyInsert
            };
        }

        /// <summary>
        /// Generates the INSERT INTO statement that will apply the row creation
        /// </summary>
        /// <returns>INSERT INTO statement</returns>
        public override string GetScript()
        {
            // Process the cells and columns
            List<string> inColumns = new List<string>();
            List<string> inValues = new List<string>();
            for (int i = 0; i < AssociatedObjectMetadata.Columns.Length; i++)
            {
                DbColumnWrapper column = AssociatedResultSet.Columns[i];
                CellUpdate cell = newCells[i];
                
                // Continue if we're not inserting a value for this column
                if (!IsCellValueProvided(column, cell, DefaultValues[i]))
                {
                    continue;
                }
                
                // Column is provided
                inColumns.Add(AssociatedObjectMetadata.Columns[i].EscapedName);
                inValues.Add(ToSqlScript.FormatValue(cell.AsDbCellValue, column));
            }
            
            // Build the insert statement
            return inValues.Count > 0
                ? string.Format(InsertScriptValuesStatement, 
                    AssociatedObjectMetadata.EscapedMultipartName,
                    string.Join(", ", inColumns), 
                    string.Join(", ", inValues))
                : string.Format(InsertScriptDefaultStatement, AssociatedObjectMetadata.EscapedMultipartName);
        }

        /// <summary>
        /// Reverts a cell to an unset value.
        /// </summary>
        /// <param name="columnId">The ordinal ID of the cell to reset</param>
        /// <returns>The default value for the column, or null if no default is defined</returns>
        public override EditRevertCellResult RevertCell(int columnId)
        {
            // Validate that the column can be reverted
            Validate.IsWithinRange(nameof(columnId), columnId, 0, newCells.Length - 1);

            // Remove the cell update from list of set cells
            newCells[columnId] = null;
            return new EditRevertCellResult
            {
                IsRowDirty = true, 
                Cell = GetEditCell(null, columnId)
            };
        }

        /// <summary>
        /// Sets the value of a cell in the row to be added
        /// </summary>
        /// <param name="columnId">Ordinal of the column to set in the row</param>
        /// <param name="newValue">String representation from the client of the value to add</param>
        /// <returns>
        /// The updated value as a string of the object generated from <paramref name="newValue"/>
        /// </returns>
        public override EditUpdateCellResult SetCell(int columnId, string newValue)
        {
            // Validate the column and the value and convert to object
            ValidateColumnIsUpdatable(columnId);
            CellUpdate update = new CellUpdate(AssociatedResultSet.Columns[columnId], newValue);

            // Add the cell update to the 
            newCells[columnId] = update;

            // Put together a result of the change
            return new EditUpdateCellResult
            {
                IsRowDirty = true,                // Row creates will always be dirty
                Cell = update.AsEditCell
            };
        }

        #endregion

        /// <summary>
        /// Verifies the column and cell, ensuring a column that needs a value has one.
        /// </summary>
        /// <param name="column">Column that will be inserted into</param>
        /// <param name="cell">Current cell value for this row</param>
        /// <param name="defaultCell">Default value for the column in this row</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the column needs a value but it is not provided
        /// </exception>
        /// <returns>
        /// <c>true</c> If the column has a value provided 
        /// <c>false</c> If the column does not have a value provided (column is read-only, has default, etc)
        /// </returns>
        private static bool IsCellValueProvided(DbColumnWrapper column, CellUpdate cell, string defaultCell)
        {
            // Skip columns that cannot be updated
            if (!column.IsUpdatable)
            {
                return false;
            }
                
            // Make sure a value was provided for the cell
            if (cell == null)
            {
                // If the column is not nullable and there is not default defined, then fail
                if (!column.AllowDBNull.HasTrue() && defaultCell == null)
                {
                    throw new InvalidOperationException(SR.EditDataCreateScriptMissingValue(column.ColumnName));
                }
                    
                // There is a default value (or omitting the value is fine), so trust the db will apply it correctly
                return false;
            }

            return true;
        }
        
        private EditCell GetEditCell(CellUpdate cell, int index)
        {
            DbCellValue dbCell;
            if (cell == null)
            {
                // Cell hasn't been provided by user yet, attempt to use the default value
                dbCell = new DbCellValue
                {
                    DisplayValue = DefaultValues[index] ?? string.Empty,
                    IsNull = false,    // TODO: This doesn't properly consider null defaults 
                    RawObject = null,
                    RowId = RowId
                };
            }
            else
            {
                // Cell has been provided by user, so use that
                dbCell = cell.AsDbCellValue;
            }
            return new EditCell(dbCell, isDirty: true);
        }
    }
}
