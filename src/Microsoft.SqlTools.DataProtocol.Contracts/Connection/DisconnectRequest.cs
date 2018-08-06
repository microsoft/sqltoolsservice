//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Contracts;

namespace Microsoft.SqlTools.DataProtocol.Contracts.Connection
{
    /// <summary>
    /// Disconnect request mapping entry 
    /// </summary>
    public class DisconnectRequest
    {
        public static readonly
            RequestType<DisconnectParams, bool> Type =
            RequestType<DisconnectParams, bool>.Create("connection/disconnect");
    }
}
