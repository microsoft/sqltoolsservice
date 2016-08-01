//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ConnectionServices
{
    /// <summary>
    /// Message format for the initial connection request
    /// </summary>
    public class ConnectionDetails
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

        /// <summary>
        /// Gets or sets the connection password
        /// </summary>
        /// <returns></returns>
        public string Password { get; set; }
    }

    /// <summary>
    /// Message format for the connection result response
    /// </summary>
    public class ConnectionResult
    {
        /// <summary>
        /// Gets or sets the connection id
        /// </summary>
        public int ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets any connection error messages
        /// </summary>
        public string Messages { get; set; }
    }

    /// <summary>
    /// Connect request mapping entry 
    /// </summary>
    public class ConnectionRequest
    {
        public static readonly
            RequestType<ConnectionDetails, ConnectionResult> Type =
            RequestType<ConnectionDetails, ConnectionResult>.Create("connection/connect");
    }

}
