//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    /// <summary>
    /// Base class for row edit operations. Provides basic information and helper functionality
    /// that all RowEdit implementations can use. Defines functionality that must be implemented
    /// in all child classes.
    /// </summary>
    public abstract class RowEditBase
    {
        /// <summary>
        /// Base constructor for a row edit. Stores the state that should be available to all row
        /// edit implementations.
        /// </summary>
        /// <param name="rowId">The internal ID of the row that is being edited</param>
        /// <param name="associatedResultSet">The result set that will be updated</param>
        protected RowEditBase(long rowId, ResultSet associatedResultSet)
        {
            RowId = rowId;
            AssociatedResultSet = associatedResultSet;
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

        #endregion

        /// <summary>
        /// Converts the row edit into a SQL statement
        /// </summary>
        /// <returns>A SQL statement</returns>
        public abstract string GetScript();

        /// <summary>
        /// Changes the value a cell in the row.
        /// </summary>
        /// <param name="columnId">Ordinal of the column in the row to update</param>
        /// <param name="newValue">The new value for the cell</param>
        /// <returns>The value of the cell after applying validation logic</returns>
        public abstract string SetCell(int columnId, string newValue);

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
                // @TODO: Add to constants file
                throw new ArgumentOutOfRangeException(nameof(columnId), "Column ID must be in the range of columns for the query");
            }

            DbColumnWrapper column = AssociatedResultSet.Columns[columnId];
            if (!column.IsUpdatable)
            {
                // @TODO: Add to constants file
                throw new InvalidOperationException("Column cannot be edited");
            }
        }
    }
}
