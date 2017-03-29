//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters for the cell revert request
    /// </summary>
    public class EditRevertCellParams : RowOperationParams
    {
        public int ColumnId { get; set; }
    }

    /// <summary>
    /// Parameters to return upon successful revert of the cell
    /// </summary>
    public class EditRevertCellResult
    {
        /// <summary>
        /// The cell after the revert was applied
        /// </summary>
        public EditCell RevertedCell { get; set; }

        /// <summary>
        /// Whether or not the row is dirty after the revert has been applied
        /// </summary>
        public bool IsRowDirty { get; set; }
    }

    public class EditRevertCellRequest
    {
        public static readonly
            RequestType<EditRevertCellParams, EditRevertCellResult> Type =
            RequestType<EditRevertCellParams, EditRevertCellResult>.Create("edit/revertCell");
    }
}
