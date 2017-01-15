//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters for the revert row request
    /// </summary>
    public class EditRevertRowParams
    {
        /// <summary>
        /// Owner URI for the session to revert the row in
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Internal ID of the row to revert
        /// </summary>
        public long RowId { get; set; }
    }

    /// <summary>
    /// Parameters to return upon successful revert of a row
    /// </summary>
    public class EditRevertRowResult
    {
    }

    public class EditRevertRowRequest
    {
        public static readonly
            RequestType<EditRevertRowParams, EditRevertRowResult> Type =
            RequestType<EditRevertRowParams, EditRevertRowResult>.Create("edit/revertRow");
    }
}
