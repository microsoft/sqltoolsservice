namespace Microsoft.Kusto.ServiceLayer.DataSource.Models
{
    public class FunctionInfo
    {
        public string Name { get; set; }
        public string Parameters { get; set; }
        public string Body { get; set; }
        public string Folder { get; set; }
        public string DocString { get; set; }
    }
}