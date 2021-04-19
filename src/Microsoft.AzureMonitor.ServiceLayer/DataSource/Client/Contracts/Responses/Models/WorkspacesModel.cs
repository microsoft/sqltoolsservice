namespace Microsoft.AzureMonitor.ServiceLayer.DataSource.Client.Contracts.Responses.Models
{
    public class WorkspacesModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
        public string ResourceId { get; set; }
        public string[] TableGroups { get; set; }
        public string[] Tables { get; set; }
    }
}