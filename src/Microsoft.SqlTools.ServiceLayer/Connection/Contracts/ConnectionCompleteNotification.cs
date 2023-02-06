//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

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
        /// A GUID representing a unique connection ID, only populated if the connection was successful.
        /// </summary>
        public string? ConnectionId { get; set; }

        /// <summary>
        /// Additional optional detailed error messages, if an error occurred.
        /// </summary>
        public string? Messages { get; set; }

        /// <summary>
        /// Error message for the connection failure, if an error occured.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Error number returned from the engine or server host, if an error occurred.
        /// </summary>
        public int? ErrorNumber { get; set; }

        /// <summary>
        /// Information about the connected server, if the connection was successful.
        /// </summary>
        public ServerInfo? ServerInfo { get; set; }

        /// <summary>
        /// Information about the actual connection established, if the connection was successful.
        /// </summary>
        public ConnectionSummary? ConnectionSummary { get; set; }

        /// <summary>
        /// The type of connection that this notification is for
        /// </summary>
        public string Type { get; set; } = ConnectionType.Default;

        /// <summary>
        /// Whether the server version is supported
        /// </summary>
        public bool? IsSupportedVersion { get; set; }

        /// <summary>
        /// Additional optional message with details about why the version isn't supported.
        /// </summary>
        public string? UnsupportedVersionMessage { get; set; }
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
