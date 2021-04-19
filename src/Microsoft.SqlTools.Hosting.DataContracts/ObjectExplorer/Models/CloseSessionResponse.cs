namespace Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models
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
}