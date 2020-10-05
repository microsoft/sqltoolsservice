//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Metadata.Contracts;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;

namespace Microsoft.Kusto.ServiceLayer.Metadata
{
    /// <summary>
    /// Main class for Metadata Service functionality
    /// </summary>
    public sealed class MetadataService
    {
        private static readonly Lazy<MetadataService> LazyInstance = new Lazy<MetadataService>();

        public static MetadataService Instance => LazyInstance.Value;

        private static ConnectionService _connectionService;
        
        internal Task MetadataListTask { get; private set; }

        /// <summary>
        /// Initializes the Metadata Service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        /// <param name="connectionService"></param>
        public void InitializeService(IProtocolEndpoint serviceHost, ConnectionService connectionService)
        {
            _connectionService = connectionService;
            serviceHost.SetRequestHandler(MetadataListRequest.Type, HandleMetadataListRequest);
        }

        /// <summary>
        /// Handle a metadata query request
        /// </summary>        
        internal async Task HandleMetadataListRequest(
            MetadataQueryParams metadataParams,
            RequestContext<MetadataQueryResult> requestContext)
        {
            try
            {
                Func<Task> requestHandler = async () =>
                {
                    ConnectionInfo connInfo;
                    _connectionService.TryFindConnection(metadataParams.OwnerUri, out connInfo);

                    var metadata = new List<ObjectMetadata>();
                    if (connInfo != null)
                    {
                        ReliableDataSourceConnection connection;
                        connInfo.TryGetConnection("Default", out connection);
                        IDataSource dataSource = connection.GetUnderlyingConnection();

                        DataSourceObjectMetadata objectMetadata = MetadataFactory.CreateClusterMetadata(connInfo.ConnectionDetails.ServerName);
                        DataSourceObjectMetadata databaseMetadata = MetadataFactory.CreateDatabaseMetadata(objectMetadata, connInfo.ConnectionDetails.DatabaseName);

                        IEnumerable<DataSourceObjectMetadata> databaseChildMetadataInfo = dataSource.GetChildObjects(databaseMetadata, true);
                        metadata = MetadataFactory.ConvertToObjectMetadata(databaseChildMetadataInfo);
                    }

                    await requestContext.SendResult(new MetadataQueryResult
                    {
                        Metadata = metadata.ToArray()
                    });
                };

                Task task = Task.Run(async () => await requestHandler()).ContinueWithOnFaulted(async t =>
                {
                    await requestContext.SendError(t.Exception.ToString());
                });
                MetadataListTask = task;
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        
    }
}
