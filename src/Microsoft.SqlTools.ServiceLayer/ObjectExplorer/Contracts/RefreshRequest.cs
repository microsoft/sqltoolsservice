//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts
{
    /// <summary>
    /// Information returned from a <see cref="RefreshRequest"/>.
    /// </summary>
    public class RefreshResponse
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
    }

    /// <summary>
    /// Parameters to the <see cref="ExpandRequest"/>.
    /// </summary>
    public class RefreshParams
    {
        /// <summary>
        /// The Id returned from a <see cref="CreateSessionRequest"/>. This
        /// is used to disambiguate between different trees. 
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Path identifying the node to refresh. See <see cref="NodeInfo.NodePath"/> for details
        /// </summary>
        public string NodePath { get; set; }
    }

    /// <summary>
    /// A request to expand a 
    /// </summary>
    public class RefreshRequest
    {
        /// <summary>
        /// Returns children of a given node as a <see cref="NodeInfo"/> array.
        /// </summary>
        public static readonly
            RequestType<RefreshParams, RefreshResponse> Type =
            RequestType<RefreshParams, RefreshResponse>.Create("objectexplorer/refresh");
    }
}
