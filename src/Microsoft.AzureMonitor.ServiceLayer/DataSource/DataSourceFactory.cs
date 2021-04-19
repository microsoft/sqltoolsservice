using System.Composition;
using Microsoft.AzureMonitor.ServiceLayer.DataSource.Client;
using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;

namespace Microsoft.AzureMonitor.ServiceLayer.DataSource
{
    [Export(typeof(IDataSourceFactory))]
    public class DataSourceFactory : IDataSourceFactory
    {
        public MonitorDataSource Create(ConnectionDetails connectionDetails)
        {
            var httpClient = new MonitorClient(connectionDetails.ServerName, connectionDetails.AccountToken);
            return new MonitorDataSource(httpClient, connectionDetails.UserName);
        }
    }
}