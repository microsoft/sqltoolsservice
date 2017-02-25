//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    /// <summary>
    /// Represents a row that should be added to the result set. Generates an INSERT statement.
    /// </summary>
    public sealed class RowCreate : RowEditBase
    {
        private const string InsertStatement = "INSERT INTO {0}({1}) OUTPUT inserted.* VALUES ({2})";

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

        protected override int SortId => 1;

        public override Task ApplyChanges(DbDataReader dataReader)
        {
            return AssociatedResultSet.AddRow(dataReader);
        }

        public override DbCommand GetCommand(DbConnection connection)
        {
            DbCommand command = connection.CreateCommand();
            command.CommandText = GetCommandText(command);
            return command;
        }

        /// <summary>
        /// Generates the INSERT INTO statement that will apply the row creation
        /// </summary>
        /// <returns>INSERT INTO statement</returns>
        public override string GetScript()
        {
            return GetCommandText(null);
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

        private string GetCommandText(DbCommand command)
        {
            List<string> columnNames = new List<string>();
            List<string> columnValues = new List<string>();

            // Build the column list and value list
            for (int i = 0; i < AssociatedResultSet.Columns.Length; i++)
            {
                DbColumnWrapper column = AssociatedResultSet.Columns[i];
                CellUpdate cell = newCells[i];

                // If the column is not updatable, then skip it
                if (!column.IsUpdatable)
                {
                    continue;
                }

                // If the cell doesn't have a value, but is updatable, don't try to create the script
                if (cell == null)
                {
                    throw new InvalidOperationException(SR.EditDataCreateScriptMissingValue);
                }

                // Add the column and the data to their respective lists
                columnNames.Add(SqlScriptFormatter.FormatIdentifier(column.ColumnName));
                if (command != null)
                {
                    // Add the parameterization to the list and add it to the command
                    string paramName = $"@Value{RowId}{i}";
                    columnValues.Add(paramName);
                    SqlParameter param = new SqlParameter(paramName, cell.Column.SqlDbType)
                    {
                        Value = cell.Value
                    };
                    command.Parameters.Add(param);
                }
                else
                {
                    // Format the value and add it to the list
                    columnValues.Add(SqlScriptFormatter.FormatValue(cell.Value, column));
                }
            }

            // Put together the components of the statement
            string joinedColumnNames = string.Join(", ", columnNames);
            string joinedColumnValues = string.Join(", ", columnValues);
            return string.Format(InsertStatement, AssociatedObjectMetadata.EscapedMultipartName, joinedColumnNames,
                joinedColumnValues);
        }
    }
}
