namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    /// <summary>
    /// Database metadata information
    /// </summary>
    public class DatabaseMetadata : DataSourceObjectMetadata
    {
        public string ClusterName { get; set; }
    }
}