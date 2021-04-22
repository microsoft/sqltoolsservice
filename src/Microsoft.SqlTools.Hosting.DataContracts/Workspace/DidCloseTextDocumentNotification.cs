using Microsoft.SqlTools.Hosting.DataContracts.Workspace.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Workspace
{
    public class DidCloseTextDocumentNotification
    {
        public static readonly
            EventType<DidCloseTextDocumentParams> Type =
                EventType<DidCloseTextDocumentParams>.Create("textDocument/didClose");
    }
}