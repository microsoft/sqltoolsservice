//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.SqlCore.Metadata;
using Microsoft.SqlTools.Utility;

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
                connectionService ??= ConnectionService.Instance;
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
            serviceHost.SetRequestHandler(MetadataListRequest.Type, HandleMetadataListRequest, true);
            serviceHost.SetRequestHandler(TableMetadataRequest.Type, HandleGetTableRequest, true);
            serviceHost.SetRequestHandler(ViewMetadataRequest.Type, HandleGetViewRequest, true);
            serviceHost.SetEventHandler(GenerateServerContextualizationNotification.Type, HandleGenerateServerContextualizationNotification, true);
            serviceHost.SetRequestHandler(GetServerContextualizationRequest.Type, HandleGetServerContextualizationRequest, true);
        }

        /// <summary>
        /// Handle a metadata query request
        /// </summary>        
        internal async Task HandleMetadataListRequest(
            MetadataQueryParams metadataParams,
            RequestContext<MetadataQueryResult> requestContext)
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
            MetadataListTask = task;
        }

        internal Task MetadataListTask { get; set; }

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
        /// Handles the event for generating server contextualization scripts.
        /// </summary>
        internal static Task HandleGenerateServerContextualizationNotification(GenerateServerContextualizationParams contextualizationParams,
            EventContext eventContext)
        {
            _ = Task.Factory.StartNew(() =>
            {
                GenerateServerContextualization(contextualizationParams);
            },
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskScheduler.Default);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Generates the contextualization scripts for a server. The generated context is in the form of create scripts for
        /// database objects like tables and views.
        /// </summary>
        /// <param name="contextualizationParams">The contextualization parameters.</param>
        internal static void GenerateServerContextualization(GenerateServerContextualizationParams contextualizationParams)
        {
            MetadataService.ConnectionServiceInstance.TryFindConnection(contextualizationParams.OwnerUri, out ConnectionInfo connectionInfo);

            if (connectionInfo != null)
            {
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionInfo, "metadata"))
                {
                    // If scripts have been generated within the last 30 days then there isn't a need to go through the process
                    // of generating scripts again.
                    if (!MetadataScriptTempFileStream.IsScriptTempFileUpdateNeeded(connectionInfo.ConnectionDetails.ServerName))
                    {
                        return;
                    }

                    var scripts = SmoScripterHelpers.GenerateAllServerTableScripts(sqlConn);
                    if (scripts != null)
                    {
                        try
                        {
                            MetadataScriptTempFileStream.Write(connectionInfo.ConnectionDetails.ServerName, scripts);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"An error was encountered while writing to the cache. Error: {ex.Message}");
                        }
                    }
                    else
                    {
                        Logger.Error("Failed to generate server scripts");
                    }
                }
            }
        }

        /// <summary>
        /// Handles the request for getting database server contextualization scripts.
        /// </summary>
        internal static Task HandleGetServerContextualizationRequest(GetServerContextualizationParams contextualizationParams,
            RequestContext<GetServerContextualizationResult> requestContext)
        {
            _ = Task.Factory.StartNew(async () =>
            {
                await GetServerContextualization(contextualizationParams, requestContext);
            },
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskScheduler.Default);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets server contextualization scripts. The retrieved scripts are create scripts for database objects like tables and views.
        /// </summary>
        /// <param name="contextualizationParams">The contextualization parameters to get context.</param>
        /// <param name="requestContext">The request context for the request.</param>
        /// <returns></returns>
        internal static async Task GetServerContextualization(GetServerContextualizationParams contextualizationParams, RequestContext<GetServerContextualizationResult> requestContext)
        {
            MetadataService.ConnectionServiceInstance.TryFindConnection(contextualizationParams.OwnerUri, out ConnectionInfo connectionInfo);

            if (connectionInfo != null)
            {
                try
                {
                    var scripts = MetadataScriptTempFileStream.Read(connectionInfo.ConnectionDetails.ServerName);
                    await requestContext.SendResult(new GetServerContextualizationResult
                    {
                        Context = scripts.ToArray()
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to read scripts from the script cache");
                    await requestContext.SendError(ex);
                }
            }
            else
            {
                Logger.Error("Failed to find connection info about the server.");
                await requestContext.SendError("Failed to find connection info about the server.");
            }
        }

        /// <summary>
        /// Handle a table pr view metadata query request
        /// </summary>        
        private static async Task HandleGetTableOrViewRequest(
            TableMetadataParams metadataParams,
            string objectType,
            RequestContext<TableMetadataResult> requestContext)
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
