//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Parameters for the Connect Request.
    /// </summary>
    public class ConnectParams
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.         
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Contains the required parameters to initialize a connection to a database.
        /// A connection will identified by its server name, database name and user name.
        /// This may be changed in the future to support multiple connections with different 
        /// connection properties to the same database.
        /// </summary>
        public ConnectionDetails Connection { get; set; }

        /// <summary>
        /// The type of this connection. By default, this is set to ConnectionType.Default.
        /// </summary>
        public string Type { get; set; } = ConnectionType.Default;

        /// <summary>
        /// The porpose of the connection to keep track of open connections
        /// </summary>
        public string Purpose { get; set; } = ConnectionType.GeneralConnection;
    }
}
