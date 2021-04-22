using System.Diagnostics;

namespace Microsoft.SqlTools.Hosting.DataContracts.Workspace.Models
{
    /// <summary>
    /// Defines a text document.
    /// </summary>
    [DebuggerDisplay("TextDocumentItem = {Uri}")]
    public class TextDocumentItem
    {
        /// <summary>
        /// Gets or sets the URI which identifies the path of the
        /// text document.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the language of the document
        /// </summary>
        public string LanguageId { get; set; }

        /// <summary>
        /// Gets or sets the version of the document
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the full content of the document.
        /// </summary>
        public string Text { get; set; }
    }
}