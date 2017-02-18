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
    public class EditUpdateCellParams : RowOperationParams
    {
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
        /// Whether or not the cell value was modified from the provided string.
        /// If <c>true</c>, the client should replace the display value of the cell with the value
        /// in <see cref="NewValue"/>
        /// </summary>
        public bool HasCorrections { get; set; }

        /// <summary>
        /// Whether or not the cell was reverted with the change.
        /// If <c>true</c>, the client should unmark the cell as having an update and replace the
        /// display value of the cell with the value in <see cref="NewValue"/>
        /// </summary>
        public bool IsRevert { get; set; }

        /// <summary>
        /// Whether or not the new value of the cell is null
        /// </summary>
        public bool IsNull { get; set; }

        /// <summary>
        /// The new string value of the cell
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
