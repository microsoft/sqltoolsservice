namespace Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense
{
    public class ShowDatabaseSchemaResult
    {
        public string DatabaseName;
        public string TableName;
        public string ColumnName;
        public string ColumnType;
        public bool IsDefaultTable;
        public bool IsDefaultColumn;
        public string PrettyName;
        public string Version;
        public string Folder;
        public string DocName;
    }
}