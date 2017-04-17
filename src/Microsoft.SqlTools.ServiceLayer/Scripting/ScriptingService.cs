//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Main class for Scripting Service functionality
    /// </summary>
    public sealed class ScriptingService : IDisposable
    {    
        private const int ScriptingOperationTimeout = 60000;

        private static readonly Lazy<ScriptingService> LazyInstance = new Lazy<ScriptingService>(() => new ScriptingService());

        public static ScriptingService Instance => LazyInstance.Value;

        private static ConnectionService connectionService = null;        

        private static LanguageService languageServices = null;

        private readonly Lazy<ConcurrentDictionary<string, ScriptingOperation>> operations =
            new Lazy<ConcurrentDictionary<string, ScriptingOperation>>(() => new ConcurrentDictionary<string, ScriptingOperation>());

        private bool disposed;

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }     

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static LanguageService LanguageServiceInstance
        {
            get
            {
                if (languageServices == null)
                {
                    languageServices = LanguageService.Instance;
                }
                return languageServices;
            }
            set
            {
                languageServices = value;
            }
        }

        /// <summary>
        /// The collection of active operations
        /// </summary>
        internal ConcurrentDictionary<string, ScriptingOperation> ActiveOperations => operations.Value;

        /// <summary>
        /// Initializes the Scripting Service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        /// <param name="context"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(ScriptingScriptAsRequest.Type, HandleScriptingScriptAsRequest);
            serviceHost.SetRequestHandler(ScriptingRequest.Type, this.HandleScriptExecuteRequest);
            serviceHost.SetRequestHandler(ScriptingCancelRequest.Type, this.HandleScriptCancelRequest);
            serviceHost.SetRequestHandler(ScriptingListObjectsRequest.Type, this.HandleListObjectsRequest);

            // Register handler for shutdown event
            serviceHost.RegisterShutdownTask((shutdownParams, requestContext) =>
            {
                this.Dispose();
                return Task.FromResult(0);
            });
        }

        /// <summary>
        /// Handles request to execute start the list objects operation.
        /// </summary>
        private async Task HandleListObjectsRequest(ScriptingListObjectsParams parameters, RequestContext<ScriptingListObjectsResult> requestContext)
        {
            try
            {
                ScriptingListObjectsOperation operation = new ScriptingListObjectsOperation(parameters, requestContext);
                RunListObjectsTask(operation);
                await requestContext.SendResult(new ScriptingListObjectsResult { OperationId = operation.OperationId });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles request to execute start the script operation.
        /// </summary>
        public async Task HandleScriptExecuteRequest(ScriptingParams parameters, RequestContext<ScriptingResult> requestContext)
        {
            try
            {
                ScriptingScriptOperation operation = new ScriptingScriptOperation(parameters, requestContext);
                RunScriptTask(operation);
                await requestContext.SendResult(new ScriptingResult { OperationId = operation.OperationId });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles request to cancel the database script operation
        /// </summary>
        public async Task HandleScriptCancelRequest(ScriptingCancelParams parameters, RequestContext<ScriptingCancelResult> requestContext)
        {
            try
            {
                ScriptingOperation operation = null;
                if (this.ActiveOperations.TryRemove(parameters.OperationId, out operation))
                {
                    operation.Cancel();
                }

                await requestContext.SendResult(new ScriptingCancelResult());
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Runs the async task to list scriptable objects.
        /// </summary>
        private void RunListObjectsTask(ScriptingListObjectsOperation operation)
        {
            Task.Run(async () =>
            {
                try
                {
                    this.ActiveOperations[operation.OperationId] = operation;
                    await operation.Execute();
                }
                catch (Exception e)
                {
                    await operation.RequestContext.SendError(e);
                }
                finally
                {
                    ScriptingOperation temp;
                    this.ActiveOperations.TryRemove(operation.OperationId, out temp);
                }
            });
        }

        /// <summary>
        /// Runs the async task to script objects.
        /// </summary>
        private void RunScriptTask(ScriptingScriptOperation operation)
        {
            Task.Run(async () =>
            {
                try
                {
                    this.ActiveOperations[operation.OperationId] = operation;
                    await operation.Execute();
                }
                catch (Exception e)
                {
                    await operation.RequestContext.SendError(e);
                }
                finally
                {
                    ScriptingOperation temp;
                    this.ActiveOperations.TryRemove(operation.OperationId, out temp);
                }
            });
        }
        public void Dispose()
        {
            if (!disposed)
            {
                foreach (ScriptingScriptOperation operation in this.ActiveOperations.Values)
                {
                    operation.Dispose();
                }

                disposed = true;
            }
        }

        /// <summary>
        /// Script create statements for metadata object
        /// </summary>
        private static string ScriptAsCreate(
            IBindingContext bindingContext,
            ConnectionInfo connInfo,
            ObjectMetadata metadata)
        {
            Scripter scripter = new Scripter(bindingContext.ServerConnection, connInfo);
            StringCollection results = null;
            if (metadata.MetadataType == MetadataType.Table)
            {
                results = scripter.GetTableScripts(metadata.Name, metadata.Schema);
            }
            else if (metadata.MetadataType == MetadataType.SProc)
            {
                results = scripter.GetStoredProcedureScripts(metadata.Name, metadata.Schema);
            }
            else if (metadata.MetadataType == MetadataType.View)
            {
                results = scripter.GetViewScripts(metadata.Name, metadata.Schema);
            }

            StringBuilder builder = null;
            if (results != null) 
            {                                
                builder = new StringBuilder();                             
                foreach (var result in results)
                {
                    builder.AppendLine(result);
                    builder.AppendLine();
                }
            }
            return builder != null ? builder.ToString() : null;
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        private static string ScriptAsUpdate(
            IBindingContext bindingContext,
            ConnectionInfo connInfo,
            ObjectMetadata metadata)
        {
            return null;
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        private static string ScriptAsInsert(
            IBindingContext bindingContext,
            ConnectionInfo connInfo,
            ObjectMetadata metadata)
        {
            return null;
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        private static string ScriptAsDelete(
            IBindingContext bindingContext,
            ConnectionInfo connInfo,
            ObjectMetadata metadata)
        {
            return null;
        }

        /// <summary>
        /// Handle Script As Update requests
        /// </summary>
        private static string QueueScriptOperation(
            ScriptOperation operation,
            ConnectionInfo connInfo,
            ObjectMetadata metadata)
        {
            // get or create the current parse info object
            ScriptParseInfo parseInfo = LanguageServiceInstance.GetScriptParseInfo(connInfo.OwnerUri);
            if (Monitor.TryEnter(parseInfo.BuildingMetadataLock, LanguageService.BindingTimeout))
            {
                try
                {
                    QueueItem queueItem = LanguageServiceInstance.BindingQueue.QueueBindingOperation(
                        key: parseInfo.ConnectionKey,
                        bindingTimeout: ScriptingService.ScriptingOperationTimeout,
                        bindOperation: (bindingContext, cancelToken) =>
                        {
                            if (operation == ScriptOperation.Select)
                            {                    
                                return string.Format(
                                    @"SELECT TOP 100 * " + Environment.NewLine + @"FROM {0}.{1}",
                                    metadata.Schema, metadata.Name);
                            }
                            else if (operation == ScriptOperation.Create)
                            {
                                return ScriptAsCreate(bindingContext, connInfo, metadata);
                            }
                            else if (operation == ScriptOperation.Update)
                            {
                                return ScriptAsUpdate(bindingContext, connInfo, metadata);
                            }
                            else if (operation == ScriptOperation.Insert)
                            {
                                return ScriptAsInsert(bindingContext, connInfo, metadata);
                            }
                            else if (operation == ScriptOperation.Delete)
                            {
                               return ScriptAsDelete(bindingContext, connInfo, metadata);
                            }
                            else
                            {
                                return null;
                            }
                        });

                    queueItem.ItemProcessed.WaitOne();

                    return queueItem.GetResultAsT<string>();
                }
                finally
                {
                    Monitor.Exit(parseInfo.BuildingMetadataLock);
                }
            }

            return string.Empty;
        }        

        /// <summary>
        /// Handles script as request messages
        /// </summary>
        /// <param name="scriptingParams"></param>
        /// <param name="requestContext"></param>
        internal static async Task HandleScriptingScriptAsRequest(
            ScriptingScriptAsParams scriptingParams,
            RequestContext<ScriptingScriptAsResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                ScriptingService.ConnectionServiceInstance.TryFindConnection(
                    scriptingParams.OwnerUri,
                    out connInfo);

                ObjectMetadata metadata = scriptingParams.Metadata;
                string script = string.Empty;

                if (connInfo != null) 
                {
                    script = QueueScriptOperation(scriptingParams.Operation, connInfo, metadata);
                }

                await requestContext.SendResult(new ScriptingScriptAsResult
                {
                    OwnerUri = scriptingParams.OwnerUri,
                    Script = script
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }
    }
}
