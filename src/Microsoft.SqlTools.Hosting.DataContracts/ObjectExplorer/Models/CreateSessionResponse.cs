namespace Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models
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
}