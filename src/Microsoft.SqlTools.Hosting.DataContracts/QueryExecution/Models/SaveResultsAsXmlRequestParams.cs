namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary>
    /// Parameters to save results as XML
    /// </summary>
    public class SaveResultsAsXmlRequestParams: SaveResultsRequestParams
    {
        /// <summary>
        /// Formatting of the XML file
        /// </summary>
        public bool Formatted { get; set; }
        
        /// <summary>
        /// Encoding of the XML file
        /// </summary>
        public string Encoding { get; set; }
    }
}