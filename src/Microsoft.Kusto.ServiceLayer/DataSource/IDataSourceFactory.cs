namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public interface IDataSourceFactory
    {
        IDataSource Create(DataSourceType dataSourceType, string connectionString, string azureAccountToken);
    }
}