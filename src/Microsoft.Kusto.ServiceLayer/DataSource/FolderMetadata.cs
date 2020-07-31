namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    /// <summary>
    /// Folder metadata information
    /// </summary>
    public class FolderMetadata : DataSourceObjectMetadata
    {
        public DataSourceObjectMetadata ParentMetadata { get; set; }
    }
}