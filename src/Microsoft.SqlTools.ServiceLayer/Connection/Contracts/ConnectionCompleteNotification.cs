//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Parameters to be sent back with a connection complete event
    /// </summary>
    public class ConnectionCompleteParams
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.         
        /// </summary>
        public string OwnerUri { get; set;  }

        /// <summary>
        /// A GUID representing a unique connection ID
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets any detailed connection error messages.
        /// </summary>
        public string Messages { get; set; }

        /// <summary>
        /// Error message returned from the engine for a connection failure reason, if any.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Error number returned from the engine for connection failure reason, if any.
        /// </summary>
        public int ErrorNumber { get; set; }

        /// <summary>
        /// Information about the connected server.
        /// </summary>
        public ServerInfo ServerInfo { get; set; }

        /// <summary>
        /// Gets or sets the actual Connection established, including Database Name
        /// </summary>
        public ConnectionSummary ConnectionSummary { get; set; }

        /// <summary>
        /// The type of connection that this notification is for
        /// </summary>
        public string Type { get; set; } = ConnectionType.Default;

        /// <summary>
        /// Gets or sets a boolean value indicates whether the current server version is supported by the service.
        /// </summary>
        public bool IsSupportedVersion { get; set; }

        /// <summary>
        /// Gets or sets the additional warning message about the unsupported server version.
        /// </summary>
        public string UnsupportedVersionMessage { get; set; }
    }

    /// <summary>
    /// ConnectionComplete notification mapping entry 
    /// </summary>
    public class ConnectionCompleteNotification
    {
        public static readonly 
            EventType<ConnectionCompleteParams> Type =
            EventType<ConnectionCompleteParams>.Create("connection/complete");
    }
}
