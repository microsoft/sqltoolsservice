namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary>
    /// Parameters for executing a query from a provided string
    /// </summary>
    public class SimpleExecuteParams
    {
        /// <summary>
        /// The string to execute
        /// </summary>
        public string QueryString { get; set; }

        /// <summary>
        /// The owneruri to get connection from
        /// </summary>
        public string OwnerUri { get; set; }
    }
}