//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Connect request mapping entry 
    /// </summary>
    public class ConnectionRequest
    {
        public static readonly
            RequestType<ConnectParams, ConnectResponse> Type =
            RequestType<ConnectParams, ConnectResponse>.Create("connection/connect");
    }
}
