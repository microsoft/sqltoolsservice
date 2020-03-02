//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility.SqlScriptFormatters;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    /// <summary>
    /// Base class for row edit operations. Provides basic information and helper functionality
    /// that all RowEdit implementations can use. Defines functionality that must be implemented
    /// in all child classes. Implements a custom IComparable to enable sorting by type of the edit
    /// and then by an overrideable
    /// </summary>
    public abstract class RowEditBase : IComparable<RowEditBase>
    {
        /// <summary>
        /// Internal parameterless constructor, required for mocking
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        protected internal RowEditBase() { }

        /// <summary>
        /// Base constructor for a row edit. Stores the state that should be available to all row
        /// edit implementations.
        /// </summary>
        /// <param name="rowId">The internal ID of the row that is being edited</param>
        /// <param name="associatedResultSet">The result set that will be updated</param>
        /// <param name="associatedMetadata">Metadata provider for the object to edit</param>
        protected RowEditBase(long rowId, ResultSet associatedResultSet, EditTableMetadata associatedMetadata)
        {
            if (!associatedMetadata.HasExtendedProperties)
            {
                throw new ArgumentException(SR.EditDataMetadataNotExtended);
            }

            RowId = rowId;
            AssociatedObjectMetadata = associatedMetadata;
            AssociatedResultSet = associatedResultSet;

            AssociatedObjectMetadata.Columns = EditTableMetadata.FilterColumnMetadata(AssociatedObjectMetadata.Columns, AssociatedResultSet.Columns);
        }

        #region Properties

        /// <summary>
        /// The internal ID of the row to which this edit applies, relative to the result set
        /// </summary>
        public long RowId { get; }

        /// <summary>
        /// The result set that is associated with this row edit
        /// </summary>
        public ResultSet AssociatedResultSet { get; }

        /// <summary>
        /// The metadata for the table this edit is associated to
        /// </summary>
        public EditTableMetadata AssociatedObjectMetadata { get; }

        /// <summary>
        /// Sort ID for a row edit. Ensures that when a collection of RowEditBase objects are
        /// sorted, the appropriate types are sorted to the top.
        /// </summary>
        protected abstract int SortId { get; }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Applies the changes to the associated result set
        /// </summary>
        /// <param name="dataReader">
        /// Data reader from execution of the command to commit the change to the db
        /// </param>
        public abstract Task ApplyChanges(DbDataReader dataReader);

        /// <summary>
        /// Gets a command that will commit the change to the db
        /// </summary>
        /// <param name="connection">The connection to associate the command to</param>
        /// <returns>Command to commit the change to the db</returns>
        public abstract DbCommand GetCommand(DbConnection connection);

        /// <summary>
        /// Generates a row that has the pending update applied. The dirty status of the row is
        /// reflected in the returned EditRow.
        /// </summary>
        /// <param name="cachedRow">The original, cached row values</param>
        /// <returns>An EditRow with the pending changes applied</returns>
        public abstract EditRow GetEditRow(DbCellValue[] cachedRow);

        /// <summary>
        /// Converts the row edit into a SQL statement
        /// </summary>
        /// <returns>A SQL statement</returns>
        public abstract string GetScript();

        /// <summary>
        /// Reverts a specific cell in row with pending edits
        /// </summary>
        /// <param name="columnId">Ordinal ID of the column to revert</param>
        /// <returns>String value of the original value of the cell</returns>
        public abstract EditRevertCellResult RevertCell(int columnId);

        /// <summary>
        /// Changes the value a cell in the row.
        /// </summary>
        /// <param name="columnId">Ordinal of the column in the row to update</param>
        /// <param name="newValue">The new value for the cell</param>
        /// <returns>The value of the cell after applying validation logic</returns>
        public abstract EditUpdateCellResult SetCell(int columnId, string newValue);

        #endregion

        #region Protected Helper Methods

        /// <summary>
        /// Performs validation of column ID and if column can be updated.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="columnId"/> is less than 0 or greater than the number of columns
        /// in the row
        /// </exception>
        /// <exception cref="InvalidOperationException">If the column is not updatable</exception>
        /// <param name="columnId">Ordinal of the column to update</param>
        protected void ValidateColumnIsUpdatable(int columnId)
        {
            // Sanity check that the column ID is within the range of columns
            if (columnId >= AssociatedResultSet.Columns.Length || columnId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columnId), SR.EditDataColumnIdOutOfRange);
            }

            DbColumnWrapper column = AssociatedResultSet.Columns[columnId];
            if (!column.IsUpdatable)
            {
                throw new InvalidOperationException(SR.EditDataColumnCannotBeEdited);
            }
        }

        /// <summary>
        /// Generates a WHERE clause that uses the key columns of the table to uniquely identity
        /// the row that will be updated.
        /// </summary>
        /// <param name="parameterize">
        /// Whether or not to generate a parameterized where clause. If <c>true</c> verbatim values
        /// will be replaced with paremeters (like @Param12). The parameters must be added to the
        /// SqlCommand used to execute the commit.
        /// </param>
        /// <returns>A <see cref="WhereClause"/> object</returns>
        protected WhereClause GetWhereClause(bool parameterize)
        {
            WhereClause output = new WhereClause();

            if (!AssociatedObjectMetadata.KeyColumns.Any())
            {
                throw new InvalidOperationException(SR.EditDataColumnNoKeyColumns);
            }

            IList<DbCellValue> row = AssociatedResultSet.GetRow(RowId);
            foreach (EditColumnMetadata col in AssociatedObjectMetadata.KeyColumns)
            {
                // Put together a clause for the value of the cell
                DbCellValue cellData = row[col.Ordinal];
                string cellDataClause;
                if (cellData.IsNull)
                {
                    cellDataClause = "IS NULL";
                }
                else
                {
                    if (parameterize)
                    {
                        // Add a parameter and parameterized clause component
                        // NOTE: We include the row ID to make sure the parameter is unique if
                        //       we execute multiple row edits at once.
                        string paramName = $"@Param{RowId}{col.Ordinal}";
                        if (cellData.RawObject is byte[] ||
                            col.DbColumn.DataTypeName.Equals("TEXT", StringComparison.OrdinalIgnoreCase) ||
                            col.DbColumn.DataTypeName.Equals("NTEXT", StringComparison.OrdinalIgnoreCase))
                        {
                            // Special cases for byte[] and TEXT/NTEXT types
                            cellDataClause = $"LIKE {paramName}";
                        }
                        else
                        {
                            cellDataClause = $"= {paramName}";
                        }
                        SqlParameter parameter = new SqlParameter(paramName, col.DbColumn.SqlDbType)
                        {
                            Value = cellData.RawObject
                        };
                        output.Parameters.Add(parameter);
                    }
                    else
                    {
                        // Add the clause component with the formatted value
                        cellDataClause = $"= {ToSqlScript.FormatValue(cellData, col.DbColumn)}";
                    }
                }

                string completeComponent = $"({col.EscapedName} {cellDataClause})";
                output.ClauseComponents.Add(completeComponent);
            }

            return output;
        }

        #endregion

        #region IComparable Implementation

        /// <summary>
        /// Compares a row edit against another row edit. If they are the same type, then we
        /// compare using an overrideable "same type" comparer. If they are different types, they
        /// are sorted by their sort indexes.
        ///
        /// In general, RowCreate and RowUpdates are sorted to the top. RowDeletes are sorted last.
        /// If there are ties, default behavior is to sort by row ID ascending.
        /// </summary>
        /// <param name="other">The other row edit to compare against</param>
        /// <returns>
        /// A positive value if this edit should go first, a negative value if the other edit
        /// should go first. 0 is returned if there is a tie.
        /// </returns>
        public int CompareTo(RowEditBase other)
        {
            // If the other is null, this one will come out on top
            if (other == null)
            {
                return 1;
            }

            // If types are the same, use the type's tiebreaking sorter
            if (GetType() == other.GetType())
            {
                return CompareToSameType(other);
            }

            // If the type's sort index is the same, use our tiebreaking sorter
            // If they are different, use that as the comparison
            int sortIdComparison = SortId.CompareTo(other.SortId);
            return sortIdComparison == 0
                ? CompareByRowId(other)
                : sortIdComparison;
        }

        /// <summary>
        /// Default behavior for sorting if the two compared row edits are the same type. Sorts
        /// by row ID ascending.
        /// </summary>
        /// <param name="rowEdit">The other row edit to compare against</param>
        protected virtual int CompareToSameType(RowEditBase rowEdit)
        {
            return CompareByRowId(rowEdit);
        }

        /// <summary>
        /// Compares two row edits by their row ID ascending.
        /// </summary>
        /// <param name="rowEdit">The other row edit to compare against</param>
        private int CompareByRowId(RowEditBase rowEdit)
        {
            return RowId.CompareTo(rowEdit.RowId);
        }

        #endregion

        /// <summary>
        /// Represents a WHERE clause that can be used for identifying a row in a table.
        /// </summary>
        protected class WhereClause
        {
            /// <summary>
            /// Constructs and initializes a new where clause
            /// </summary>
            public WhereClause()
            {
                Parameters = new List<DbParameter>();
                ClauseComponents = new List<string>();
            }

            /// <summary>
            /// SqlParameters used in a parameterized query. If this object was generated without
            /// parameterization, this will be an empty list
            /// </summary>
            public List<DbParameter> Parameters { get; }

            /// <summary>
            /// Strings that make up the WHERE clause, such as <c>"([col1] = 'something')"</c>
            /// </summary>
            public List<string> ClauseComponents { get; }

            /// <summary>
            /// Total text of the WHERE clause that joins all the components with AND
            /// </summary>
            public string CommandText => $"WHERE {string.Join(" AND ", ClauseComponents)}";
        }
    }
}
