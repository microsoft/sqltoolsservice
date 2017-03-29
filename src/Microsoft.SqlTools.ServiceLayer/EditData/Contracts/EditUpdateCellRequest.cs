//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

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
    public class EditUpdateCellResult : EditCellResult
    {
    }

    public class EditUpdateCellRequest
    {
        public static readonly
            RequestType<EditUpdateCellParams, EditUpdateCellResult> Type =
            RequestType<EditUpdateCellParams, EditUpdateCellResult>.Create("edit/updateCell");
    }
}
