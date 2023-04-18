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
using Microsoft.SqlTools.Utility;
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
        private List<ObjectTypeHandler> objectTypeHandlers = new List<ObjectTypeHandler>();
        private ConcurrentDictionary<string, ISqlObjectViewContext> contextMap = new ConcurrentDictionary<string, ISqlObjectViewContext>();

        public ObjectManagementService()
        {
            var CommonObjectTypeHandler = new CommonObjectTypeHandler(ConnectionService.Instance);
            var loginHandler = new LoginHandler(ConnectionService.Instance);
            var userHandler = new UserHandler(ConnectionService.Instance);
            this.objectTypeHandlers.Add(CommonObjectTypeHandler);
            this.objectTypeHandlers.Add(loginHandler);
            this.objectTypeHandlers.Add(userHandler);
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
            this.serviceHost.SetRequestHandler(CreateObjectRequest.Type, HandleCreateObjectRequest, true);
            this.serviceHost.SetRequestHandler(UpdateObjectRequest.Type, HandleUpdateObjectRequest, true);
            this.serviceHost.SetRequestHandler(ScriptObjectRequest.Type, HandleScriptObjectRequest, true);
            this.serviceHost.SetRequestHandler(DisposeViewRequest.Type, HandleDisposeViewRequest, true);
        }

        internal async Task HandleRenameRequest(RenameRequestParams requestParams, RequestContext<RenameRequestResponse> requestContext)
        {
            var handler = this.GetObjectTypeHandler(requestParams.ObjectType);
            handler.Rename(requestParams.ConnectionUri, requestParams.ObjectUrn, requestParams.NewName);
            await requestContext.SendResult(new RenameRequestResponse());
        }

        internal async Task HandleDropRequest(DropRequestParams requestParams, RequestContext<DropRequestResponse> requestContext)
        {
            var handler = this.GetObjectTypeHandler(requestParams.ObjectType);
            handler.Drop(requestParams.ConnectionUri, requestParams.ObjectUrn, requestParams.ThrowIfNotExist);
            await requestContext.SendResult(new DropRequestResponse());
        }

        private async Task HandleInitializeViewRequest(InitializeViewRequestParams requestParams, RequestContext<InitializeViewRequestResponse> requestContext)
        {
            var handler = this.GetObjectTypeHandler(requestParams.ObjectType);
            ISqlObjectViewContext context;
            var objectView = handler.InitializeObjectView(requestParams, out context);
            contextMap[requestParams.ContextId] = context;
            await requestContext.SendResult(new InitializeViewRequestResponse()
            {
                viewInfo = objectView
            });
        }

        private async Task HandleCreateObjectRequest(CreateObjectRequestParams requestParams, RequestContext<CreateObjectRequestResponse> requestContext)
        {
            var context = this.GetContext(requestParams.ContextId);
            var handler = this.GetObjectTypeHandler(context.ObjectType);
            var objectType = handler.GetObjectType();
            var obj = requestParams.Object.ToObject(objectType);
            handler.Create(context, obj as SqlObject);
            await requestContext.SendResult(new CreateObjectRequestResponse());
        }

        private async Task HandleUpdateObjectRequest(UpdateObjectRequestParams requestParams, RequestContext<UpdateObjectRequestResponse> requestContext)
        {
            var context = this.GetContext(requestParams.ContextId);
            var handler = this.GetObjectTypeHandler(context.ObjectType);
            var objectType = handler.GetObjectType();
            var obj = requestParams.Object.ToObject(objectType);
            handler.Update(context, obj as SqlObject);
            await requestContext.SendResult(new UpdateObjectRequestResponse());
        }

        private async Task HandleScriptObjectRequest(ScriptObjectRequestParams requestParams, RequestContext<string> requestContext)
        {
            var context = this.GetContext(requestParams.ContextId);
            var handler = this.GetObjectTypeHandler(context.ObjectType);
            var objectType = handler.GetObjectType();
            var obj = requestParams.Object.ToObject(objectType);
            var script = handler.Script(context, obj as SqlObject);
            await requestContext.SendResult(script);
        }

        private async Task HandleDisposeViewRequest(DisposeObjectViewRequestParams requestParams, RequestContext<DisposeViewRequestResponse> requestContext)
        {
            ISqlObjectViewContext context;
            if (contextMap.Remove(requestParams.ContextId, out context))
            {
                context.Dispose();
            }
            await requestContext.SendResult(new DisposeViewRequestResponse());
        }

        private ObjectTypeHandler GetObjectTypeHandler(SqlObjectType objectType)
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

        private ISqlObjectViewContext GetContext(string contextId)
        {
            if (contextMap.TryGetValue(contextId, out ISqlObjectViewContext context))
            {
                return context;
            }
            throw new ArgumentException($"Context '{contextId}' not found");
        }
    }
}