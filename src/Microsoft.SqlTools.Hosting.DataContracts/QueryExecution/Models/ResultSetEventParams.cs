namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary>
    /// Base class of parameters to return when a result set is available, updated or completed
    /// </summary>
    public class ResultSetEventParams
    {
        public ResultSetSummary ResultSetSummary { get; set; }

        public string OwnerUri { get; set; }
    }
}