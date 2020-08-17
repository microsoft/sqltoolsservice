//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.Contracts
{
    /// <summary>
    /// Information returned from a <see cref="CreateSessionRequest"/>.
    /// Contains success information, a <see cref="SessionId"/> to be used when
    /// requesting expansion of nodes, and a root node to display for this area.
    /// </summary>
    public class CreateSessionResponse
    {
        /// <summary>
        /// Unique ID to use when sending any requests for objects in the
        /// tree under the node
        /// </summary>
        public string SessionId { get; set; }

    }

    /// <summary>
    /// Information returned from a <see cref="CreateSessionRequest"/>.
    /// Contains success information, a <see cref="SessionId"/> to be used when
    /// requesting expansion of nodes, and a root node to display for this area.
    /// </summary>
    public class SessionCreatedParameters
    {
        /// <summary>
        /// Boolean indicating if the connection was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Unique ID to use when sending any requests for objects in the
        /// tree under the node
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Information describing the base node in the tree
        /// </summary>
        public NodeInfo RootNode { get; set; }


        /// <summary>
        /// Error message returned from the engine for a object explorer session failure reason, if any.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
    /// <summary>
    /// Establishes an Object Explorer tree session for a specific connection.
    /// This will create a connection to a specific server or database, register
    /// it for use in the 
    /// </summary>
    public class CreateSessionRequest
    {
        public static readonly
            RequestType<ConnectionDetails, CreateSessionResponse> Type =
            RequestType<ConnectionDetails, CreateSessionResponse>.Create("kusto/objectexplorer/createsession");
    }

    /// <summary>
    /// Session notification mapping entry 
    /// </summary>
    public class CreateSessionCompleteNotification
    {
        public static readonly
            EventType<SessionCreatedParameters> Type =
            EventType<SessionCreatedParameters>.Create("kustoo/bjectexplorer/sessioncreated");
    }
}
