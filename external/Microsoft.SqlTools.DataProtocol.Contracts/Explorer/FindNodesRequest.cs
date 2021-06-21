//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.DataProtocol.Contracts.Connection;

namespace Microsoft.SqlTools.DataProtocol.Contracts.Explorer
{
    /// <summary>
    /// Information returned from a <see cref="FindNodesRequest"/>.
    /// </summary>
    public class FindNodesResponse
    {
        /// <summary>
        /// Information describing the matching nodes in the tree
        /// </summary>
        public List<NodeInfo> Nodes { get; set; }
    }

    /// <summary>
    /// Parameters to the <see cref="FindNodesRequest"/>.
    /// </summary>
    public class FindNodesParams
    {
        /// <summary>
        /// The Id returned from a <see cref="CreateSessionRequest"/>. This
        /// is used to disambiguate between different trees. 
        /// </summary>
        public string SessionId { get; set; }

        public string Type { get; set; }

        public string Schema { get; set; }

        public string Name { get; set; }

        public string Database { get; set; }

        public List<string> ParentObjectNames { get; set; }

    }

    /// <summary>
    /// TODO
    /// </summary>
    public class FindNodesRequest
    {
        public static readonly
            RequestType<FindNodesParams, FindNodesResponse> Type =
            RequestType<FindNodesParams, FindNodesResponse>.Create("explorer/findnodes");
    }
}
