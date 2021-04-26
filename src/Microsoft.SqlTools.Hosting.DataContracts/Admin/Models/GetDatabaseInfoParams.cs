namespace Microsoft.SqlTools.Hosting.DataContracts.Admin.Models
{
    /// <summary>
    /// Params for a get database info request
    /// </summary>
    public class GetDatabaseInfoParams
    {
        /// <summary>
        /// Uri identifier for the connection to get the database info for
        /// </summary>
        public string OwnerUri { get; set; }
    }
}