//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters for identifying a row to mark for deletion
    /// </summary>
    public class EditDeleteRowParams : RowOperationParams
    {
    }

    /// <summary>
    /// Parameters to return upon successfully adding row delete to update cache
    /// </summary>
    public class EditDeleteRowResult
    {
    }

    public class EditDeleteRowRequest
    {
        public static readonly
            RequestType<EditDeleteRowParams, EditDeleteRowResult> Type =
            RequestType<EditDeleteRowParams, EditDeleteRowResult>.Create("edit/deleteRow");
    }
}
