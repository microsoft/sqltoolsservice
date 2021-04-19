namespace Microsoft.SqlTools.Hosting.DataContracts.Connection.Models
{
    /// <summary>
    /// Message format for the list databases response
    /// </summary>
    public class ListDatabasesResponse
    {
        /// <summary>
        /// Gets or sets the list of database names.
        /// </summary>
        public string[] DatabaseNames { get; set; }

        /// <summary>
        /// Gets or sets the databases details.
        /// </summary>
        public DatabaseInfo[] Databases { get; set; }
    }
}