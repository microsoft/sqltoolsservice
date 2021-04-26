using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;

namespace Microsoft.SqlTools.Hosting.DataContracts.Admin.Models
{
    /// <summary>
    /// Response object for get database info
    /// </summary>
    public class GetDatabaseInfoResponse
    {
        /// <summary>
        /// The object containing the database info
        /// </summary>
        public DatabaseInfo DatabaseInfo { get; set; }
    }
}