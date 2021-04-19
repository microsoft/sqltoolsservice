using Microsoft.AzureMonitor.ServiceLayer.DataSource.Client.Contracts.Responses.Models;

namespace Microsoft.AzureMonitor.ServiceLayer.DataSource.Client.Contracts.Responses
{
    public class WorkspaceResponse
    {
        public TableGroupsModel[] TableGroups { get; set; }
        public TablesModel[] Tables { get; set; }
        public WorkspacesModel[] Workspaces { get; set; }
    }
}