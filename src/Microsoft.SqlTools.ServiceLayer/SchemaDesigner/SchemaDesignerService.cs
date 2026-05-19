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
                string sessionId = string.IsNullOrWhiteSpace(requestParams.SessionId) ? Guid.NewGuid().ToString() : requestParams.SessionId;
                var session = new SchemaDesignerSession(
                    sessionId,
                    requestParams.ConnectionString,
                    requestParams.AccessToken,
                    CreateProgressNotificationHandler(),
                    CreateMessageNotificationHandler());
                sessions.Add(sessionId, session);

                await requestContext.SendResult(new CreateSessionResponse()
                {
                    Schema = session.InitialSchema,
                    DataTypes = session.AvailableDataTypes(),
                    SchemaNames = session.AvailableSchemas(),
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
                    Script = SchemaCreationScriptGenerator.GenerateCreateTableScript(requestParams.UpdatedSchema!),
                });
            });
        }

        internal Task HandleGenerateScriptRequest(GenerateScriptRequest requestParams, RequestContext<GenerateScriptResponse> requestContext)
        {
            return Utils.HandleRequest<GenerateScriptResponse>(requestContext, async () =>
            {
                if (sessions.TryGetValue(requestParams.SessionId!, out SchemaDesignerSession? session))
                {
                    await requestContext.SendResult(new GenerateScriptResponse()
                    {
                        Script = await session.GenerateScript(),
                    });
                    return;
                }

                throw CreateSessionNotFoundException(requestParams.SessionId);
            });
        }

        internal Task HandlePublishSchemaDesignerSessionRequest(PublishSessionRequest requestParams, RequestContext<PublishSessionResponse> requestContext)
        {
            return Utils.HandleRequest<PublishSessionResponse>(requestContext, async () =>
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

                        await requestContext.SendResult(new PublishSessionResponse());
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

                    return;
                }

                throw CreateSessionNotFoundException(requestParams.SessionId);
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
                SchemaDesignerSession session = sessions.TryGetValue(requestParams.SessionId!, out SchemaDesignerSession? activeSession)
                    ? activeSession
                    : throw CreateSessionNotFoundException(requestParams.SessionId);
                var report = await session.GetReport(requestParams.UpdatedSchema!);
                await requestContext.SendResult(report);
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
