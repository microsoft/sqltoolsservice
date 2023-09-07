//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Clear Pooled connectins request
    /// </summary>
    public class ClearPooledConnectionsRequest
    {
        public static readonly
            RequestType<object, bool> Type = RequestType<object, bool>.Create("connection/clearpooledconnections");
    }
}
