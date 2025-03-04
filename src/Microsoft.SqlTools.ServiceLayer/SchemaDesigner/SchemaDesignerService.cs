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

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static SchemaDesignerService Instance => instance.Value;

        /// <summary>
        /// Dipose the service
        /// </summary>
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
            serviceHost.SetRequestHandler(CreateSchemaDesignerSessionRequest.Type, HandleGetSchemaModelRequest);
            serviceHost.SetRequestHandler(GetSchemaDesignerCreateAsScriptRequest.Type, HandleGetSchemaDesignerScriptRequest);
            serviceHost.SetRequestHandler(DisposeSchemaDesignerSessionRequest.Type, HandleDisposeSchemaDesignerSessionRequest);
            serviceHost.SetRequestHandler(GetSchemaDesignerSessionReportRequest.Type, HandleGetSchemaDesignerSessionReportRequest);
            Logger.Verbose("Initialized Schema Designer Service");
        }


        internal async Task HandleGetSchemaModelRequest(CreateSchemaDesignerSessionRequestParams requestParams, RequestContext<CreateSchemaDesignerSessionResponseParams> requestContext)
        {
            try
            {
                string sessionId = Guid.NewGuid().ToString();
                await SchemaDesignerQueryExecution.CloneConnection(requestParams.ConnectionUri, sessionId, requestParams.DatabaseName);

                SchemaDesignerModel schema = await SchemaDesignerSchemaFetcher.GetSchemaModel(sessionId);
                List<string> datatypes = await SchemaDesignerSchemaFetcher.GetDatatypes(sessionId);
                List<string> schemas = await SchemaDesignerSchemaFetcher.GetSchemas(sessionId);
                
                await requestContext.SendResult(new CreateSchemaDesignerSessionResponseParams()
                {
                    SchemaModel = schema,
                    DataTypes = datatypes,
                    SchemaNames = schemas,
                    SessionId = sessionId,
                });

                _ = Task.Run(async () =>
                {
                    var session = new SchemaDesignerSession(sessionId, schema);
                    await requestContext.SendEvent(SchemaDesignerModelReady.Type, new SchemaDesignerModelReadyParams()
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

        internal async Task HandleGetSchemaDesignerScriptRequest(GetSchemaDesignerCreateAsScriptParams requestParams, RequestContext<GetSchemaDesignerCreateAsScriptResponse> requestContext)
        {
            try
            {
                await requestContext.SendResult(new GetSchemaDesignerCreateAsScriptResponse()
                {
                    Scripts = SchemaDesignerUtils.GetCreateAsScriptForSchema(requestParams.UpdatedModel),
                    CombinedScript = SchemaDesignerUtils.GetCombinedScriptForSchema(requestParams.UpdatedModel)
                });
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                await requestContext.SendError(e);
            }
        }

        internal async Task HandleDisposeSchemaDesignerSessionRequest(DisposeSchemaDesignerSessionParams requestParams, RequestContext<DisposeSchemaDesignerSessionResponse> requestContext)
        {
            try
            {
                SchemaDesignerSession session = sessions[requestParams.SessionId];
                session.CloseSession();
                sessions.Remove(requestParams.SessionId);
                SchemaDesignerQueryExecution.Disconnect(requestParams.SessionId);
                await requestContext.SendResult(new DisposeSchemaDesignerSessionResponse());
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                await requestContext.SendError(e);
            }
        }

        internal async Task HandleGetSchemaDesignerSessionReportRequest(GetSchemaDesignerSessionReportParams requestParams, RequestContext<GetSchemaDesignerSessionReportResponse> requestContext)
        {
            try
            {
                SchemaDesignerSession session = sessions[requestParams.SessionId];
                await requestContext.SendResult(new GetSchemaDesignerSessionReportResponse()
                {
                    Reports = await session.GetReport(requestParams.UpdatedModel)
                });
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                await requestContext.SendError(e);
            }
        }
    }
}