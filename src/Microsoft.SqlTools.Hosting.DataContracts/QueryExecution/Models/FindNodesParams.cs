using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary>
    /// Parameters to the <see cref="FindNodesRequest"/>.
    /// </summary>
    public class FindNodesParams
    {
        /// <summary>
        /// The Id returned from a <see cref="CreateSessionRequest"/>. This
        /// is used to disambiguate between different trees. 
        /// </summary>
        public string SessionId { get; set; }

        public string Type { get; set; }

        public string Schema { get; set; }

        public string Name { get; set; }

        public string Database { get; set; }

        public List<string> ParentObjectNames { get; set; }
    }
}