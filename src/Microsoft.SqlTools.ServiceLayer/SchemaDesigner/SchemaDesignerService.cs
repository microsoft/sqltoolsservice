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
                string connectionUri = Guid.NewGuid().ToString();
                var connectionCompleteParams = await SchemaDesignerQueryExecution.CloneConnectionAsync(requestParams.ConnectionUri, connectionUri, requestParams.DatabaseName);

                SchemaDesignerModel schema = await SchemaDesignerModelProvider.GetSchemaModelAsync(connectionUri);
                List<string> dataTypes = await SchemaDesignerModelProvider.GetDatatypesAsync(connectionUri);
                List<string> schemas = await SchemaDesignerModelProvider.GetSchemasAsync(connectionUri);

                await requestContext.SendResult(new CreateSessionResponse()
                {
                    Schema = schema,
                    DataTypes = dataTypes,
                    SchemaNames = schemas,
                    SessionId = connectionUri,
                });

                _ = Task.Run(async () =>
                {
                    var session = new SchemaDesignerSession(connectionUri, schema);
                    sessions.Add(connectionUri, session);
                    await requestContext.SendEvent(SchemaReady.Type, new SchemaReadyResponse()
                    {
                        SessionId = connectionUri,
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

        }

        internal async Task HandleGetSchemaDesignerSessionReportRequest(GetReportRequest requestParams, RequestContext<GetReportResponse> requestContext)
        {
            try
            {
                SchemaDesignerSession session = sessions[requestParams.SessionId];
                await requestContext.SendResult(session.GetReport(requestParams.UpdatedSchema));
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                await requestContext.SendError(e);
            }
        }
    }
}