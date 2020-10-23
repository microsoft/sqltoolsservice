using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public interface IDataSourceFactory
    {
        IDataSource Create(DataSourceType dataSourceType, string connectionString, string azureAccountToken, string ownerUri,
            RetryPolicy commandRetryPolicy);
    }
}