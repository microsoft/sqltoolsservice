//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Parameters for the Disconnect Request.
    /// </summary>
    public class DisconnectParams
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.         
        /// </summary>
        public string OwnerUri { get; set; }
    }

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
