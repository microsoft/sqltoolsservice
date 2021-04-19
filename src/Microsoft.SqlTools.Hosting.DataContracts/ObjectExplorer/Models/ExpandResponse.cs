namespace Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models
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
}