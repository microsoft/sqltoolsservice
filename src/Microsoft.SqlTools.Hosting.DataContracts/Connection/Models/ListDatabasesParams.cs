namespace Microsoft.SqlTools.Hosting.DataContracts.Connection.Models
{
    /// <summary>
    /// Parameters for the List Databases Request.
    /// </summary>
    public class ListDatabasesParams
    {
        /// <summary>
        /// URI of the owner of the connection requesting the list of databases.
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// whether to include the details of the databases. Called by manage dashboard
        /// </summary>
        public bool? IncludeDetails { get; set; }
    }
}