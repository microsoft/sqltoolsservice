using Microsoft.SqlTools.Hosting.DataContracts.Workspace.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Workspace
{
    public class DidChangeTextDocumentNotification
    {
        public static readonly
            EventType<DidChangeTextDocumentParams> Type = EventType<DidChangeTextDocumentParams>.Create("textDocument/didChange");
    }
}