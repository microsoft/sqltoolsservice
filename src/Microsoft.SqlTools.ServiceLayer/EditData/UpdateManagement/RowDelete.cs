//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    /// <summary>
    /// Represents a row that should be deleted. This will generate a DELETE statement
    /// </summary>
    public sealed class RowDelete : RowEditBase
    {
        private const string DeleteStatement = "DELETE FROM {0} {1}";
        private const string DeleteMemoryOptimizedStatement = "DELETE FROM {0} WITH(SNAPSHOT) {1}";

        /// <summary>
        /// Constructs a new RowDelete object
        /// </summary>
        /// <param name="rowId">Internal ID of the row to be deleted</param>
        /// <param name="associatedResultSet">Result set that is being edited</param>
        /// <param name="associatedMetadata">Improved metadata of the object being edited</param>
        public RowDelete(long rowId, ResultSet associatedResultSet, EditTableMetadata associatedMetadata)
            : base(rowId, associatedResultSet, associatedMetadata)
        {
        }

        /// <summary>
        /// Sort ID for a RowDelete object. Setting to 2 ensures that these are the LAST changes
        /// to be committed
        /// </summary>
        protected override int SortId => 2;

        /// <summary>
        /// Applies the changes to the associated result set after successfully executing the
        /// change on the database
        /// </summary>
        /// <param name="dataReader">
        /// Reader returned from the execution of the command to insert a new row. Should NOT
        /// contain any rows.
        /// </param>
        public override Task ApplyChanges(DbDataReader dataReader)
        {
            // Take the result set and remove the row from it
            AssociatedResultSet.RemoveRow(RowId);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Generates a command for deleting the selected row
        /// </summary>
        /// <returns></returns>
        public override DbCommand GetCommand(DbConnection connection)
        {
            Validate.IsNotNull(nameof(connection), connection);

            // Return a SqlCommand with formatted with the parameters from the where clause
            WhereClause where = GetWhereClause(true);
            DbCommand command = connection.CreateCommand();
            string commandText = "";
            if (!this.checkIfValid(connection, where))
            {
                commandText = " DECLARE @error NVARCHAR(100) = N'INVALID DELETE: rows with duplicate or text values are not currently supported';" + Environment.NewLine +
                                                           " RAISERROR (@error, 16, 1)";
            }
            else
            {
                commandText = GetCommandText(where.CommandText);
            }

            command.CommandText = commandText;
            command.Parameters.AddRange(where.Parameters.ToArray());
            return command;
        }

        private Boolean checkIfValid(DbConnection connection, WhereClause where)
        {
            string formatString = "SELECT COUNT(*) FROM {0} {1}";
            string whereText = where.CommandText;
            string commandText = string.Format(CultureInfo.InvariantCulture, formatString,
                AssociatedObjectMetadata.EscapedMultipartName, whereText);
            int number = 1;
            using (DbCommand command = connection.CreateCommand()){
                command.CommandText = commandText;
                command.Parameters.AddRange(where.Parameters.ToArray());
                using(DbDataReader reader = command.ExecuteReader()){
                    if(reader != null && reader.HasRows && reader.Read()){
                        number = (int) reader.GetValue(0);
                    }
                };
                command.Parameters.Clear();
            }
            if(number != 1){
                return false;
            }
            return true;
        }

        /// <summary>
        /// Generates a edit row that represents a row pending deletion. All the original cells are
        /// intact but the state is dirty.
        /// </summary>
        /// <param name="cachedRow">Original, cached cell contents</param>
        /// <returns>EditRow that is pending deletion</returns>
        public override EditRow GetEditRow(DbCellValue[] cachedRow)
        {
            Validate.IsNotNull(nameof(cachedRow), cachedRow);

            return new EditRow
            {
                Id = RowId,
                Cells = cachedRow.Select(cell => new EditCell(cell, true)).ToArray(),
                State = EditRow.EditRowState.DirtyDelete
            };
        }

        /// <summary>
        /// Generates a DELETE statement to delete this row
        /// </summary>
        /// <returns>String of the DELETE statement</returns>
        public override string GetScript()
        {
            return GetCommandText(GetWhereClause(false).CommandText);
        }

        /// <summary>
        /// This method should not be called. A cell cannot be reverted on a row that is pending
        /// deletion.
        /// </summary>
        /// <param name="columnId">Ordinal of the column to update</param>
        public override EditRevertCellResult RevertCell(int columnId)
        {
            throw new InvalidOperationException(SR.EditDataDeleteSetCell);
        }

        /// <summary>
        /// This method should not be called. A cell cannot be updated on a row that is pending
        /// deletion.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always thrown</exception>
        /// <param name="columnId">Ordinal of the column to update</param>
        /// <param name="newValue">New value for the cell</param>
        public override EditUpdateCellResult SetCell(int columnId, string newValue)
        {
            throw new InvalidOperationException(SR.EditDataDeleteSetCell);
        }

        protected override int CompareToSameType(RowEditBase rowEdit)
        {
            // We want to sort by row ID *IN REVERSE* to make sure we delete from the bottom first.
            // If we delete from the top first, it will change IDs, making all subsequent deletes
            // off by one or more!
            return RowId.CompareTo(rowEdit.RowId) * -1;
        }

        private string GetCommandText(string whereText)
        {
            string formatString = AssociatedObjectMetadata.IsMemoryOptimized
                ? DeleteMemoryOptimizedStatement
                : DeleteStatement;

            return string.Format(CultureInfo.InvariantCulture, formatString,
                AssociatedObjectMetadata.EscapedMultipartName, whereText);
        }
    }
}
