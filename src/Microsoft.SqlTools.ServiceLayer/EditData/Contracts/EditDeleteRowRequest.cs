//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    public class EditDeleteRowParams
    {
        /// <summary>
        /// Owner URI for the session to to add the delete row
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Internal ID of the row to delete
        /// </summary>
        public long RowId { get; set; }
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
