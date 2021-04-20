namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary>
    /// Basic parameters that are required for executing a query
    /// </summary>
    public class ExecuteRequestParamsBase
    {
        /// <summary>
        /// URI for the editor that is asking for the query execute
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Execution plan options
        /// </summary>
        public ExecutionPlanOptions ExecutionPlanOptions { get; set; }

        /// <summary>
        /// Flag to get full column schema via additional queries.
        /// </summary>
        public bool GetFullColumnSchema { get; set; }
    }
}