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
        private const string InsertStart = "INSERT INTO {0}({1})";
        private const string InsertCompleteScript = "{0} VALUES ({1})";
        private const string InsertCompleteOutput = "{0} OUTPUT {1} VALUES ({2})";

        private readonly CellUpdate[] newCells;

        /// <summary>
        /// Creates a new Row Creation edit to the result set
        /// </summary>
        /// <param name="rowId">Internal ID of the row that is being created</param>
        /// <param name="associatedResultSet">The result set for the rows in the table we're editing</param>
        /// <param name="associatedMetadata">The metadata for table we're editing</param>
        public RowCreate(long rowId, ResultSet associatedResultSet, IEditTableMetadata associatedMetadata)
            : base(rowId, associatedResultSet, associatedMetadata)
        {
            newCells = new CellUpdate[associatedResultSet.Columns.Length];
        }

        /// <summary>
        /// Sort ID for a RowCreate object. Setting to 1 ensures that these are the first changes 
        /// to be committed
        /// </summary>
        protected override int SortId => 1;

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

            // Process all the columns. Add the column to the output columns, add updateable
            // columns to the input parameters
            List<string> outColumns = new List<string>();
            List<string> inColumns = new List<string>();
            DbCommand command = connection.CreateCommand();
            for (int i = 0; i < AssociatedResultSet.Columns.Length; i++)
            {
                DbColumnWrapper column = AssociatedResultSet.Columns[i];
                CellUpdate cell = newCells[i];

                // Add the column to the output
                outColumns.Add($"inserted.{SqlScriptFormatter.FormatIdentifier(column.ColumnName)}");

                // Skip columns that cannot be updated
                if (!column.IsUpdatable)
                {
                    continue;
                }

                // If we're missing a cell, then we cannot continue
                if (cell == null)
                {
                    throw new InvalidOperationException(SR.EditDataCreateScriptMissingValue);
                }

                // Create a parameter for the value and add it to the command
                // Add the parameterization to the list and add it to the command
                string paramName = $"@Value{RowId}{i}";
                inColumns.Add(paramName);
                SqlParameter param = new SqlParameter(paramName, cell.Column.SqlDbType)
                {
                    Value = cell.Value
                };
                command.Parameters.Add(param);
            }
            string joinedInColumns = string.Join(", ", inColumns);
            string joinedOutColumns = string.Join(", ", outColumns);
            
            // Get the start clause
            string start = GetTableClause();

            // Put the whole #! together
            command.CommandText = string.Format(InsertCompleteOutput, start, joinedOutColumns, joinedInColumns);
            command.CommandType = CommandType.Text;
                
            return command;
        }

        /// <summary>
        /// Generates the INSERT INTO statement that will apply the row creation
        /// </summary>
        /// <returns>INSERT INTO statement</returns>
        public override string GetScript()
        {
            // Process all the cells, and generate the values
            List<string> values = new List<string>();
            for (int i = 0; i < AssociatedResultSet.Columns.Length; i++)
            {
                DbColumnWrapper column = AssociatedResultSet.Columns[i];
                CellUpdate cell = newCells[i];

                // Skip columns that cannot be updated
                if (!column.IsUpdatable)
                {
                    continue;
                }

                // If we're missing a cell, then we cannot continue
                if (cell == null)
                {
                    throw new InvalidOperationException(SR.EditDataCreateScriptMissingValue);
                }

                // Format the value and add it to the list
                values.Add(SqlScriptFormatter.FormatValue(cell.Value, column));
            }
            string joinedValues = string.Join(", ", values);

            // Get the start clause
            string start = GetTableClause();

            // Put the whole #! together
            return string.Format(InsertCompleteScript, start, joinedValues);
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
            EditUpdateCellResult eucr = new EditUpdateCellResult
            {
                HasCorrections = update.ValueAsString != newValue,
                NewValue = update.ValueAsString != newValue ? update.ValueAsString : null,
                IsNull = update.Value == DBNull.Value,
                IsRevert = false            // Editing cells of new rows cannot be reverts
            }; 
            return eucr;
        }

        #endregion

        private string GetTableClause()
        {
            // Get all the columns that will be provided
            var inColumns = from c in AssociatedResultSet.Columns
                            where c.IsUpdatable
                            select SqlScriptFormatter.FormatIdentifier(c.ColumnName);

            // Package it into a single INSERT statement starter
            string inColumnsJoined = string.Join(", ", inColumns);
            return string.Format(InsertStart, AssociatedObjectMetadata.EscapedMultipartName, inColumnsJoined);
        }
    }
}
