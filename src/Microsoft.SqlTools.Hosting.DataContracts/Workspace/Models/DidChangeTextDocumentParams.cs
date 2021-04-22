namespace Microsoft.SqlTools.Hosting.DataContracts.Workspace.Models
{
    public class DidChangeTextDocumentParams
    {
        /// <summary>
        /// Gets or sets the changed document.
        /// </summary>
        public VersionedTextDocumentIdentifier TextDocument { get; set; } 

        /// <summary>
        /// Gets or sets the list of changes to the document content.
        /// </summary>
        public TextDocumentChangeEvent[] ContentChanges { get; set; }
    }
}