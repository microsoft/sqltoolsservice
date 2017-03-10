//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Metadata
{
    /// <summary>
    /// Main class for Metadata Service functionality
    /// </summary>
    public sealed class MetadataService
    {
        private static readonly Lazy<MetadataService> LazyInstance = new Lazy<MetadataService>(() => new MetadataService());

        public static MetadataService Instance => LazyInstance.Value;

        private static ConnectionService connectionService = null;        

         /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }

        /// <summary>
        /// Initializes the Metadata Service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        /// <param name="context"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(MetadataListRequest.Type, HandleMetadataListRequest);
        }

        internal static async Task HandleMetadataListRequest(
            MetadataQueryParams metadataParams,
            RequestContext<MetadataQueryResult> requestContext)
        {
            ConnectionInfo connInfo;
            MetadataService.ConnectionServiceInstance.TryFindConnection(
                metadataParams.OwnerUri,
                out connInfo);

            var tables = new List<string>();

            if (connInfo != null) 
            {
                try
                {                 
                    // increase the connection timeout to at least 30 seconds and and build connection string
                    // enable PersistSecurityInfo to handle issues in SMO where the connection context is lost in reconnections
                    int? originalTimeout = connInfo.ConnectionDetails.ConnectTimeout;
                    bool? originalPersistSecurityInfo = connInfo.ConnectionDetails.PersistSecurityInfo;
                    connInfo.ConnectionDetails.ConnectTimeout = Math.Max(30, originalTimeout ?? 0);
                    connInfo.ConnectionDetails.PersistSecurityInfo = true;
                    string connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
                    connInfo.ConnectionDetails.ConnectTimeout = originalTimeout;
                    connInfo.ConnectionDetails.PersistSecurityInfo = originalPersistSecurityInfo;

                    // open a dedicated binding server connection
                    SqlConnection sqlConn = new SqlConnection(connectionString); 
                    sqlConn.Open();

                    // populate the binding context to work with the SMO metadata provider
                    ServerConnection serverConn = new ServerConnection(sqlConn);
                     // Reuse existing connection
                    Server server = new Server(serverConn);
                    // The default database name is the database name of the server connection
                    string dbName = serverConn.DatabaseName;
                    if (connInfo != null)
                    {
                        // If there is a query DbConnection, use that connection to get the database name
                        // This is preferred since it has the most current database name (in case of database switching)
                        DbConnection connection;
                        if (connInfo.TryGetConnection(Connection.ConnectionType.Query, out connection))
                        {
                            if (!string.IsNullOrEmpty(connection.Database))
                            {
                                dbName  = connection.Database;
                            }
                        }
                    }
                    
                    var database = new Database(server, dbName);
                    database.Refresh();

                    foreach (Table table in database.Tables)
                    {
                        tables.Add(table.Schema + "." + table.Name);
                    }                      
                }
                catch (Exception)
                {
                }
                finally
                {
                }
            } 
                            
            await requestContext.SendResult(new MetadataQueryResult()
            {
                OwnerUri = metadataParams.OwnerUri,
                Tables = tables.ToArray()
            });
        }
    }
}
