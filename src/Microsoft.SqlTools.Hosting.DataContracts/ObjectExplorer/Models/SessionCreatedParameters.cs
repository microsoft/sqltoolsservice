namespace Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models
{
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
}