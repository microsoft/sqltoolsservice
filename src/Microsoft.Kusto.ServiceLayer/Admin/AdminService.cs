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
        private async Task HandleGetDatabaseInfoRequest(
            GetDatabaseInfoParams databaseParams,
            RequestContext<GetDatabaseInfoResponse> requestContext)
        {
            try
            {
                Func<Task> requestHandler = async () =>
                {
                    _connectionService.TryFindConnection(databaseParams.OwnerUri, out var connInfo);
                    DatabaseInfo info = null;

                    if (connInfo != null)
                    {
                        info = GetDatabaseInfo(connInfo);
                    }

                    await requestContext.SendResult(new GetDatabaseInfoResponse()
                    {
                        DatabaseInfo = info
                    });
                };

                Task task = Task.Run(async () => await requestHandler()).ContinueWithOnFaulted(async t =>
                {
                    await requestContext.SendError(t.Exception.ToString());
                });

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
            
            ReliableDataSourceConnection connection;
            connInfo.TryGetConnection("Default", out connection);
            IDataSource dataSource = connection.GetUnderlyingConnection();
            DataSourceObjectMetadata objectMetadata =
                MetadataFactory.CreateClusterMetadata(connInfo.ConnectionDetails.ServerName);

            List<DataSourceObjectMetadata> metadata = dataSource.GetChildObjects(objectMetadata, true).ToList();
            var databaseMetadata = metadata.Where(o => o.Name == connInfo.ConnectionDetails.DatabaseName);

            List<DatabaseInfo> databaseInfo = MetadataFactory.ConvertToDatabaseInfo(databaseMetadata);

            return databaseInfo.ElementAtOrDefault(0);
        }
    }
}
