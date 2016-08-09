//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
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
        public string OwnerUri { get; set;  }
        /// <summary>
        /// Contains the required parameters to initialize a connection to a database.
        /// A connection will identified by its server name, database name and user name.
        /// This may be changed in the future to support multiple connections with different 
        /// connection properties to the same database.
        /// </summary>
        public ConnectionDetails Connection { get; set; }
    }

    /// <summary>
    /// Message format for the connection result response
    /// </summary>
    public class ConnectResponse
    {
        /// <summary>
        /// A GUID representing a unique connection ID
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets any connection error messages
        /// </summary>
        public string Messages { get; set; }
    }

    /// <summary>
    /// Provides high level information about a connection.
    /// </summary>
    public class ConnectionSummary
    {
        /// <summary>
        /// Gets or sets the connection server name
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the connection database name
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the connection user name
        /// </summary>
        public string UserName { get; set; }
    }

    /// <summary>
    /// Message format for the initial connection request
    /// </summary>
    public class ConnectionDetails : ConnectionSummary
    {
        /// <summary>
        /// Gets or sets the connection password
        /// </summary>
        /// <returns></returns>
        public string Password { get; set; }

        // TODO Handle full set of properties
    }

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
