//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Diagram.Contracts;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Diagram
{
    /// <summary>
    /// Main class for Diagram Service functionality
    /// </summary>
    public sealed class DiagramService
    {
        private static readonly Lazy<DiagramService> LazyInstance = new Lazy<DiagramService>(() => new DiagramService());

        public static DiagramService Instance => LazyInstance.Value;

        internal static Task DiagramModelTask { get; set; }

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
            serviceHost.SetRequestHandler(DiagramModelRequest.Type, HandleDiagramModelRequest);
            serviceHost.SetRequestHandler(DiagramPropertiesRequest.Type, HandleDiagramPropertiesRequest);
        }

        /// <summary>
        /// Handle a metadata query request
        /// </summary>        
        internal static async Task HandleDiagramModelRequest(
            DiagramRequestParams metadataParams,
            RequestContext<DiagramRequestResult> requestContext)
        {
            try
            {
                Func<Task> requestHandler = async () =>
                {
                    ConnectionInfo connInfo;
                    DiagramService.ConnectionServiceInstance.TryFindConnection(
                        metadataParams.OwnerUri,
                        out connInfo);

                    var metadata = new List<ObjectMetadata>();
                    if (connInfo != null)
                    {
                        using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo, "DiagramModel"))
                        {
                            switch(metadataParams.DiagramView)
                            {
                                case (DiagramObject.Schema):
                                    ReadMetadata(sqlConn, metadata);
                                    break;
                                case (DiagramObject.Table):
                                    // add another function to query the table
                                    // or add object type as parameter to existing
                                    // ReadMetada function to make the right query
                                    break;
                                case (DiagramObject.Database):
                                    break;
                                default:
                                    ReadMetadata(sqlConn, metadata);
                                    break;
                            }
                        }
                    }

                    await requestContext.SendResult(new DiagramRequestResult
                    {
                        Metadata = metadata.ToArray()
                    });
                };

                Task task = Task.Run(async () => await requestHandler()).ContinueWithOnFaulted(async t =>
                {
                    await requestContext.SendError(t.Exception.ToString());
                });
                
                DiagramModelTask = task;
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

         /// <summary>
        /// Handle a properties request
        /// </summary>        
        internal static async Task HandleDiagramPropertiesRequest(
            DiagramRequestParams metadataParams,
            RequestContext<DiagramRequestResult> requestContext)
        {
            try
            {
                Func<Task> requestHandler = async () =>
                {
                    ConnectionInfo connInfo;
                    DiagramService.ConnectionServiceInstance.TryFindConnection(
                        metadataParams.OwnerUri,
                        out connInfo);

                    var metadata = new List<ObjectMetadata>();
                    if (connInfo != null)
                    {
                        using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo, "DiagramProperties"))
                        {
                            switch(metadataParams.DiagramView)
                            {
                                case (DiagramObject.Schema):
                                    ReadMetadata(sqlConn, metadata);
                                    break;
                                case (DiagramObject.Table):
                                    // add another function to query the table
                                    // or add object type as parameter to existing
                                    // ReadMetada function to make the right query
                                    break;
                                case (DiagramObject.Database):
                                    break;
                                default:
                                    ReadMetadata(sqlConn, metadata);
                                    break;
                            }
                        }
                    }

                    await requestContext.SendResult(new DiagramRequestResult
                    {
                        Metadata = metadata.ToArray()
                    });
                };

                Task task = Task.Run(async () => await requestHandler()).ContinueWithOnFaulted(async t =>
                {
                    await requestContext.SendError(t.Exception.ToString());
                });
                
                DiagramModelTask = task;
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

        internal static void ReadMetadata(SqlConnection sqlConn, List<ObjectMetadata> metadata)
        {
            string sql =
                @"SELECT s.name AS schema_name, o.[name] AS object_name, o.[type] AS object_type
                  FROM sys.all_objects o
                    INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
                  WHERE o.[type] IN ('P','V','U','AF','FN','IF','TF') ";

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
                        else if (objectType == "AF" || objectType == "FN" || objectType == "IF" || objectType == "TF")
                        {
                            metadataType = MetadataType.Function;
                            metadataTypeName = "UserDefinedFunction";
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
