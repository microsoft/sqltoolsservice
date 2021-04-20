namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    public class ExecuteStringParams : ExecuteRequestParamsBase
    {
        /// <summary>
        /// The query to execute
        /// </summary>
        public string Query { get; set; }
    }
}