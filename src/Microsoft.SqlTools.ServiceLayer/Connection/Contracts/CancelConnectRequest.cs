//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Cancel connect request mapping entry 
    /// </summary>
    public class CancelConnectRequest
    {
        public static readonly
            RequestType<CancelConnectParams, bool> Type =
            RequestType<CancelConnectParams, bool>.Create("connection/cancelconnect");
    }
}
