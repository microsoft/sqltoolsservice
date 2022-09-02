namespace Microsoft.SqlTools.ServiceLayer.Rename.Requests
{
    public class RenameTableInfo
    {
        public string Server { get; set; }
        public string TableName { get; set; }
        public string Schema { get; set; }
        public string ConnectionString { get; set; }
        public string Id { get; set; }
        public bool IsNewTable { get; set; }
        public string OwnerUri { get; set; }

    }
}