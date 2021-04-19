namespace Microsoft.SqlTools.Hosting.DataContracts.Connection.Models
{
    /// <summary>
    /// Parameters for the List Databases Request.
    /// </summary>
    public class ChangeDatabaseParams
    {
        /// <summary>
        /// URI of the owner of the connection requesting the list of databases.
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// The database to change to
        /// </summary>
        public string NewDatabase { get; set; }
    }
}