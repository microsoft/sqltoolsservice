using Microsoft.SqlTools.Hosting.DataContracts.Workspace.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Workspace
{
    public class DidOpenTextDocumentNotification
    {
        public static readonly
            EventType<DidOpenTextDocumentNotification> Type =
                EventType<DidOpenTextDocumentNotification>.Create("textDocument/didOpen");

        /// <summary>
        /// Gets or sets the opened document.
        /// </summary>
        public TextDocumentItem TextDocument { get; set; }
    }
}