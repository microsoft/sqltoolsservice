namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary>
    /// Parameters for executing a query from a document open in the workspace
    /// </summary>
    public class ExecuteDocumentStatementParams : ExecuteRequestParamsBase
    {
        /// <summary>
        /// Line in the document for the location of the SQL statement
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Column in the document for the location of the SQL statement
        /// </summary>
        public int Column { get; set; }
    }
}