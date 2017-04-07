using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

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
            RequestType<ExpandParams, ExpandResponse> Type =
            RequestType<ExpandParams, ExpandResponse>.Create("objectexplorer/expand");
    }
}
