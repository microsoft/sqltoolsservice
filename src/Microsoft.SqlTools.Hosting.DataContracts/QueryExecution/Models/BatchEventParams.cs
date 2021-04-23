namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary>
    /// Parameters to be sent back as part of a batch start or complete event to indicate that a
    /// batch of a query started or completed.
    /// </summary>
    public class BatchEventParams
    {
        /// <summary>
        /// Summary of the batch that just completed
        /// </summary>
        public BatchSummary BatchSummary { get; set; }

        /// <summary>
        /// URI for the editor that owns the query
        /// </summary>
        public string OwnerUri { get; set; }
    }
}