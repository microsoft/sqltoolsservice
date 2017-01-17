//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.QueryExecution;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    public abstract class RowEditBase
    {
        protected RowEditBase(long rowId, ResultSet associatedResultSet)
        {
            RowId = rowId;
            AssociatedResultSet = associatedResultSet;
        }

        #region Properties

        /// <summary>
        /// The internal ID of the row to which this edit applies, relative to the result set
        /// </summary>
        public long RowId { get; set; }

        /// <summary>
        /// The result set that is associated with this row edit
        /// </summary>
        public ResultSet AssociatedResultSet { get; private set; }

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
    }
}
