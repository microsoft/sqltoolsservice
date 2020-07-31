namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    /// <summary>
    /// Database metadata information
    /// </summary>
    public class TableMetadata : DatabaseMetadata
    {
        public string DatabaseName { get; set; }
        public string Folder { get; set; }
    }
}