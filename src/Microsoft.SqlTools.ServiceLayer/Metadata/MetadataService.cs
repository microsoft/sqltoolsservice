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
using Microsoft.SqlTools.ServiceLayer.Utility;

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
            serviceHost.SetRequestHandler(TableMetadataRequest.Type, HandleGetTableRequest);
            serviceHost.SetRequestHandler(ViewMetadataRequest.Type, HandleGetViewRequest);
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
                Func<Task> requestHandler = async () =>
                {
                    ConnectionInfo connInfo;
                    MetadataService.ConnectionServiceInstance.TryFindConnection(
                        metadataParams.OwnerUri,
                        out connInfo);

                    var metadata = new List<ObjectMetadata>();
                    if (connInfo != null)
                    {
                        using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo, "Metadata"))
                        {
                            ReadMetadata(sqlConn, metadata);
                        }
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
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        /// <summary>
        /// Handle a table metadata query request
        /// </summary>        
        internal static async Task HandleGetTableRequest(
            TableMetadataParams metadataParams,
            RequestContext<TableMetadataResult> requestContext)
        {
            await HandleGetTableOrViewRequest(metadataParams, "table", requestContext);
        }

        /// <summary>
        /// Handle a view metadata query request
        /// </summary>        
        internal static async Task HandleGetViewRequest(
            TableMetadataParams metadataParams,
            RequestContext<TableMetadataResult> requestContext)
        {
            await HandleGetTableOrViewRequest(metadataParams, "view", requestContext);
        }

        /// <summary>
        /// Handle a table pr view metadata query request
        /// </summary>        
        private static async Task HandleGetTableOrViewRequest(
            TableMetadataParams metadataParams,
            string objectType,
            RequestContext<TableMetadataResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                MetadataService.ConnectionServiceInstance.TryFindConnection(
                    metadataParams.OwnerUri,
                    out connInfo);

                ColumnMetadata[] metadata = null;
                if (connInfo != null) 
                {
                    using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo, "Metadata"))
                    {
                        TableMetadata table = new SmoMetadataFactory().GetObjectMetadata(
                            sqlConn, metadataParams.Schema,
                            metadataParams.ObjectName, objectType);
                        metadata = table.Columns;
                    }
                }

                await requestContext.SendResult(new TableMetadataResult
                {
                    Columns = metadata    
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        internal static bool IsSystemDatabase(string database)
        {
            // compare against master for now
            return string.Compare("master", database, StringComparison.OrdinalIgnoreCase) == 0;
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
                  WHERE (o.[type] = 'P' OR o.[type] = 'V' OR o.[type] = 'U') ";
   
            if (!IsSystemDatabase(sqlConn.Database))
            {
                sql += @"AND o.is_ms_shipped != 1 ";
            }
            
            sql += @"ORDER BY object_type, schema_name, object_name";

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
                        string metadataTypeName;
                        if (objectType.StartsWith("V"))
                        {
                            metadataType = MetadataType.View;
                            metadataTypeName = "View";
                        }
                        else if (objectType.StartsWith("P"))
                        {
                            metadataType = MetadataType.SProc;
                            metadataTypeName = "StoredProcedure";
                        }
                        else
                        {
                            metadataType = MetadataType.Table;
                            metadataTypeName = "Table";
                        }

                        metadata.Add(new ObjectMetadata
                        {
                            MetadataType = metadataType,
                            MetadataTypeName = metadataTypeName,
                            Schema = schemaName,
                            Name = objectName
                        });
                    }
                }
            }
        }
    }
}
