//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Sdk.Sfc;

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
            serviceHost.SetRequestHandler(ScriptingRequest.Type, HandleScriptExecuteRequest);
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
        /// Handles request to get select script for an smo object
        /// </summary>
        private async Task HandleScriptSelectRequest(ScriptingParams parameters, RequestContext<ScriptingResult> requestContext)
        {
            try 
            {
                string script = String.Empty;
                ScriptingObject scriptingObject = parameters.ScriptingObjects[0];

                // convert owner uri received from parameters to lookup for its
                // associated connection and build a connection string out of it
                SqlConnection sqlConn = new SqlConnection(parameters.ConnectionString);
                ServerConnection serverConn = new ServerConnection(sqlConn);
                Server server = new Server(serverConn);
                server.DefaultTextMode = true;
                SqlConnectionStringBuilder connStringBuilder = new SqlConnectionStringBuilder(parameters.ConnectionString);
                string urnString = string.Format(
                    "Server[@Name='{0}']/Database[@Name='{1}']/{2}[@Name='{3}' {4}]",
                    server.Name.ToUpper(),
                    connStringBuilder.InitialCatalog,
                    scriptingObject.Type,
                    scriptingObject.Name,
                    scriptingObject.Schema != null ? string.Format("and @Schema = '{0}'", scriptingObject.Schema) : string.Empty);
                Urn urn = new Urn(urnString);
                string name = urn.GetNameForType(scriptingObject.Type);
                if (string.Compare(name, "ServiceBroker", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    script = Scripter.SelectAllValuesFromTransmissionQueue(urn);
                }
                else 
                {
                    if (string.Compare(name, "Queues", StringComparison.CurrentCultureIgnoreCase) == 0 ||
                        string.Compare(name, "SystemQueues", StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        script = Scripter.SelectAllValues(urn);
                    }
                    else 
                    {   
                        Database db = server.Databases[connStringBuilder.InitialCatalog];
                        bool isDw = db.IsSqlDw;
                        script = new Scripter().SelectFromTableOrView(server, urn, isDw);
                    }
                }
                await requestContext.SendResult(new ScriptingResult { Script = script});
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles request to execute start the list objects operation.
        /// </summary>
        private async Task HandleListObjectsRequest(ScriptingListObjectsParams parameters, RequestContext<ScriptingListObjectsResult> requestContext)
        {
            try
            {
                ScriptingListObjectsOperation operation = new ScriptingListObjectsOperation(parameters);
                operation.CompleteNotification += (sender, e) => this.SendEvent(requestContext, ScriptingListObjectsCompleteEvent.Type, e);

                RunTask(requestContext, operation);

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
                // convert owner uri received from parameters to lookup for its
                // associated connection and build a connection string out of it
                ConnectionInfo connInfo;
                ScriptingService.ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                if (connInfo != null)
                {
                    parameters.ConnectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
                }

                // if the scripting operation is for select
                if (parameters.ScriptOptions.ScriptCreateDrop == "ScriptSelect")
                {
                    await this.HandleScriptSelectRequest(parameters, requestContext);
                }
                else
                {
                    ScriptingScriptOperation operation = new ScriptingScriptOperation(parameters);
                    operation.PlanNotification += (sender, e) => this.SendEvent(requestContext, ScriptingPlanNotificationEvent.Type, e);
                    operation.ProgressNotification += (sender, e) => this.SendEvent(requestContext, ScriptingProgressNotificationEvent.Type, e);
                    operation.CompleteNotification += (sender, e) => this.SendScriptingCompleteEvent(requestContext, ScriptingCompleteEvent.Type, e, operation, parameters.ScriptDestination);

                    RunTask(requestContext, operation);
                }
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles request to cancel a script operation.
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
                else
                {
                    Logger.Write(LogLevel.Normal, string.Format("Operation {0} was not found", operation.OperationId));
                }

                await requestContext.SendResult(new ScriptingCancelResult());
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        private async void SendScriptingCompleteEvent<TParams>(RequestContext<ScriptingResult> requestContext, EventType<TParams> eventType, TParams parameters, 
                                                               ScriptingScriptOperation operation, string scriptDestination)
        {
            await Task.Run(async () => 
            {
                await requestContext.SendEvent(eventType, parameters);
                if (scriptDestination == "ToEditor")
                {
                    await requestContext.SendResult(new ScriptingResult { OperationId = operation.OperationId, Script = operation.PublishModel.RawScript });
                }
                else if (scriptDestination == "ToSingleFile")
                {
                    await requestContext.SendResult(new ScriptingResult { OperationId = operation.OperationId });
                }
                else
                {
                    await requestContext.SendError(string.Format("Operation {0} failed", operation.ToString()));
                }
            });
        }

        /// <summary>
        /// Sends a JSON-RPC event.
        /// </summary>
        private void SendEvent<TParams>(IEventSender requestContext, EventType<TParams> eventType, TParams parameters)
        {
            Task.Run(async () => await requestContext.SendEvent(eventType, parameters));
        }

        /// <summary>
        /// Runs the async task that performs the scripting operation.
        /// </summary>
        private void RunTask<T>(RequestContext<T> context, ScriptingOperation operation)
        {
            Task.Run(() =>
            {
                try
                {
                    Debug.Assert(!this.ActiveOperations.ContainsKey(operation.OperationId), "Operation id must be unique");
                    this.ActiveOperations[operation.OperationId] = operation;
                    operation.Execute();
                }
                catch (Exception e)
                {
                    context.SendError(e);
                }
                finally
                {
                    ScriptingOperation temp;
                    this.ActiveOperations.TryRemove(operation.OperationId, out temp);
                }
            });
        }

        /// <summary>
        /// Disposes the scripting service and all active scripting operations.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                
                foreach (ScriptingScriptOperation operation in this.ActiveOperations.Values)
                {
                    operation.Dispose();
                }
            }
        }
    }
}