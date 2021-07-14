//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.Kusto.ServiceLayer.Admin.Contracts;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.Admin
{
    /// <summary>
    /// Admin task service class
    /// </summary>
    public class AdminService
    {
        private static readonly Lazy<AdminService> _instance = new Lazy<AdminService>(() => new AdminService());

        private static ConnectionService _connectionService;

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static AdminService Instance => _instance.Value;

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost, ConnectionService connectionService)
        {
            serviceHost.SetRequestHandler(GetDatabaseInfoRequest.Type, HandleGetDatabaseInfoRequest);
            _connectionService = connectionService;
        }

        /// <summary>
        /// Handle get database info request
        /// </summary>
        private async Task HandleGetDatabaseInfoRequest(GetDatabaseInfoParams databaseParams, RequestContext<GetDatabaseInfoResponse> requestContext)
        {
            try
            {
                var infoResponse = await Task.Run(() =>
                {
                    DatabaseInfo info = null;
                    if (_connectionService.TryFindConnection(databaseParams.OwnerUri, out var connInfo))
                    {
                        info = GetDatabaseInfo(connInfo);
                    }

                    return new GetDatabaseInfoResponse {DatabaseInfo = info};
                });
                
                await requestContext.SendResult(infoResponse);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        /// <summary>
        /// Return database info for a specific database
        /// </summary>
        /// <param name="connInfo"></param>
        /// <returns></returns>
        public DatabaseInfo GetDatabaseInfo(ConnectionInfo connInfo)
        {
            if (string.IsNullOrEmpty(connInfo.ConnectionDetails.DatabaseName))
            {
                return null;
            }
            
            connInfo.TryGetConnection(ConnectionType.Default, out ReliableDataSourceConnection connection);
            IDataSource dataSource = connection.GetUnderlyingConnection();
            return dataSource.GetDatabaseInfo(connInfo.ConnectionDetails.ServerName, connInfo.ConnectionDetails.DatabaseName);
        }
    }
}
