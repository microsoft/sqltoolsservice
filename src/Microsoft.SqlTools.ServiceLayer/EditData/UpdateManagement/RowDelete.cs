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
    /// An error indicating that a delete action will delete multiple rows.
    /// </summary>
    public class DeleteError : Exception
    {
        public DeleteError()
        {
        }

        public DeleteError(string message)
            : base(message)
        {
        }

        public DeleteError(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    /// Represents a row that should be deleted. This will generate a DELETE statement
    /// </summary>
    public sealed class RowDelete : RowEditBase
    {
        private const string DeleteStatement = "DELETE FROM {0} {1}";
        private const string DeleteMemoryOptimizedStatement = "DELETE FROM {0} WITH(SNAPSHOT) {1}";
        private const string VerifyStatement = "SELECT COUNT (*) FROM ";

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
            string commandText = GetCommandText(where.CommandText);
            string verifyText = GetVerifyText(where.CommandText);
            if (checkWhereDuplicate(where, verifyText, connection))
            {
                throw new DeleteError("This action will delete more than one row!");
            }

            DbCommand command = connection.CreateCommand();
            command.CommandText = commandText;
            command.Parameters.AddRange(where.Parameters.ToArray());

            return command;
        }

        /// <summary>
        /// Runs a query using the where clause to determine if duplicates are found (causes issues when deleting).
        /// </summary>
        private bool checkWhereDuplicate(WhereClause where, string input, DbConnection connection)
        {
            bool verifyStatus = false;
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = input;
                command.Parameters.AddRange(where.Parameters.ToArray());
                using (DbDataReader reader = command.ExecuteReader())
                {
                    try
                    {
                        while (reader.Read())
                        {
                            if (reader.GetInt32(0) != 1)
                            {
                                verifyStatus = true;
                            }
                        }
                        reader.Close();
                    }
                    catch
                    {
                        //Likely means there was nothing found that matched the query.
                    }
                }
            }
            return verifyStatus;
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

        private string GetVerifyText(string whereText)
        {
            return $"{VerifyStatement}{AssociatedObjectMetadata.EscapedMultipartName}{whereText}";;
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
