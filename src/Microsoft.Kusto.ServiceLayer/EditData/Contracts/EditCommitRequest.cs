//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters for a request to commit pending edit operations
    /// </summary>
    public class EditCommitParams : SessionOperationParams
    {
    }

    /// <summary>
    /// Parameters to return upon successful completion of commiting pending edit operations
    /// </summary>
    public class EditCommitResult
    {
    }

    public class EditCommitRequest
    {
        public static readonly
            RequestType<EditCommitParams, EditCommitResult> Type =
            RequestType<EditCommitParams, EditCommitResult>.Create("edit/commit");
    }
}
