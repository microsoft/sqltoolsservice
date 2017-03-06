//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    public class EditRevertCellParams : RowOperationParams
    {
        public int ColumnId { get; set; }
    }

    public class EditRevertCellResult
    {
        public string NewValue { get; set; }
    }

    public class EditRevertCellRequest
    {
        public static readonly
            RequestType<EditRevertCellParams, EditRevertCellResult> Type =
            RequestType<EditRevertCellParams, EditRevertCellResult>.Create("edit/revertCell");
    }
}
