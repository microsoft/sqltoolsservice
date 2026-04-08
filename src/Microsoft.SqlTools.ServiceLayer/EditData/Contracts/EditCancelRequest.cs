//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters for the edit session cancel request
    /// </summary>
    public class EditCancelParams : SessionOperationParams
    {
    }

    /// <summary>
    /// Object to return upon successful cancellation of an edit session query
    /// </summary>
    public class EditCancelResult { }

    public class EditCancelRequest
    {
        public static readonly
            RequestType<EditCancelParams, EditCancelResult> Type =
            RequestType<EditCancelParams, EditCancelResult>.Create("edit/cancel");
    }
}
