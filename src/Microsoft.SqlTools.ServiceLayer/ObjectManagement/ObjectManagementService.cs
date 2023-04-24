//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Main class for ObjectManagement Service functionality
    /// </summary>
    public class ObjectManagementService
    {
        public const string ApplicationName = "azdata-object-management";
        private static Lazy<ObjectManagementService> objectManagementServiceInstance = new Lazy<ObjectManagementService>(() => new ObjectManagementService());
        public static ObjectManagementService Instance => objectManagementServiceInstance.Value;
        public static ConnectionService connectionService;
        private IProtocolEndpoint serviceHost;
        private List<IObjectTypeHandler> objectTypeHandlers = new List<IObjectTypeHandler>();
        private ConcurrentDictionary<string, SqlObjectViewContext> contextMap = new ConcurrentDictionary<string, SqlObjectViewContext>();

        public ObjectManagementService()
        {
            this.objectTypeHandlers.Add(new CommonObjectTypeHandler(ConnectionService.Instance));
            this.objectTypeHandlers.Add(new LoginHandler(ConnectionService.Instance));
            this.objectTypeHandlers.Add(new UserHandler(ConnectionService.Instance));
            this.objectTypeHandlers.Add(new CredentialHandler(ConnectionService.Instance));
            this.objectTypeHandlers.Add(new AppRoleHandler(ConnectionService.Instance));
            this.objectTypeHandlers.Add(new DatabaseRoleHandler(ConnectionService.Instance));
            this.objectTypeHandlers.Add(new ServerRoleHandler(ConnectionService.Instance));
        }

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

        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            this.serviceHost = serviceHost;
            this.serviceHost.SetRequestHandler(RenameRequest.Type, HandleRenameRequest, true);
            this.serviceHost.SetRequestHandler(DropRequest.Type, HandleDropRequest, true);
            this.serviceHost.SetRequestHandler(InitializeViewRequest.Type, HandleInitializeViewRequest, true);
            this.serviceHost.SetRequestHandler(SaveObjectRequest.Type, HandleSaveObjectRequest, true);
            this.serviceHost.SetRequestHandler(ScriptObjectRequest.Type, HandleScriptObjectRequest, true);
            this.serviceHost.SetRequestHandler(DisposeViewRequest.Type, HandleDisposeViewRequest, true);
        }

        internal async Task HandleRenameRequest(RenameRequestParams requestParams, RequestContext<RenameRequestResponse> requestContext)
        {
            var handler = this.GetObjectTypeHandler(requestParams.ObjectType);
            await handler.Rename(requestParams.ConnectionUri, requestParams.ObjectUrn, requestParams.NewName);
            await requestContext.SendResult(new RenameRequestResponse());
        }

        internal async Task HandleDropRequest(DropRequestParams requestParams, RequestContext<DropRequestResponse> requestContext)
        {
            var handler = this.GetObjectTypeHandler(requestParams.ObjectType);
            await handler.Drop(requestParams.ConnectionUri, requestParams.ObjectUrn, requestParams.ThrowIfNotExist);
            await requestContext.SendResult(new DropRequestResponse());
        }

        internal async Task HandleInitializeViewRequest(InitializeViewRequestParams requestParams, RequestContext<SqlObjectViewInfo> requestContext)
        {
            var handler = this.GetObjectTypeHandler(requestParams.ObjectType);
            var result = await handler.InitializeObjectView(requestParams);
            contextMap[requestParams.ContextId] = result.Context;
            await requestContext.SendResult(result.ViewInfo);
        }

        internal async Task HandleSaveObjectRequest(SaveObjectRequestParams requestParams, RequestContext<SaveObjectRequestResponse> requestContext)
        {
            var context = this.GetContext(requestParams.ContextId);
            var handler = this.GetObjectTypeHandler(context.Parameters.ObjectType);
            var obj = requestParams.Object.ToObject(handler.GetObjectType());
            await handler.Save(context, obj as SqlObject);
            await requestContext.SendResult(new SaveObjectRequestResponse());
        }

        internal async Task HandleScriptObjectRequest(ScriptObjectRequestParams requestParams, RequestContext<string> requestContext)
        {
            var context = this.GetContext(requestParams.ContextId);
            var handler = this.GetObjectTypeHandler(context.Parameters.ObjectType);
            var obj = requestParams.Object.ToObject(handler.GetObjectType());
            var script = await handler.Script(context, obj as SqlObject);
            await requestContext.SendResult(script);
        }

        internal async Task HandleDisposeViewRequest(DisposeViewRequestParams requestParams, RequestContext<DisposeViewRequestResponse> requestContext)
        {
            SqlObjectViewContext context;
            if (contextMap.Remove(requestParams.ContextId, out context))
            {
                context.Dispose();
            }
            await requestContext.SendResult(new DisposeViewRequestResponse());
        }

        private IObjectTypeHandler GetObjectTypeHandler(SqlObjectType objectType)
        {
            foreach (var handler in objectTypeHandlers)
            {
                if (handler.CanHandleType(objectType))
                {
                    return handler;
                }
            }
            throw new NotSupportedException(objectType.ToString());
        }

        private SqlObjectViewContext GetContext(string contextId)
        {
            if (contextMap.TryGetValue(contextId, out SqlObjectViewContext context))
            {
                return context;
            }
            throw new ArgumentException($"Context '{contextId}' not found");
        }
    }
}