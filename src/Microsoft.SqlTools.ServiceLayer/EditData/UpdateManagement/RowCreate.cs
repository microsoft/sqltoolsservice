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
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    /// <summary>
    /// Represents a row that should be added to the result set. Generates an INSERT statement.
    /// </summary>
    public sealed class RowCreate : RowEditBase
    {
        private const string InsertScriptStart = "INSERT INTO {0}";
        private const string InsertScriptColumns = "({0})";
        private const string InsertScriptOut = " OUTPUT {0}";
        private const string InsertScriptDefault = " DEFAULT VALUES";
        private const string InsertScriptValues = " VALUES ({0})";

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

            // Build the script and generate a command
            ScriptBuildResult result = BuildInsertScript(forCommand: true);
            
            DbCommand command = connection.CreateCommand();
            command.CommandText = result.ScriptText;
            command.CommandType = CommandType.Text;
            command.Parameters.AddRange(result.ScriptParameters);
                
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
            return BuildInsertScript(forCommand: false).ScriptText;
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
        /// Generates an INSERT script that will insert this row
        /// </summary>
        /// <param name="forCommand">
        /// If <c>true</c> the script will be generated with an OUTPUT clause for returning all
        /// values in the inserted row (including computed values). The script will also generate
        /// parameters for inserting the values.
        /// If <c>false</c> the script will not have an OUTPUT clause and will have the values
        /// directly inserted into the script (with proper escaping, of course). 
        /// </param>
        /// <returns>A script build result object with the script text and any parameters</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there are columns that are not readonly, do not have default values, and were
        /// not assigned values.
        /// </exception>
        private ScriptBuildResult BuildInsertScript(bool forCommand)
        {           
            // Process all the columns in this table
            List<string> inValues = new List<string>();
            List<string> inColumns = new List<string>();
            List<string> outColumns = new List<string>();
            List<SqlParameter> sqlParameters = new List<SqlParameter>(); 
            for (int i = 0; i < AssociatedObjectMetadata.Columns.Length; i++)
            {
                DbColumnWrapper column = AssociatedResultSet.Columns[i];
                CellUpdate cell = newCells[i];
                
                // Add an out column if we're doing this for a command
                if (forCommand)
                {
                    outColumns.Add($"inserted.{SqlScriptFormatter.FormatIdentifier(column.ColumnName)}");
                }
                
                // Skip columns that cannot be updated
                if (!column.IsUpdatable)
                {
                    continue;
                }
                
                // Make sure a value was provided for the cell 
                if (cell == null)
                {
                    // If the column is not nullable and there is no default defined, then fail
                    if (!column.AllowDBNull.HasTrue() && DefaultValues[i] == null)
                    {
                        throw new InvalidOperationException(SR.EditDataCreateScriptMissingValue(column.ColumnName));
                    }
                    
                    // There is a default value (or omitting the value is fine), so trust the db will apply it correctly
                    continue;
                }

                // Add the input values
                if (forCommand)
                {
                    // Since this script is for command use, add parameter for the input value to the list
                    string paramName = $"@Value{RowId}_{i}";
                    inValues.Add(paramName);

                    SqlParameter param = new SqlParameter(paramName, cell.Column.SqlDbType) {Value = cell.Value};
                    sqlParameters.Add(param);
                }
                else
                {
                    // This script isn't for command use, add the value, formatted for insertion
                    inValues.Add(SqlScriptFormatter.FormatValue(cell.Value, column));
                }
                
                // Add the column to the in columns
                inColumns.Add(SqlScriptFormatter.FormatIdentifier(column.ColumnName));
            }
            
            // Begin the script (ie, INSERT INTO blah)
            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.AppendFormat(InsertScriptStart, AssociatedObjectMetadata.EscapedMultipartName);
            
            // Add the input columns (if there are any)
            if (inColumns.Count > 0)
            {
                string joinedInColumns = string.Join(", ", inColumns);
                queryBuilder.AppendFormat(InsertScriptColumns, joinedInColumns);
            }
            
            // Add the output columns (this will be empty if we are not building for command)
            if (outColumns.Count > 0)
            {
                string joinedOutColumns = string.Join(", ", outColumns);
                queryBuilder.AppendFormat(InsertScriptOut, joinedOutColumns);
            }
            
            // Add the input values (if there any) or use the default values
            if (inValues.Count > 0)
            {
                string joinedInValues = string.Join(", ", inValues);
                queryBuilder.AppendFormat(InsertScriptValues, joinedInValues);
            }
            else
            {
                queryBuilder.AppendFormat(InsertScriptDefault);
            }

            return new ScriptBuildResult
            {
                ScriptText = queryBuilder.ToString(),
                ScriptParameters = sqlParameters.ToArray()
            };
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

        private class ScriptBuildResult
        {
            public string ScriptText { get; set; }
            public SqlParameter[] ScriptParameters { get; set; }
        }
    }
}
