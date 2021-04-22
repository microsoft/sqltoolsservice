namespace Microsoft.SqlTools.Hosting.DataContracts.Workspace.Models
{
    public class DidCloseTextDocumentParams
    {
        /// <summary>
        /// Gets or sets the closed document.
        /// </summary>
        public TextDocumentItem TextDocument { get; set; }
    }
}