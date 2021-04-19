namespace Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models
{
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
}