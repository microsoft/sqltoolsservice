namespace Microsoft.Kusto.ServiceLayer.DataSource.Metadata
{
    /// <summary>
    /// Database metadata information
    /// </summary>
    public class DatabaseMetadata : DataSourceObjectMetadata
    {
        public string ClusterName { get; set; }
    }
}