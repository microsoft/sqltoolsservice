﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts
{
    /// <summary>
    /// Information returned from a <see cref="ExpandRequest"/>.
    /// </summary>
    public class ExpandResponse
    {
        /// <summary>
        /// Unique ID to use when sending any requests for objects in the
        /// tree under the node
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Information describing the expanded nodes in the tree
        /// </summary>
        public NodeInfo[] Nodes { get; set; }

        /// <summary>
        /// Path identifying the node to expand. See <see cref="NodeInfo.NodePath"/> for details
        /// </summary>
        public string NodePath { get; set; }

        /// <summary>
        /// Error message returned from the engine for a object explorer expand failure reason, if any.
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Parameters to the <see cref="ExpandRequest"/>.
    /// </summary>
    public class ExpandParams
    {
        /// <summary>
        /// The Id returned from a <see cref="CreateSessionRequest"/>. This
        /// is used to disambiguate between different trees. 
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Path identifying the node to expand. See <see cref="NodeInfo.NodePath"/> for details
        /// </summary>
        public string NodePath { get; set; }

        /// <summary>
        /// Security token for AzureMFA authentication for refresing access token on connection.
        /// </summary>
        public SecurityToken? SecurityToken { get; set; }
    }

    /// <summary>
    /// A request to expand a 
    /// </summary>
    public class ExpandRequest
    {
        /// <summary>
        /// Returns children of a given node as a <see cref="NodeInfo"/> array.
        /// </summary>
        public static readonly
            RequestType<ExpandParams, bool> Type =
            RequestType<ExpandParams, bool>.Create("objectexplorer/expand");
    }

    /// <summary>
    /// Expand notification mapping entry 
    /// </summary>
    public class ExpandCompleteNotification
    {
        public static readonly
            EventType<ExpandResponse> Type =
            EventType<ExpandResponse>.Create("objectexplorer/expandCompleted");
    }
}
