//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

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
        /// <param name="associatedResultSet">The result set that this update will be applied to</param>
        public RowUpdate(long rowId, ResultSet associatedResultSet) : base(rowId, associatedResultSet)
        {
            cellUpdates = new Dictionary<int, CellUpdate>();
            associatedRow = associatedResultSet.GetRow(rowId);
        }

        public override string GetScript()
        {
            throw new NotImplementedException();
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
        public override string SetCell(int columnId, string newValue)
        {
            // Validate the value and convert to object
            ValidateColumnIsUpdatable(columnId);            
            CellUpdate update = new CellUpdate(AssociatedResultSet.Columns[columnId], newValue);

            // If the value is the same as the old value, we shouldn't make changes
            if (update.Value == associatedRow[columnId].RawObject)
            {
                // Remove any pending change and stop processing this
                if (cellUpdates.ContainsKey(columnId))
                {
                    cellUpdates.Remove(columnId);
                }
                return null;
            }

            // The change is real, so set it
            cellUpdates[columnId] = update;
            return update.ValueAsString;
        }
    }
}
