//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters for the update cell request
    /// </summary>
    public class EditUpdateCellParams
    {
        /// <summary>
        /// Owner URI for the session to apply update to
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Internal ID of the row to update
        /// </summary>
        public long RowId { get; set; }

        /// <summary>
        /// Internal ID of the column to update
        /// </summary>
        public int ColumnId { get; set; }

        /// <summary>
        /// String representation of the value to assign to the cell
        /// </summary>
        public string NewValue { get; set; }
    }

    /// <summary>
    /// Parameters to return upon successful update of the cell
    /// </summary>
    public class EditUpdateCellResult
    {
        /// <summary>
        /// Whether or not the cell value was modified from the provided string
        /// </summary>
        public bool HasCorrections { get; set; }

        /// <summary>
        /// If <see cref="HasCorrections"/> is <c>true</c>, this property will be a string
        /// representation of the updated cell value.
        /// </summary>
        public string NewValue { get; set; }
    }

    public class EditUpdateCellRequest
    {
        public static readonly
            RequestType<EditUpdateCellParams, EditUpdateCellResult> Type =
            RequestType<EditUpdateCellParams, EditUpdateCellResult>.Create("edit/updateCell");
    }
}
