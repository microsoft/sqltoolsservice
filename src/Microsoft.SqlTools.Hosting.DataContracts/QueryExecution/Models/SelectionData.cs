namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary> 
    /// Container class for a selection range from file 
    /// </summary>
    public class SelectionData
    {
        public int EndColumn { get; set; }

        public int EndLine { get; set; }

        public int StartColumn { get; set; }
        
        public int StartLine { get; set; }
    }
}