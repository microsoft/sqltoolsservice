namespace Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense
{
    public class ShowDatabasesResult
    {
        public string DatabaseName;
        public string PersistentStorage;
        public string Version;
        public bool IsCurrent;
        public string DatabaseAccessMode;
        public string PrettyName;
        public bool CurrentUserIsUnrestrictedViewer;
        public string DatabaseId;
    }
}