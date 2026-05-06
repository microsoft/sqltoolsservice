//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts
{
    /// <summary>
    /// Parameters for canceling an Object Explorer operation.
    /// </summary>
    public class CancelObjectExplorerTaskParams
    {
        public string TaskId { get; set; }
        public string SessionId { get; set; }
        public string NodePath { get; set; }
    }

    /// <summary>
    /// Cancels an in-progress Object Explorer operation.
    /// </summary>
    public class CancelObjectExplorerTaskRequest
    {
        public static readonly
            RequestType<CancelObjectExplorerTaskParams, bool> Type =
            RequestType<CancelObjectExplorerTaskParams, bool>.Create("objectexplorer/cancel");
    }
}
