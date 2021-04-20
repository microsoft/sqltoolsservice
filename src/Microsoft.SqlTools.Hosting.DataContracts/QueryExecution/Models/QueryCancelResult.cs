namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary>
    /// Parameters to return as the result of a query dispose request
    /// </summary>
    public class QueryCancelResult
    {
        /// <summary>
        /// Any error messages that occurred during disposing the result set. Optional, can be set
        /// to null if there were no errors.
        /// </summary>
        public string Messages { get; set; }
    }
}