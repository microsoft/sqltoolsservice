//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.Contracts
{
    /// <summary>
    /// Information returned from a <see cref="CloseSessionRequest"/>.
    /// Contains success information, a <see cref="SessionId"/> to be used when
    /// requesting closing an existing session.
    /// </summary>
    public class CloseSessionResponse
    {
        /// <summary>
        /// Boolean indicating if the session was closed successfully 
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Unique ID to use when sending any requests for objects in the
        /// tree under the node
        /// </summary>
        public string SessionId { get; set; }
    }

    /// <summary>
    /// Parameters to the <see cref="CloseSessionRequest"/>.
    /// </summary>
    public class CloseSessionParams
    {
        /// <summary>
        /// The Id returned from a <see cref="CreateSessionRequest"/>. This
        /// is used to disambiguate between different trees. 
        /// </summary>
        public string SessionId { get; set; }
    }

    /// <summary>
    /// Information returned when a session is disconnected.
    /// Contains success information and a <see cref="SessionId"/>
    /// </summary>
    public class SessionDisconnectedParameters
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
        /// Error message returned from the engine for a object explorer session failure reason, if any.
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Establishes an Object Explorer tree session for a specific connection.
    /// This will create a connection to a specific server or database, register
    /// it for use in the 
    /// </summary>
    public class CloseSessionRequest
    {
        public static readonly
            RequestType<CloseSessionParams, CloseSessionResponse> Type =
            RequestType<CloseSessionParams, CloseSessionResponse>.Create("kusto/objectexplorer/closesession");
    }
    
    /// <summary>
    /// Session disconnected notification
    /// </summary>
    public class SessionDisconnectedNotification
    {
        public static readonly
            EventType<SessionDisconnectedParameters> Type =
            EventType<SessionDisconnectedParameters>.Create("kusto/objectexplorer/sessiondisconnected");
    }
}
