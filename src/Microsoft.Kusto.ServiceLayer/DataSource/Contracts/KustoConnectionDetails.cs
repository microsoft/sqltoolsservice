namespace Microsoft.Kusto.ServiceLayer.DataSource.Contracts
{
    public class KustoConnectionDetails
    {
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string UserToken { get; set; }
        public string ConnectionString { get; set; }
        public string AuthenticationType { get; set; }
    }
}