namespace Microsoft.Kusto.ServiceLayer.DataSource.Metadata
{
    /// <summary>
    /// Column metadata information
    /// </summary>
    public class ColumnMetadata : TableMetadata
    {
        public string TableName { get; set; }
        public string DataType { get; set; }
    }
}