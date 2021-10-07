//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.Kusto.ServiceLayer.Admin.Contracts;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.DataSource;

namespace Microsoft.Kusto.ServiceLayer.Admin
{
    /// <summary>
    /// Admin task service class
    /// </summary>
    public class AdminService
    {
        private static readonly Lazy<AdminService> _instance = new Lazy<AdminService>(() => new AdminService());

        private IConnectionManager _connectionManager;

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static AdminService Instance => _instance.Value;

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(IProtocolEndpoint serviceHost, IConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
            serviceHost.SetRequestHandler(GetDatabaseInfoRequest.Type, HandleGetDatabaseInfoRequest);
        }

        /// <summary>
        /// Handle get database info request
        /// </summary>
        public async Task HandleGetDatabaseInfoRequest(GetDatabaseInfoParams databaseParams, RequestContext<GetDatabaseInfoResponse> requestContext)
        {
            try
            {
                var infoResponse = await Task.Run(() =>
                {
                    DatabaseInfo info = null;
                    if (_connectionManager.TryGetValue(databaseParams.OwnerUri, out var connInfo))
                    {
                        info = GetDatabaseInfo(connInfo);
                    }

                    return new GetDatabaseInfoResponse {DatabaseInfo = info};
                });
                
                await requestContext.SendResult(infoResponse);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex);
            }
        }

        /// <summary>
        /// Return database info for a specific database
        /// </summary>
        /// <param name="connInfo"></param>
        /// <returns></returns>
        private DatabaseInfo GetDatabaseInfo(ConnectionInfo connInfo)
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
