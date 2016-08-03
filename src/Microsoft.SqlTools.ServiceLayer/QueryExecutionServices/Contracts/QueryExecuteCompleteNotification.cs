using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecutionServices.Contracts
{
    public class QueryExecuteCompleteNotification
    {
        /// <summary>
        /// Any messages that came back from the server during execution of the query
        /// </summary>
        public string[] Messages { get; set; }

        /// <summary>
        /// Whether or not the query was successful. True indicates errors, false indicates success
        /// </summary>
        public bool Error { get; set; }

        /// <summary>
        /// Summaries of the result sets that were returned with the query
        /// </summary>
        public ResultSetSummary[] ResultSetSummaries { get; set; }
    }
}
