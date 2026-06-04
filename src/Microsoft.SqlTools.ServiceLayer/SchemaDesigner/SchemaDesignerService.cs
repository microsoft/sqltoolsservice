//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public sealed class SchemaDesignerService : IDisposable
    {
        private static readonly Lazy<SchemaDesignerService> instance = new Lazy<SchemaDesignerService>(() => new SchemaDesignerService());
        private bool disposed = false;
        private IRpcServiceHost? serviceHost;
        private Dictionary<string, SchemaDesignerSession> sessions = new Dictionary<string, SchemaDesignerSession>();
        public static SchemaDesignerService Instance => instance.Value;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }

        public void InitializeService(IRpcServiceHost serviceHost)
        {
            Logger.Verbose("Initializing Schema Designer Service");
            this.serviceHost = serviceHost;
            serviceHost.RegisterRequestHandler(CreateSession.Type, HandleGetSchemaModelRequest);
            serviceHost.RegisterRequestHandler(GetDefinition.Type, HandleGetDefinitionRequest);
            serviceHost.RegisterRequestHandler(GenerateScript.Type, HandleGenerateScriptRequest);
            serviceHost.RegisterRequestHandler(GetReport.Type, HandleGetSchemaDesignerSessionReportRequest);
            serviceHost.RegisterRequestHandler(PublishSession.Type, HandlePublishSchemaDesignerSessionRequest);
            serviceHost.RegisterRequestHandler(DisposeSession.Type, HandleDisposeSchemaDesignerSessionRequest);

            Logger.Verbose("Initialized Schema Designer Service");
        }


        internal Task<CreateSessionResponse> HandleGetSchemaModelRequest(CreateSessionRequest requestParams)
        {
            return Utils.HandleRequest<CreateSessionResponse>(async () =>
            {
                string sessionId = string.IsNullOrWhiteSpace(requestParams.SessionId) ? Guid.NewGuid().ToString() : requestParams.SessionId;
                var session = new SchemaDesignerSession(
                    sessionId,
                    requestParams.ConnectionString,
                    requestParams.AccessToken,
                    CreateProgressNotificationHandler(),
                    CreateMessageNotificationHandler());
                sessions.Add(sessionId, session);

                return new CreateSessionResponse()
                {
                    Schema = session.InitialSchema,
                    DataTypes = session.AvailableDataTypes(),
                    SchemaNames = session.AvailableSchemas(),
                    SessionId = sessionId,
                };
            });
        }

        internal Task<GetDefinitionResponse> HandleGetDefinitionRequest(GetDefinitionRequest requestParams)
        {
            return Utils.HandleRequest<GetDefinitionResponse>(async () =>
            {
                return new GetDefinitionResponse()
                {
                    Script = SchemaCreationScriptGenerator.GenerateCreateTableScript(requestParams.UpdatedSchema!),
                };
            });
        }

        internal Task<GenerateScriptResponse> HandleGenerateScriptRequest(GenerateScriptRequest requestParams)
        {
            return Utils.HandleRequest<GenerateScriptResponse>(async () =>
            {
                if (sessions.TryGetValue(requestParams.SessionId!, out SchemaDesignerSession? session))
                {
                    return new GenerateScriptResponse()
                    {
                        Script = await session.GenerateScript(),
                    };
                }

                throw CreateSessionNotFoundException(requestParams.SessionId);
            });
        }

        internal Task<PublishSessionResponse> HandlePublishSchemaDesignerSessionRequest(PublishSessionRequest requestParams)
        {
            return Utils.HandleRequest<PublishSessionResponse>(async () =>
            {
                if (sessions.TryGetValue(requestParams.SessionId!, out SchemaDesignerSession? session))
                {
                    var metadata = new TaskMetadata()
                    {
                        Name = SR.SchemaDesignerPublishTaskName,
                        Description = SR.SchemaDesignerPublishTaskDescription,
                        TaskExecutionMode = TaskExecutionMode.Execute,
                        DatabaseName = session.DatabaseName,
                        ServerName = session.ServerName,
                        OperationName = "SchemaDesignerPublish",
                    };

                    var sqlTask = SqlTaskManager.Instance.CreateTask<SqlTask>(metadata, async (task) =>
                    {
                        await Task.Run(() =>
                        {
                            session.PublishSchema(task);
                        });

                        return new TaskResult()
                        {
                            TaskStatus = SqlTaskStatus.Succeeded,
                        };
                    });

                    await sqlTask.RunAsync();
                    if (sqlTask.TaskStatus == SqlTaskStatus.Failed)
                    {
                        throw new Exception(sqlTask.GetLastMessage()?.Description ?? SR.SchemaDesignerPublishFailed);
                    }

                    return new PublishSessionResponse();
                }

                throw CreateSessionNotFoundException(requestParams.SessionId);
            });
        }

        internal Task<DisposeSessionResponse> HandleDisposeSchemaDesignerSessionRequest(DisposeSessionRequest requestParams)
        {
            return Utils.HandleRequest<DisposeSessionResponse>(async () =>
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
                return new DisposeSessionResponse();
            });

        }

        internal Task<GetReportResponse> HandleGetSchemaDesignerSessionReportRequest(GetReportRequest requestParams)
        {
            return Utils.HandleRequest<GetReportResponse>(async () =>
            {
                SchemaDesignerSession session = sessions.TryGetValue(requestParams.SessionId!, out SchemaDesignerSession? activeSession)
                    ? activeSession
                    : throw CreateSessionNotFoundException(requestParams.SessionId);
                var report = await session.GetReport(requestParams.UpdatedSchema!);
                return report;
            });
        }

        private static Exception CreateSessionNotFoundException(string? sessionId)
        {
            return new KeyNotFoundException(string.Format(SR.SchemaDesignerSessionNotFound, sessionId ?? string.Empty));
        }

        private EventHandler<SchemaDesignerProgressNotificationParams> CreateProgressNotificationHandler()
        {
            return async (_, args) =>
            {
                if (serviceHost != null)
                {
                    await serviceHost.SendEvent(SchemaDesignerProgressNotification.Type, args);
                }
            };
        }

        private EventHandler<SchemaDesignerMessageNotificationParams> CreateMessageNotificationHandler()
        {
            return async (_, args) =>
            {
                if (serviceHost != null)
                {
                    await serviceHost.SendEvent(SchemaDesignerMessageNotification.Type, args);
                }
            };
        }
    }
}
