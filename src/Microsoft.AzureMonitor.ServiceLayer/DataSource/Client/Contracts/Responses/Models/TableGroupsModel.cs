namespace Microsoft.AzureMonitor.ServiceLayer.DataSource.Client.Contracts.Responses.Models
{
    public class TableGroupsModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public string[] Tables { get; set; }
    }
}