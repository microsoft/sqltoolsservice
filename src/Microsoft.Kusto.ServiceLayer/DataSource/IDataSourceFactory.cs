using Microsoft.SqlTools.Hosting.Contracts.Connection;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public interface IDataSourceFactory
    {
        IDataSource Create(DataSourceType dataSourceType, ConnectionDetails connectionDetails, string ownerUri);
    }
}