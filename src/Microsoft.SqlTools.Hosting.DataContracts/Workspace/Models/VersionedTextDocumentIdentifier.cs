namespace Microsoft.SqlTools.Hosting.DataContracts.Workspace.Models
{
    /// <summary>
    /// Define a specific version of a text document
    /// </summary>
    public class VersionedTextDocumentIdentifier : TextDocumentIdentifier
    {
        /// <summary>
        /// Gets or sets the Version of the changed text document 
        /// </summary>
        public int Version { get; set; }
    }
}