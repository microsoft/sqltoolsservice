//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts
{
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
        /// Path identifying the node to expand. See <see cref="NodeInfo.NodePath"/> for details
        /// </summary>
        public string[] NodePath { get; set; }
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
            RequestType<ExpandParams, NodeInfo[]> Type =
            RequestType<ExpandParams, NodeInfo[]>.Create("objectexplorer/refresh");
    }
}
