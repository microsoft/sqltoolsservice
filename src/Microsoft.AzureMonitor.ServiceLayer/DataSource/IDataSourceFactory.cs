using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;

namespace Microsoft.AzureMonitor.ServiceLayer.DataSource
{
    public interface IDataSourceFactory
    {
        MonitorDataSource Create(ConnectionDetails connectionDetails);
    }
}