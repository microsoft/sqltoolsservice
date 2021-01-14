using Microsoft.Kusto.ServiceLayer.Connection.Contracts;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public interface IDataSourceFactory
    {
        IDataSource Create(DataSourceType dataSourceType, ConnectionDetails connectionDetails, string ownerUri);
    }
}