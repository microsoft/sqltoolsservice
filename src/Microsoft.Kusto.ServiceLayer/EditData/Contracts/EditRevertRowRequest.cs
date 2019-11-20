//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.EditData.Contracts
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
    }

    public class EditRevertRowRequest
    {
        public static readonly
            RequestType<EditRevertRowParams, EditRevertRowResult> Type =
            RequestType<EditRevertRowParams, EditRevertRowResult>.Create("edit/revertRow");
    }
}
