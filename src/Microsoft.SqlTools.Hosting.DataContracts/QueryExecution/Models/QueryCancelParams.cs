namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary>
    /// Parameters for the query cancellation request
    /// </summary>
    public class QueryCancelParams
    {
        public string OwnerUri { get; set; }
    }
}