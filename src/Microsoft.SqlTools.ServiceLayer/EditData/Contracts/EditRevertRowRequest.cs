//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters for the revert row request
    /// </summary>
    public class EditRevertRowParams : RowOperationParams
    {
    }

    /// <summary>
    /// Parameters to return upon successful revert of a row
    /// </summary>
    public class EditRevertRowResult
    {
        /// <summary>
        /// The row after the revert was applied, representing the original, unedited state
        /// </summary>
        public EditRow Row { get; set; }
    }

    public class EditRevertRowRequest
    {
        public static readonly
            RequestType<EditRevertRowParams, EditRevertRowResult> Type =
            RequestType<EditRevertRowParams, EditRevertRowResult>.Create("edit/revertRow");
    }
}
