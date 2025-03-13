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
        private Dictionary<string, SchemaDesignerSession2> sessions = new Dictionary<string, SchemaDesignerSession2>();
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
            serviceHost.SetRequestHandler(CreateSession.Type, HandleGetSchemaModelRequest);
            serviceHost.SetRequestHandler(GenerateScript.Type, HandleGetSchemaDesignerScriptRequest);
            serviceHost.SetRequestHandler(DisposeSession.Type, HandleDisposeSchemaDesignerSessionRequest);
            serviceHost.SetRequestHandler(GetReport.Type, HandleGetSchemaDesignerSessionReportRequest);
            Logger.Verbose("Initialized Schema Designer Service");
        }


        internal async Task HandleGetSchemaModelRequest(CreateSessionRequest requestParams, RequestContext<CreateSessionResponse> requestContext)
        {
            try
            {
                string sessionId = Guid.NewGuid().ToString();
                await SchemaDesignerQueryExecution.CloneConnection(requestParams.ConnectionUri, sessionId, requestParams.DatabaseName);

                SchemaDesignerModel schema = await SchemaDesignerModelProvider.GetSchemaModel(sessionId);
                List<string> dataTypes = await SchemaDesignerModelProvider.GetDatatypes(sessionId);
                List<string> schemas = await SchemaDesignerModelProvider.GetSchemas(sessionId);

                await requestContext.SendResult(new CreateSessionResponse()
                {
                    Schema = schema,
                    DataTypes = dataTypes,
                    SchemaNames = schemas,
                    SessionId = sessionId,
                });

                _ = Task.Run(async () =>
                {
                    var session = new SchemaDesignerSession2(sessionId, schema);
                    sessions.Add(sessionId, session);
                    await requestContext.SendEvent(SchemaReady.Type, new SchemaReadyResponse()
                    {
                        SessionId = sessionId,
                    });

                });
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                await requestContext.SendError(e);
            }
        }

        internal async Task HandleGetSchemaDesignerScriptRequest(GenerateScriptRequest requestParams, RequestContext<GenerateScriptResponse> requestContext)
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
        }

        internal async Task HandleDisposeSchemaDesignerSessionRequest(DisposeSessionRequest requestParams, RequestContext<DisposeSessionResponse> requestContext)
        {
            try
            {
                SchemaDesignerSession2 session = sessions[requestParams.SessionId];
                session.CloseSession();
                sessions.Remove(requestParams.SessionId);
                SchemaDesignerQueryExecution.Disconnect(requestParams.SessionId);
                await requestContext.SendResult(new DisposeSessionResponse());
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                await requestContext.SendError(e);
            }
        }

        internal async Task HandleGetSchemaDesignerSessionReportRequest(GetReportRequest requestParams, RequestContext<GetReportResponse> requestContext)
        {
            try
            {
                SchemaDesignerSession2 session = sessions[requestParams.SessionId];
                await requestContext.SendResult(SchemaDesignerUpdater.GenerateUpdateScripts(session.schema, requestParams.UpdatedSchema));
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                await requestContext.SendError(e);
            }
        }
    }
}