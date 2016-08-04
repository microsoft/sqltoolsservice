using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    public class QueryExecuteCompleteParams
    {
        /// <summary>
        /// URI for the editor that owns the query
        /// </summary>
        public string OwnerUri { get; set; }

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

    public class QueryExecuteCompleteEvent
    {
        public static readonly 
            EventType<QueryExecuteCompleteParams> Type =
            EventType<QueryExecuteCompleteParams>.Create("query/complete");
    }
}
