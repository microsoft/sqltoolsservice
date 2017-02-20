//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;

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
        public RowDelete(long rowId, ResultSet associatedResultSet, IEditTableMetadata associatedMetadata)
            : base(rowId, associatedResultSet, associatedMetadata)
        {
        }

        /// <summary>
        /// Generates a DELETE statement to delete this row
        /// </summary>
        /// <returns>String of the DELETE statement</returns>
        public override string GetScript()
        {
            string formatString = AssociatedObjectMetadata.IsMemoryOptimized ? DeleteMemoryOptimizedStatement : DeleteStatement;
            return string.Format(CultureInfo.InvariantCulture, formatString,
                AssociatedObjectMetadata.EscapedMultipartName, GetWhereClause(false).CommandText);
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
    }
}
