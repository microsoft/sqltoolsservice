namespace Microsoft.SqlTools.Hosting.DataContracts.Connection.Models
{
    public class ConnectionSummary
    {
        /// <summary>
        /// Gets or sets the connection server name
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the connection database name
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the connection user name
        /// </summary>
        public string UserName { get; set; }
    }
}