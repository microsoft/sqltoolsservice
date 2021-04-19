using System.Collections.Generic;

namespace Microsoft.SqlTools.Hosting.DataContracts.Connection.Models
{
    public class DatabaseInfo
    {
        /// <summary>
        /// Gets or sets the options
        /// </summary>
        public Dictionary<string, object> Options { get; set; } = new Dictionary<string, object>();
    }
}