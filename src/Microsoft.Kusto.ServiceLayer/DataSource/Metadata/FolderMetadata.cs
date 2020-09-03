namespace Microsoft.Kusto.ServiceLayer.DataSource.Metadata
{
    /// <summary>
    /// Folder metadata information
    /// </summary>
    public class FolderMetadata : DataSourceObjectMetadata
    {
        public DataSourceObjectMetadata ParentMetadata { get; set; }
    }
}