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

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    /// <summary>
    /// An update to apply to a row of a result set. This will generate an UPDATE statement.
    /// </summary>
    public sealed class RowUpdate : RowEditBase
    {
        private readonly Dictionary<int, CellUpdate> cellUpdates;
        private readonly IList<DbCellValue> associatedRow;

        /// <summary>
        /// Constructs a new RowUpdate to be added to the cache.
        /// </summary>
        /// <param name="rowId">Internal ID of the row that will be updated with this object</param>
        /// <param name="associatedResultSet">Result set for the rows of the object to update</param>
        /// <param name="associatedMetadata">Metadata provider for the object to update</param>
        public RowUpdate(long rowId, ResultSet associatedResultSet, IEditTableMetadata associatedMetadata)
            : base(rowId, associatedResultSet, associatedMetadata)
        {
            cellUpdates = new Dictionary<int, CellUpdate>();
            associatedRow = associatedResultSet.GetRow(rowId);
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
            DbCommand command = connection.CreateCommand();

            // Build the "SET" portion of the statement
            List<string> setComponents = new List<string>();
            foreach (var updateElement in cellUpdates)
            {
                string formattedColumnName = SqlScriptFormatter.FormatIdentifier(updateElement.Value.Column.ColumnName);
                string paramName = $"@Value{RowId}{updateElement.Key}";
                setComponents.Add($"{formattedColumnName} = {paramName}");
                SqlParameter parameter = new SqlParameter(paramName, updateElement.Value.Column.SqlDbType)
                {
                    Value = updateElement.Value.Value
                };
                command.Parameters.Add(parameter);
            }
            string setComponentsJoined = string.Join(", ", setComponents);

            // Build the "OUTPUT" portion of the statement
            var outColumns = from c in AssociatedResultSet.Columns
                             let formatted = SqlScriptFormatter.FormatIdentifier(c.ColumnName)
                             select $"inserted.{formatted}";
            string outColumnsJoined = string.Join(", ", outColumns);

            // Get the where clause
            WhereClause where = GetWhereClause(true);
            command.Parameters.AddRange(where.Parameters.ToArray());

            // Get the start of the statement
            string statementStart = GetStatementStart();

            // Put the whole #! together
            command.CommandText = $"{statementStart} SET {setComponentsJoined} OUTPUT {outColumnsJoined} {where.CommandText}";
            command.CommandType = CommandType.Text;
            return command;
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
                string formattedColumnName = SqlScriptFormatter.FormatIdentifier(cellUpdate.Column.ColumnName);
                string formattedValue = SqlScriptFormatter.FormatValue(cellUpdate.Value, cellUpdate.Column);
                return $"{formattedColumnName} = {formattedValue}";
            });
            string setClause = string.Join(", ", setComponents);

            // Get the where clause
            string whereClause = GetWhereClause(false).CommandText;

            // Get the start of the statement
            string statementStart = GetStatementStart();

            // Put the whole #! together
            return $"{statementStart} SET {setClause} {whereClause}";
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
                // Remove any pending change and stop processing this
                if (cellUpdates.ContainsKey(columnId))
                {
                    cellUpdates.Remove(columnId);
                }
                return new EditUpdateCellResult
                {
                    HasCorrections = false,
                    NewValue = associatedRow[columnId].DisplayValue,
                    IsRevert = true,
                    IsNull = associatedRow[columnId].IsNull
                };
            }

            // The change is real, so set it
            cellUpdates[columnId] = update;
            return new EditUpdateCellResult
            {
                HasCorrections = update.ValueAsString != newValue,
                NewValue = update.ValueAsString != newValue ? update.ValueAsString : null,
                IsNull = update.Value == DBNull.Value,
                IsRevert = false            // If we're in this branch, it is not a revert
            };
        }

        #endregion

        private string GetStatementStart()
        {
            return AssociatedObjectMetadata.IsMemoryOptimized
                ? $"UPDATE {AssociatedObjectMetadata.EscapedMultipartName} WITH (SNAPSHOT)"
                : $"UPDATE {AssociatedObjectMetadata.EscapedMultipartName}";
        }
    }
}
