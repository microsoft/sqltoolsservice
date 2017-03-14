//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
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

        /// <summary>
        /// Handle a metadata query request
        /// </summary>        
        internal static async Task HandleMetadataListRequest(
            MetadataQueryParams metadataParams,
            RequestContext<MetadataQueryResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                MetadataService.ConnectionServiceInstance.TryFindConnection(
                    metadataParams.OwnerUri,
                    out connInfo);

                var metadata = new List<ObjectMetadata>();
                if (connInfo != null) 
                {                    
                    SqlConnection sqlConn = OpenMetadataConnection(connInfo);
                    ReadMetadata(sqlConn, metadata);
                }

                await requestContext.SendResult(new MetadataQueryResult()
                {
                    Metadata = metadata.ToArray()
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        /// <summary>
        /// Create a SqlConnection to use for querying metadata
        /// </summary>
        internal static SqlConnection OpenMetadataConnection(ConnectionInfo connInfo)
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
                return sqlConn;
            }
            catch (Exception)
            {
            }
            
            return null;
        }

        /// <summary>
        /// Read metadata for the current connection
        /// </summary>
        internal static void ReadMetadata(SqlConnection sqlConn, List<ObjectMetadata> metadata)
        {
            string sql = 
                @"SELECT s.name AS schema_name, o.[name] AS object_name, o.[type] AS object_type
                  FROM sys.all_objects o
                    INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                  WHERE o.is_ms_shipped != 1
                    AND (o.[type] = 'P' OR o.[type] = 'V' OR o.[type] = 'U')
                  ORDER BY object_type, schema_name, object_name";

            using (SqlCommand sqlCommand = new SqlCommand(sql, sqlConn))
            {
                using (var reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var schemaName = reader[0] as string;
                        var objectName = reader[1] as string;
                        var objectType = reader[2] as string;

                        MetadataType metadataType;
                        if (objectType.StartsWith("V"))
                        {
                            metadataType = MetadataType.View;
                        }
                        else if (objectType.StartsWith("P"))
                        {
                            metadataType = MetadataType.SProc;
                        }
                        else
                        {
                            metadataType = MetadataType.Table;
                        }

                        metadata.Add(new ObjectMetadata
                        {
                            MetadataType = metadataType,
                            Schema = schemaName,
                            Name = objectName
                        });
                    }
                }
            }
        }        
    }
}
