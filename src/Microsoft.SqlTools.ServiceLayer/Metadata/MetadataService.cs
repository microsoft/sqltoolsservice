//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
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
            serviceHost.RegisterRequestHandler(MetadataListRequest.Type, HandleMetadataListRequest);
            serviceHost.RegisterRequestHandler(TableMetadataRequest.Type, HandleGetTableRequest);
            serviceHost.RegisterRequestHandler(ViewMetadataRequest.Type, HandleGetViewRequest);
            serviceHost.RegisterRequestHandler(GetServerContextualizationRequest.Type, HandleGetServerContextualizationRequest);
        }

        /// <summary>
        /// Handle a metadata query request
        /// </summary>        
        internal async Task<MetadataQueryResult> HandleMetadataListRequest(
            MetadataQueryParams metadataParams)
        {
            Func<Task<MetadataQueryResult>> requestHandler = async () =>
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

                return await Task.FromResult(new MetadataQueryResult
                {
                    Metadata = metadata.ToArray()
                });
            };

            Task<MetadataQueryResult> task = Task.Run(requestHandler);
            MetadataListTask = task;
            try
            {
                return await task;
            }
            catch (Exception ex)
            {
                throw RpcErrorException.Create(ex);
            }
        }

        internal Task MetadataListTask { get; set; }

        /// <summary>
        /// Handle a table metadata query request
        /// </summary>        
        internal static async Task<TableMetadataResult> HandleGetTableRequest(
            TableMetadataParams metadataParams)
        {
            return await HandleGetTableOrViewRequest(metadataParams, "table");
        }

        /// <summary>
        /// Handle a view metadata query request
        /// </summary>        
        internal static async Task<TableMetadataResult> HandleGetViewRequest(
            TableMetadataParams metadataParams)
        {
            return await HandleGetTableOrViewRequest(metadataParams, "view");
        }

        /// <summary>
        /// Generates the contextualization scripts for a server. The generated context is in the form of create scripts for
        /// database objects like tables and views.
        /// </summary>
        /// <param name="contextualizationParams">The contextualization parameters.</param>
        internal static Task<GetServerContextualizationResult> HandleGetServerContextualizationRequest(GetServerContextualizationParams contextualizationParams)
        {
            return GetServerContextualization(contextualizationParams);
        }

        internal static async Task<GetServerContextualizationResult> GetServerContextualization(GetServerContextualizationParams contextualizationParams)
        {
            MetadataService.ConnectionServiceInstance.TryFindConnection(contextualizationParams.OwnerUri, out ConnectionInfo connectionInfo);
            if (connectionInfo == null)
            {
                Logger.Error("Failed to find connection info about the server.");
                throw new Exception(SR.FailedToFindConnectionInfoAboutTheServer);
            }
            else
            {
                if (MetadataScriptTempFileStream.IsScriptTempFileUpdateNeeded(connectionInfo.ConnectionDetails.ServerName))
                {
                    using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionInfo, "metadata"))
                    {
                        var scripts = SmoScripterHelpers.GenerateDatabaseScripts(sqlConn, contextualizationParams.DatabaseName)?.ToArray();
                        if (scripts != null)
                        {
                            try
                            {
                                string context = string.Join('\n', scripts);
                                MetadataScriptTempFileStream.Write(connectionInfo.ConnectionDetails.ServerName, contextualizationParams.DatabaseName, scripts);

                                return new GetServerContextualizationResult()
                                {
                                    Context = context
                                };
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"An error was encountered while generating server contextualization scripts. Error: {ex.Message}");
                                throw;
                            }
                        }
                        else
                        {
                            Logger.Error("Failed to generate server contextualization scripts");
                            throw new Exception(SR.FailedToGenerateServerContextualizationScripts);
                        }
                    }
                }
                else
                {
                    try
                    {
                        var scripts = MetadataScriptTempFileStream.Read(connectionInfo.ConnectionDetails.ServerName).ToArray();
                        return new GetServerContextualizationResult
                        {
                            Context = string.Join('\n', scripts)
                        };
                    }
                    catch (Exception)
                    {
                        Logger.Error("Failed to read scripts from the script cache");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Handle a table pr view metadata query request
        /// </summary>        
        private static async Task<TableMetadataResult> HandleGetTableOrViewRequest(
            TableMetadataParams metadataParams,
            string objectType)
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

            return new TableMetadataResult
            {
                Columns = metadata
            };
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
