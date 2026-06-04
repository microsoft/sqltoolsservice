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
using Microsoft.SqlTools.ServiceLayer.Management;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.PermissionsData;
using Microsoft.SqlTools.ServiceLayer.TaskServices;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Main class for ObjectManagement Service functionality
    /// </summary>
    public class ObjectManagementService
    {
        public const string ApplicationName = "object-management";
        private static Lazy<ObjectManagementService> objectManagementServiceInstance = new Lazy<ObjectManagementService>(() => new ObjectManagementService());
        public static ObjectManagementService Instance => objectManagementServiceInstance.Value;
        public static ConnectionService connectionService;
        private IRpcServiceHost serviceHost;
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
            this.objectTypeHandlers.Add(new DatabaseHandler(ConnectionService.Instance));
            this.objectTypeHandlers.Add(new ServerHandler(ConnectionService.Instance));
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

        public void InitializeService(IRpcServiceHost serviceHost)
        {
            this.serviceHost = serviceHost;
            this.serviceHost.RegisterRequestHandler(RenameRequest.Type, HandleRenameRequest);
            this.serviceHost.RegisterRequestHandler(DropRequest.Type, HandleDropRequest);
            this.serviceHost.RegisterRequestHandler(CreateCredentialRequest.Type, HandleCreateCredentialRequest);
            this.serviceHost.RegisterRequestHandler(GetCredentialNamesRequest.Type, HandleGetCredentialNamesRequest);
            this.serviceHost.RegisterRequestHandler(InitializeViewRequest.Type, HandleInitializeViewRequest);
            this.serviceHost.RegisterRequestHandler(SaveObjectRequest.Type, HandleSaveObjectRequest);
            this.serviceHost.RegisterRequestHandler(ScriptObjectRequest.Type, HandleScriptObjectRequest);
            this.serviceHost.RegisterRequestHandler(DisposeViewRequest.Type, HandleDisposeViewRequest);
            this.serviceHost.RegisterRequestHandler(SearchRequest.Type, HandleSearchRequest);
            this.serviceHost.RegisterRequestHandler(DetachDatabaseRequest.Type, HandleDetachDatabaseRequest);
            this.serviceHost.RegisterRequestHandler(AttachDatabaseRequest.Type, HandleAttachDatabaseRequest);
            this.serviceHost.RegisterRequestHandler(DropDatabaseRequest.Type, HandleDropDatabaseRequest);
            this.serviceHost.RegisterRequestHandler(RenameDatabaseRequest.Type, HandleRenameDatabaseRequest);
            this.serviceHost.RegisterRequestHandler(PurgeQueryStoreDataRequest.Type, HandlePurgeQueryStoreDataRequest);
        }

        internal async Task<RenameRequestResponse> HandleRenameRequest(RenameRequestParams requestParams)
        {
            var handler = this.GetObjectTypeHandler(requestParams.ObjectType);
            await handler.Rename(requestParams.ConnectionUri, requestParams.ObjectUrn, requestParams.NewName);
            return new RenameRequestResponse();
        }

        internal async Task<RenameDatabaseResponse> HandleRenameDatabaseRequest(RenameDatabaseRequestParams requestParams)
        {
            var handler = this.GetObjectTypeHandler(SqlObjectType.Database) as DatabaseHandler;
            var operation = new RenameDatabaseOperation(handler, requestParams);

            if (requestParams.GenerateScript)
            {
                operation.Execute(TaskExecutionMode.Script);
                return new RenameDatabaseResponse
                {
                    Script = operation.ScriptContent ?? string.Empty,
                };
            }

            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(requestParams.ConnectionUri, out connInfo);
            TaskMetadata metadata = new TaskMetadata
            {
                Name = operation.TaskName,
                Description = operation.TaskDescription,
                TaskExecutionMode = TaskExecutionMode.Execute,
                ServerName = connInfo?.ConnectionDetails?.ServerName,
                DatabaseName = requestParams.NewName ?? connInfo?.ConnectionDetails?.DatabaseName,
                TaskOperation = operation,
                OwnerUri = requestParams.ConnectionUri,
                OperationName = typeof(RenameDatabaseOperation).Name,
            };
            SqlTask sqlTask = SqlTaskManager.Instance.CreateTask<SqlTask>(metadata);
            TaskExecutionResult taskResult = await RunTaskAsync(sqlTask);

            return new RenameDatabaseResponse
            {
                TaskId = taskResult.TaskId,
                ErrorMessage = taskResult.ErrorMessage,
            };
        }

        internal async Task<DropRequestResponse> HandleDropRequest(DropRequestParams requestParams)
        {
            var handler = this.GetObjectTypeHandler(requestParams.ObjectType);
            await handler.Drop(requestParams.ConnectionUri, requestParams.ObjectUrn, requestParams.ThrowIfNotExist);
            return new DropRequestResponse();
        }

        internal async Task<CreateCredentialRequestResponse> HandleCreateCredentialRequest(CreateCredentialRequestParams requestParams)
        {
            var handler = this.GetObjectTypeHandler(SqlObjectType.Credential) as CredentialHandler;
            await handler.Create(requestParams);
            return new CreateCredentialRequestResponse();
        }

        internal async Task<List<string>> HandleGetCredentialNamesRequest(GetCredentialNamesRequestParams requestParams)
        {
            var handler = this.GetObjectTypeHandler(SqlObjectType.Credential) as CredentialHandler;
            var credentials = handler.GetCredentialNames(requestParams);
            return credentials;
        }

        internal async Task<SqlObjectViewInfo> HandleInitializeViewRequest(InitializeViewRequestParams requestParams)
        {
            var handler = this.GetObjectTypeHandler(requestParams.ObjectType);
            var result = await handler.InitializeObjectView(requestParams);
            contextMap[requestParams.ContextId] = result.Context;
            return result.ViewInfo;
        }

        internal async Task<SaveObjectRequestResponse> HandleSaveObjectRequest(SaveObjectRequestParams requestParams)
        {
            var context = this.GetContext(requestParams.ContextId);
            var handler = this.GetObjectTypeHandler(context.Parameters.ObjectType);
            var obj = requestParams.Object.ToObject(handler.GetObjectType()) as SqlObject;
            var saveOperation = new SaveObjectOperation(handler, context, obj);

            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(context.Parameters.ConnectionUri, out connInfo);

            TaskMetadata metadata = new TaskMetadata
            {
                Name = saveOperation.TaskName,
                Description = saveOperation.TaskDescription,
                TaskExecutionMode = TaskExecutionMode.Execute,
                ServerName = connInfo?.ConnectionDetails?.ServerName,
                DatabaseName = saveOperation.TargetDatabaseName ?? connInfo?.ConnectionDetails?.DatabaseName,
                TaskOperation = saveOperation,
                OwnerUri = context.Parameters.ConnectionUri,
                OperationName = typeof(SaveObjectOperation).Name,
            };

            SqlTask sqlTask = SqlTaskManager.Instance.CreateTask<SqlTask>(metadata);
            TaskExecutionResult taskResult = await RunTaskAsync(sqlTask, saveOperation.ExecutionException);

            return new SaveObjectRequestResponse
            {
                TaskId = taskResult.TaskId,
                ErrorMessage = taskResult.ErrorMessage,
            };
        }

        internal async Task<string> HandleScriptObjectRequest(ScriptObjectRequestParams requestParams)
        {
            var context = this.GetContext(requestParams.ContextId);
            var handler = this.GetObjectTypeHandler(context.Parameters.ObjectType);
            var obj = requestParams.Object.ToObject(handler.GetObjectType());
            var script = await handler.Script(context, obj as SqlObject);
            return script;
        }

        internal async Task<DisposeViewRequestResponse> HandleDisposeViewRequest(DisposeViewRequestParams requestParams)
        {
            SqlObjectViewContext context;
            if (contextMap.Remove(requestParams.ContextId, out context))
            {
                context.Dispose();
            }
            return new DisposeViewRequestResponse();
        }

        internal async Task<SearchResultItem[]> HandleSearchRequest(SearchRequestParams requestParams)
        {
            var context = this.GetContext(requestParams.ContextId);
            ConnectionInfo connInfo;
            ConnectionService.Instance.TryFindConnection(context.Parameters.ConnectionUri, out connInfo);
            if (connInfo == null)
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

            List<SearchResultItem> res = new List<SearchResultItem>();
            var schemaTypes = SecurableUtils.GetSchemaTypes(dataContainer.Server).Select(Securable.GetSearchableObjectType).ToHashSet();

            foreach (string type in requestParams.ObjectTypes)
            {
                SearchableObjectCollection result = new SearchableObjectCollection();
                SearchableObjectType searchableObjectType = SecurableUtils.ConvertPotentialSqlObjectTypeToSearchableObjectType(type);

                if (searchableObjectType == SearchableObjectType.LastType)
                {
                    continue;
                }

                // only schema-Scoped securableTypes support schema level search
                if (!string.IsNullOrEmpty(requestParams.Schema) && !schemaTypes.Contains(searchableObjectType))
                {
                    continue;
                }

                SearchableObjectTypeDescription desc = SearchableObjectTypeDescription.GetDescription(searchableObjectType);

                if (desc.IsDatabaseObject)
                {
                    if (!string.IsNullOrEmpty(requestParams.Schema))
                    {
                        SearchableObject.Search(result, searchableObjectType, dataContainer.ConnectionInfo, context.Parameters.Database, requestParams.SearchText ?? string.Empty, false, requestParams.Schema, true, true);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(requestParams.SearchText))
                        {
                            SearchableObject.Search(result, searchableObjectType, dataContainer.ConnectionInfo, context.Parameters.Database, requestParams.SearchText, false, true);
                        }
                        else
                        {
                            SearchableObject.Search(result, searchableObjectType, dataContainer.ConnectionInfo, context.Parameters.Database, true);
                        }
                    }
                }
                else
                {
                    // server object
                    if (string.IsNullOrEmpty(requestParams.SearchText))
                    {
                        SearchableObject.Search(result, searchableObjectType, dataContainer.ConnectionInfo, true);
                    }
                    else
                    {
                        SearchableObject.Search(result, searchableObjectType, dataContainer.ConnectionInfo, requestParams.SearchText, false, true);
                    }
                }

                foreach (SearchableObject obj in result)
                {
                    res.Add(new SearchResultItem
                    {
                        Name = obj.Name,
                        Type = type,
                        Schema = obj.Schema
                    });
                }
            }
            return res.ToArray();
        }

        internal async Task<string> HandleDetachDatabaseRequest(DetachDatabaseRequestParams requestParams)
        {
            var handler = this.GetObjectTypeHandler(SqlObjectType.Database) as DatabaseHandler;
            var sqlScript = handler.Detach(requestParams);
            return sqlScript;
        }

        internal async Task<string> HandleAttachDatabaseRequest(AttachDatabaseRequestParams requestParams)
        {
            var handler = this.GetObjectTypeHandler(SqlObjectType.Database) as DatabaseHandler;
            var sqlScript = handler.Attach(requestParams);
            return sqlScript;
        }

        internal async Task<DropDatabaseResponse> HandleDropDatabaseRequest(DropDatabaseRequestParams requestParams)
        {
            var handler = this.GetObjectTypeHandler(SqlObjectType.Database) as DatabaseHandler;
            var operation = new DropDatabaseOperation(handler, requestParams);

            if (requestParams.GenerateScript)
            {
                operation.Execute(TaskExecutionMode.Script);
                return new DropDatabaseResponse
                {
                    Script = operation.ScriptContent ?? string.Empty,
                };
            }

            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(requestParams.ConnectionUri, out connInfo);
            TaskMetadata metadata = new TaskMetadata
            {
                Name = DropDatabaseOperation.TaskName,
                Description = operation.TaskDescription,
                TaskExecutionMode = TaskExecutionMode.Execute,
                ServerName = connInfo?.ConnectionDetails?.ServerName,
                DatabaseName = requestParams.Database ?? connInfo?.ConnectionDetails?.DatabaseName,
                TaskOperation = operation,
                OwnerUri = requestParams.ConnectionUri,
                OperationName = typeof(DropDatabaseOperation).Name,
            };
            SqlTask sqlTask = SqlTaskManager.Instance.CreateTask<SqlTask>(metadata);
            TaskExecutionResult taskResult = await RunTaskAsync(sqlTask);

            return new DropDatabaseResponse
            {
                TaskId = taskResult.TaskId,
                ErrorMessage = taskResult.ErrorMessage,
            };
        }

        internal async Task<PurgeQueryStoreDataRequestResponse> HandlePurgeQueryStoreDataRequest(PurgeQueryStoreDataRequestParams requestParams)
        {
            var handler = this.GetObjectTypeHandler(SqlObjectType.Database) as DatabaseHandler;
            handler.PurgeQueryStoreData(requestParams);
            return new PurgeQueryStoreDataRequestResponse();
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
            throw new NotSupportedException($"No handler found for object type '{objectType.ToString()}'");
        }

        private static async Task<TaskExecutionResult> RunTaskAsync(
            SqlTask sqlTask,
            Exception executionException = null)
        {
            await sqlTask.RunAsync();

            string errorMessage = null;
            if (sqlTask.TaskStatus != SqlTaskStatus.Succeeded)
            {
                errorMessage = executionException?.Message;
                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    errorMessage = sqlTask.Messages.LastOrDefault(m => !string.IsNullOrWhiteSpace(m.Description))?.Description;
                }
            }

            return new TaskExecutionResult
            {
                TaskId = sqlTask.TaskId.ToString(),
                ErrorMessage = errorMessage,
            };
        }

        private SqlObjectViewContext GetContext(string contextId)
        {
            if (contextMap.TryGetValue(contextId, out SqlObjectViewContext context))
            {
                return context;
            }
            throw new ArgumentException($"Context '{contextId}' not found");
        }

        private sealed class TaskExecutionResult
        {
            public string TaskId { get; set; }

            public string ErrorMessage { get; set; }
        }
    }
}
