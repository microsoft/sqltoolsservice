namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary>
    /// Parameters for executing a query from a document open in the workspace
    /// </summary>
    public class ExecuteDocumentSelectionParams: ExecuteRequestParamsBase
    {
        /// <summary>
        /// The selection from the document
        /// </summary>
        public SelectionData QuerySelection { get; set; }
    }
}