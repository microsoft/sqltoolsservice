namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    public class SubsetResult
    {
        /// <summary>
        /// The requested subset of results. Optional, can be set to null to indicate an error
        /// </summary>
        public ResultSetSubset ResultSubset { get; set; }
    }
}