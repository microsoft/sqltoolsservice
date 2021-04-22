using Microsoft.SqlTools.Hosting.DataContracts.Workspace.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Workspace
{
    public class DidChangeConfigurationNotification<TConfig> 
    {
        public static readonly
            EventType<DidChangeConfigurationParams<TConfig>> Type =
                EventType<DidChangeConfigurationParams<TConfig>>.Create("workspace/didChangeConfiguration");
    }
}