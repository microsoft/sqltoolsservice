namespace Microsoft.SqlTools.Hosting.DataContracts.Workspace.Models
{
    public class DidChangeConfigurationParams<TConfig>
    {
        public TConfig Settings { get; set; }
    }
}