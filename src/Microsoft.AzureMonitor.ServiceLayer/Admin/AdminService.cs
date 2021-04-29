using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AzureMonitor.ServiceLayer.Connection;
using Microsoft.SqlTools.Hosting.DataContracts.Admin;
using Microsoft.SqlTools.Hosting.DataContracts.Admin.Models;
using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;
using Microsoft.SqlTools.Hosting.Protocol;

namespace Microsoft.AzureMonitor.ServiceLayer.Admin
{
    public class AdminService
    {
        private static readonly Lazy<AdminService> _instance = new Lazy<AdminService>(() => new AdminService());
        public static AdminService Instance => _instance.Value;
        private static ConnectionService _connectionService;
        
        public void InitializeService(ServiceHost serviceHost, ConnectionService connectionService)
        {
            serviceHost.SetRequestHandler(GetDatabaseInfoRequest.Type, HandleGetDatabaseInfoRequest);
            _connectionService = connectionService;
        }

        private async Task HandleGetDatabaseInfoRequest(GetDatabaseInfoParams databaseParams, RequestContext<GetDatabaseInfoResponse> requestContext)
        {
            try
            {
                var info = new DatabaseInfo();
                Parallel.Invoke(() => info = GetDatabaseInfo(databaseParams));

                await requestContext.SendResult(new GetDatabaseInfoResponse()
                {
                    DatabaseInfo = info
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        private static DatabaseInfo GetDatabaseInfo(GetDatabaseInfoParams databaseParams)
        {
            var datasource = _connectionService.GetDataSource(databaseParams.OwnerUri);

            var metadata = datasource.Expand("/").Select(x => x.Metadata).FirstOrDefault();

            return new DatabaseInfo
            {
                Options =
                {
                    ["Name"] = metadata?.Name,
                    ["sizeInMB"] = "0"
                }
            };
        }
    }
}