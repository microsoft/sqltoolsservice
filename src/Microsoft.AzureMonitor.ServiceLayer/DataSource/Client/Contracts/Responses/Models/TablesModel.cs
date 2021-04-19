namespace Microsoft.AzureMonitor.ServiceLayer.DataSource.Client.Contracts.Responses.Models
{
    public class TablesModel
    {
        public string Id { get; set; }
        
        public string Name { get; set; }
        
        public string TimeSpanColumn { get; set; }
        
        public ColumnsModel[] Columns { get; set; }
    }
}