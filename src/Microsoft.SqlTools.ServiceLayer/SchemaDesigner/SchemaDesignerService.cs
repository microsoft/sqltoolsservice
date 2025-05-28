//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
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
            serviceHost.SetRequestHandler(GenerateScript.Type, HandleGetSchemaDesignerScriptRequest, true);
            serviceHost.SetRequestHandler(DisposeSession.Type, HandleDisposeSchemaDesignerSessionRequest, true);
            serviceHost.SetRequestHandler(GetReport.Type, HandleGetSchemaDesignerSessionReportRequest, true);
            serviceHost.SetRequestHandler(PublishSession.Type, HandlePublishSchemaDesignerSessionRequest, true);
            Logger.Verbose("Initialized Schema Designer Service");
        }


        internal Task HandleGetSchemaModelRequest(CreateSessionRequest requestParams, RequestContext<CreateSessionResponse> requestContext)
        {
            return this.HandleRequest<CreateSessionResponse>(requestContext, async () =>
            {
                try
                {
                    string connectionUri = Guid.NewGuid().ToString();
                    var session = new SchemaDesignerSession(requestParams.ConnectionString, requestParams.AccessToken);
                    sessions.Add(connectionUri, session);

                    await requestContext.SendResult(new CreateSessionResponse()
                    {
                        Schema = session.InitialSchema,
                        DataTypes = session.AvailableDataTypes(),
                        SchemaNames = session.AvailableSchemas(),
                        SessionId = connectionUri,
                    });
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                    await requestContext.SendError(e);
                }
            });

        }

        internal Task HandleGetSchemaDesignerScriptRequest(GenerateScriptRequest requestParams, RequestContext<GenerateScriptResponse> requestContext)
        {
            return this.HandleRequest<GenerateScriptResponse>(requestContext, async () =>
            {
                try
                {
                    await requestContext.SendResult(new GenerateScriptResponse()
                    {
                        Scripts = SchemaCreationScriptGenerator.GenerateCreateAsScriptForSchemaTables(requestParams.UpdatedSchema),
                        CombinedScript = SchemaCreationScriptGenerator.GenerateCreateTableScript(requestParams.UpdatedSchema)
                    });
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                    await requestContext.SendError(e);
                }
            });

        }

        internal Task HandleDisposeSchemaDesignerSessionRequest(DisposeSessionRequest requestParams, RequestContext<DisposeSessionResponse> requestContext)
        {
            return this.HandleRequest<DisposeSessionResponse>(requestContext, async () =>
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

        internal Task HandlePublishSchemaDesignerSessionRequest(PublishSessionRequest requestParams, RequestContext<PublishSessionResponse> requestContext)
        {
            return this.HandleRequest<PublishSessionResponse>(requestContext, async () =>
            {
                try
                {
                    if (sessions.TryGetValue(requestParams.SessionId, out SchemaDesignerSession? session))
                    {
                        session.PublishSchema();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                    await requestContext.SendError(e);
                }
                await requestContext.SendResult(new PublishSessionResponse());
            });
        }


        internal Task HandleGetSchemaDesignerSessionReportRequest(GetReportRequest requestParams, RequestContext<GetReportResponse> requestContext)
        {
            return this.HandleRequest<GetReportResponse>(requestContext, async () =>
            {
                SchemaDesignerSession session = sessions[requestParams.SessionId];
                var report = await session.GetReport(requestParams.UpdatedSchema);
                try
                {
                    await requestContext.SendResult(report);
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                    await requestContext.SendError(e);
                }
            });
        }

        private Task HandleRequest<T>(RequestContext<T> requestContext, Func<Task> action)
        {
            // The request handling will take some time to return, we need to use a separate task to run the request handler so that it won't block the main thread.
            // For any specific table designer instance, ADS UI can make sure there are at most one request being processed at any given time, so we don't have to worry about race conditions.
            Task.Run(async () =>
            {
                try
                {
                    await action();
                }
                catch (Exception e)
                {
                    await requestContext.SendError(e);
                }
            });
            return Task.CompletedTask;
        }
    }
}