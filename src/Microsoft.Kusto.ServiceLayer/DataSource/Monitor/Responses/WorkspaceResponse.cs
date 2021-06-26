using Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses.Models;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses
{
    public class WorkspaceResponse
    {
        public TableGroupsModel[] TableGroups { get; set; }
        public TablesModel[] Tables { get; set; }
        public WorkspacesModel[] Workspaces { get; set; }
    }
}