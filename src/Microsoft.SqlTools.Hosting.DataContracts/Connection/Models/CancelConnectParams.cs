namespace Microsoft.SqlTools.Hosting.DataContracts.Connection.Models
{
    /// <summary>
    /// Parameters for the Cancel Connect Request.
    /// </summary>
    public class CancelConnectParams
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.         
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// The type of connection we are trying to cancel
        /// </summary>
        public string Type { get; set; } = ConnectionType.Default;
    }
}