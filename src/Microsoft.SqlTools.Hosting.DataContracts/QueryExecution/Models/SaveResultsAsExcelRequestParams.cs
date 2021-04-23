namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary>
    /// Parameters to save results as Excel
    /// </summary>
    public class SaveResultsAsExcelRequestParams : SaveResultsRequestParams
    {
        /// <summary>
        /// Include headers of columns in Excel 
        /// </summary>
        public bool IncludeHeaders { get; set; }
    }
}