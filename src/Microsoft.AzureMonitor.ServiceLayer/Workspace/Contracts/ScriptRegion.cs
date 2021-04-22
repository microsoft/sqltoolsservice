namespace Microsoft.AzureMonitor.ServiceLayer.Workspace.Contracts
{
    /// <summary>
    /// Contains details about a specific region of text in script file.
    /// </summary>
    public class ScriptRegion
    {
        /// <summary>
        /// Gets the file path of the script file in which this region is contained.
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// Gets or sets the text that is contained within the region.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the starting line number of the region.
        /// </summary>
        public int StartLineNumber { get; set; }

        /// <summary>
        /// Gets or sets the starting column number of the region.
        /// </summary>
        public int StartColumnNumber { get; set; }

        /// <summary>
        /// Gets or sets the starting file offset of the region.
        /// </summary>
        public int StartOffset { get; set; }

        /// <summary>
        /// Gets or sets the ending line number of the region.
        /// </summary>
        public int EndLineNumber { get; set; }

        /// <summary>
        /// Gets or sets the ending column number of the region.
        /// </summary>
        public int EndColumnNumber { get; set; }

        /// <summary>
        /// Gets or sets the ending file offset of the region.
        /// </summary>
        public int EndOffset { get; set; }
    }
}