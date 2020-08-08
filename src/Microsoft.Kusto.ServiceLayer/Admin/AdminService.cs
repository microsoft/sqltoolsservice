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
using Microsoft.Kusto.ServiceLayer.Hosting;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.Admin
{
    /// <summary>
    /// Admin task service class
    /// </summary>
    public class AdminService
    {
        private static readonly Lazy<AdminService> instance = new Lazy<AdminService>(() => new AdminService());

        private static ConnectionService connectionService = null;

        /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        internal AdminService()
        {
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (AdminService.connectionService == null)
                {
                    AdminService.connectionService = ConnectionService.Instance;
                }
                return AdminService.connectionService;
            }

            set
            {
                AdminService.connectionService = value;
            }
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static AdminService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(GetDatabaseInfoRequest.Type, HandleGetDatabaseInfoRequest);
        }

        /// <summary>
        /// Handle get database info request
        /// </summary>
        internal static async Task HandleGetDatabaseInfoRequest(
            GetDatabaseInfoParams databaseParams,
            RequestContext<GetDatabaseInfoResponse> requestContext)
        {
            try
            {
                Func<Task> requestHandler = async () =>
                {
                    ConnectionInfo connInfo;
                    AdminService.ConnectionServiceInstance.TryFindConnection(
                            databaseParams.OwnerUri,
                            out connInfo);
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
        internal static DatabaseInfo GetDatabaseInfo(ConnectionInfo connInfo)
        {
            if(!string.IsNullOrEmpty(connInfo.ConnectionDetails.DatabaseName)){
                ReliableDataSourceConnection connection;
                connInfo.TryGetConnection("Default", out connection);
                IDataSource dataSource = connection.GetUnderlyingConnection();
                DataSourceObjectMetadata objectMetadata = DataSourceFactory.CreateClusterMetadata(connInfo.ConnectionDetails.ServerName);

                List<DataSourceObjectMetadata> metadata = dataSource.GetChildObjects(objectMetadata, true).ToList();
                var databaseMetadata = metadata.Where(o => o.Name == connInfo.ConnectionDetails.DatabaseName);

                List<DatabaseInfo> databaseInfo = DataSourceFactory.ConvertToDatabaseInfo(databaseMetadata);

                return databaseInfo.ElementAtOrDefault(0);
            }

            return null;
        }
    }
}
