//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.Utility;


namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public sealed class SchemaDesignerService : IDisposable
    {
        private static readonly Lazy<SchemaDesignerService> instance = new Lazy<SchemaDesignerService>(() => new SchemaDesignerService());
        private bool disposed = false;
        private IProtocolEndpoint? serviceHost;
        private Dictionary<string, SchemaDesignerSession> sessions = new Dictionary<string, SchemaDesignerSession>();
        public static SchemaDesignerService Instance => instance.Value;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }

        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Verbose("Initializing Schema Designer Service");
            this.serviceHost = serviceHost;
            serviceHost.SetRequestHandler(CreateSession.Type, HandleGetSchemaModelRequest, true);
            serviceHost.SetRequestHandler(GetDefinition.Type, HandleGetDefinitionRequest, true);
            serviceHost.SetRequestHandler(GenerateScript.Type, HandleGenerateScriptRequest, true);
            serviceHost.SetRequestHandler(GetReport.Type, HandleGetSchemaDesignerSessionReportRequest, true);
            serviceHost.SetRequestHandler(PublishSession.Type, HandlePublishSchemaDesignerSessionRequest, true);
            serviceHost.SetRequestHandler(DisposeSession.Type, HandleDisposeSchemaDesignerSessionRequest, true);

            Logger.Verbose("Initialized Schema Designer Service");
        }


        internal Task HandleGetSchemaModelRequest(CreateSessionRequest requestParams, RequestContext<CreateSessionResponse> requestContext)
        {
            return Utils.HandleRequest<CreateSessionResponse>(requestContext, async () =>
            {
                string sessionId = Guid.NewGuid().ToString();

                ConnectionService.Instance.TryFindConnection(requestParams.ConnectionUri, out ConnectionInfo? connectionInfo);
                if (connectionInfo == null)
                {
                    requestContext.SendError($"Connection with URI '{requestParams.ConnectionUri}' not found.");
                    return;
                }

                ServerConnection serverConnection = ConnectionService.OpenServerConnection(connectionInfo);
                if (serverConnection == null)
                {
                    requestContext.SendError($"Failed to open server connection for URI '{requestParams.ConnectionUri}'.");
                    return;
                }

                Server server = new Server(serverConnection);
                if (server == null)
                {
                    requestContext.SendError($"Failed to create server object for URI '{requestParams.ConnectionUri}'.");
                    return;
                }

                Database? database = new Database(server, requestParams.DatabaseName);
                if (database == null)
                {
                    requestContext.SendError($"Database '{requestParams.DatabaseName}' not found in connection URI '{requestParams.ConnectionUri}'.");
                    return;
                }

                List<SchemaDesignerTable> tables = new List<SchemaDesignerTable>();
                foreach (Table table in database.Tables)
                {
                    SchemaDesignerTable schemaTable = new SchemaDesignerTable
                    {
                        Id = Guid.NewGuid(),
                        Name = table.Name,
                        Schema = table.Schema,
                        Columns = new List<SchemaDesignerColumn>(),
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    };

                    foreach (Column column in table.Columns)
                    {
                        string length = "";
                        switch (column.DataType.SqlDataType)
                        {
                            case SqlDataType.Char:
                            case SqlDataType.NChar:
                            case SqlDataType.Binary:
                            case SqlDataType.VarChar:
                            case SqlDataType.NVarChar:
                            case SqlDataType.VarBinary:
                                length = column.DataType.MaximumLength.ToString();
                                break;
                            case SqlDataType.VarBinaryMax:
                            case SqlDataType.NVarCharMax:
                            case SqlDataType.VarCharMax:
                                length += "(max)";
                                break;
                            case SqlDataType.Vector:
                                length += $"({(column.DataType.MaximumLength - 8) / 4})";
                                break;
                        }
                        schemaTable.Columns.Add(new SchemaDesignerColumn
                        {
                            Id = Guid.NewGuid(),
                            Name = column.Name,
                            DataType = column.DataType.Name,
                            MaxLength = length,
                            Precision = column.DataType.NumericPrecision,
                            Scale = column.DataType.NumericScale,
                            IsPrimaryKey = column.InPrimaryKey,
                            IsIdentity = column.Identity,
                            IdentitySeed = column.IdentitySeed,
                            IdentityIncrement = column.IdentityIncrement,
                            IsNullable = column.Nullable,
                            DefaultValue = column.DefaultConstraint?.Text,
                            IsComputed = column.Computed,
                            ComputedFormula = column.ComputedText,
                            ComputedPersisted = column.IsPersisted
                        });
                    }

                    foreach (ForeignKey foreignKey in table.ForeignKeys)
                    {
                        SchemaDesignerForeignKey schemaForeignKey = new SchemaDesignerForeignKey
                        {
                            Id = Guid.NewGuid(),
                            Name = foreignKey.Name,
                            ReferencedTableName = foreignKey.ReferencedTable,
                            ReferencedSchemaName = foreignKey.ReferencedTableSchema,
                            Columns = new List<string>(),
                            ReferencedColumns = new List<string>(),
                            OnDeleteAction = SchemaDesignerUtils.MapForeignKeyActionToOnAction(foreignKey.DeleteAction),
                            OnUpdateAction = SchemaDesignerUtils.MapForeignKeyActionToOnAction(foreignKey.UpdateAction),
                        };

                        foreach (ForeignKeyColumn fkColumn in foreignKey.Columns)
                        {
                            schemaForeignKey.Columns.Add(fkColumn.Name);
                        }

                        foreach (ForeignKeyColumn fkReferencedColumn in foreignKey.Columns)
                        {
                            schemaForeignKey.ReferencedColumns.Add(fkReferencedColumn.ReferencedColumn);
                        }
                        schemaTable.ForeignKeys.Add(schemaForeignKey);
                    }

                    tables.Add(schemaTable);
                }

                SchemaDesignerModel schema = new SchemaDesignerModel()
                {
                    Tables = tables
                };

                List<string> AvailableSchemas = new List<string>();
                foreach (Schema schemaItem in database.Schemas)
                {
                    AvailableSchemas.Add(schemaItem.Name);
                }

                List<string> AvailableDataTypes = new List<string>();
                foreach (UserDefinedDataType dataType in database.UserDefinedDataTypes)
                {
                    AvailableDataTypes.Add(dataType.Name);
                }

                await requestContext.SendResult(new CreateSessionResponse()
                {
                    Schema = schema,
                    DataTypes = AvailableDataTypes,
                    SchemaNames = AvailableSchemas,
                    SessionId = sessionId,
                });

                var session = new SchemaDesignerSession(requestParams.ConnectionString, schema, requestParams.AccessToken);
                sessions.Add(sessionId, session);
                await requestContext.SendEvent(ModelReadyNotification.Type, new ModelReadyParams()
                {
                    SessionId = sessionId,
                });
            });
        }

        internal Task HandleGetDefinitionRequest(GetDefinitionRequest requestParams, RequestContext<GetDefinitionResponse> requestContext)
        {
            return Utils.HandleRequest<GetDefinitionResponse>(requestContext, async () =>
            {
                await requestContext.SendResult(new GetDefinitionResponse()
                {
                    Script = SchemaCreationScriptGenerator.GenerateCreateTableScript(requestParams.UpdatedSchema),
                });
            });
        }

        internal Task HandleGenerateScriptRequest(GenerateScriptRequest requestParams, RequestContext<GenerateScriptResponse> requestContext)
        {
            return Utils.HandleRequest<GenerateScriptResponse>(requestContext, async () =>
            {
                if (sessions.TryGetValue(requestParams.SessionId, out SchemaDesignerSession? session))
                {
                    session.PublishSchema();

                    await requestContext.SendResult(new GenerateScriptResponse()
                    {
                        Script = await session.GenerateScript(),
                    });
                }
            });
        }

        internal Task HandlePublishSchemaDesignerSessionRequest(PublishSessionRequest requestParams, RequestContext<PublishSessionResponse> requestContext)
        {
            return Utils.HandleRequest<PublishSessionResponse>(requestContext, async () =>
            {
                if (sessions.TryGetValue(requestParams.SessionId, out SchemaDesignerSession? session))
                {
                    session.PublishSchema();
                }
                await requestContext.SendResult(new PublishSessionResponse());
            });
        }

        internal Task HandleDisposeSchemaDesignerSessionRequest(DisposeSessionRequest requestParams, RequestContext<DisposeSessionResponse> requestContext)
        {
            return Utils.HandleRequest<DisposeSessionResponse>(requestContext, async () =>
            {
                try
                {
                    if (sessions.TryGetValue(requestParams.SessionId, out SchemaDesignerSession? session))
                    {
                        session.Dispose();
                        sessions.Remove(requestParams.SessionId);
                        SchemaDesignerQueryExecution.Disconnect(requestParams.SessionId);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                }
                await requestContext.SendResult(new DisposeSessionResponse());
            });

        }

        internal Task HandleGetSchemaDesignerSessionReportRequest(GetReportRequest requestParams, RequestContext<GetReportResponse> requestContext)
        {
            return Utils.HandleRequest<GetReportResponse>(requestContext, async () =>
            {
                SchemaDesignerSession session = sessions[requestParams.SessionId];
                var report = await session.GetReport(requestParams.UpdatedSchema);
                await requestContext.SendResult(report);
            });
        }
    }
}