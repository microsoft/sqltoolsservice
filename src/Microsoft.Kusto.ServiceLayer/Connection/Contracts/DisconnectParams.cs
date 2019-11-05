//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

        /// <summary>
        /// The type of connection we are disconnecting. If null, we will disconnect all connections.
        /// connections. 
        /// </summary>
        public string Type { get; set; }
    }
}
