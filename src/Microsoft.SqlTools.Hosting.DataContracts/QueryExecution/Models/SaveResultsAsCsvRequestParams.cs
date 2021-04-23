namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary>
    /// Parameters to save results as CSV
    /// </summary>
    public class SaveResultsAsCsvRequestParams: SaveResultsRequestParams
    {
        /// <summary>
        /// Include headers of columns in CSV
        /// </summary>
        public bool IncludeHeaders { get; set; }

        /// <summary>
        /// Delimiter for separating data items in CSV
        /// </summary>
        public string Delimiter { get; set; }

        /// <summary>
        /// either CR, CRLF or LF to seperate rows in CSV
        /// </summary>
        public string LineSeperator { get; set; }

        /// <summary>
        /// Text identifier for alphanumeric columns in CSV
        /// </summary>
        public string TextIdentifier { get; set; }

        /// <summary>
        /// Encoding of the CSV file
        /// </summary>
        public string Encoding { get; set; }
    }
}