namespace Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses.Models
{
    public class TableGroupsModel
    {
        public string DisplayName { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public string[] Tables { get; set; }
    }
}