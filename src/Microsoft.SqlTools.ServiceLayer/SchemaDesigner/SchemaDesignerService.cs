//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
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