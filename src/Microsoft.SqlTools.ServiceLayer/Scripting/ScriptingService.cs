//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;

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
                ScriptingListObjectsOperation operation = new ScriptingListObjectsOperation(parameters);
                operation.CompleteNotification += (sender, e) => requestContext.SendEvent(ScriptingListObjectsCompleteEvent.Type, e);

                RunTask(requestContext, operation);

                await requestContext.SendResult(new ScriptingListObjectsResult { OperationId = operation.OperationId });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles request to start the scripting operation
        /// </summary>
        public async Task HandleScriptingScriptAsRequest(ScriptingParams parameters, RequestContext<ScriptingResult> requestContext)
        {
            try
            {
                // if a connection string wasn't provided as a parameter then
                // use the owner uri property to lookup its associated ConnectionInfo
                // and then build a connection string out of that
                ConnectionInfo connInfo = null;
                if (parameters.ConnectionString == null || parameters.ScriptOptions.ScriptCreateDrop == "ScriptSelect")
                {
                    ScriptingService.ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);
                    if (connInfo != null)
                    {
                        parameters.ConnectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
                    }
                    else
                    {
                        throw new Exception("Could not find ConnectionInfo");
                    }
                }

                // if the scripting operation is for SELECT then handle that message differently
                // for SELECT we'll build the SQL directly whereas other scripting operations depend on SMO
                if (parameters.ScriptOptions.ScriptCreateDrop == "ScriptSelect")
                {
                    RunSelectTask(connInfo, parameters, requestContext);
                }
                else
                {
                    ScriptAsScriptingOperation operation = new ScriptAsScriptingOperation(parameters);
                    operation.PlanNotification += (sender, e) => requestContext.SendEvent(ScriptingPlanNotificationEvent.Type, e);
                    operation.ProgressNotification += (sender, e) => requestContext.SendEvent(ScriptingProgressNotificationEvent.Type, e);
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
        /// Handles request to start the scripting operation
        /// </summary>
        public async Task HandleScriptExecuteRequest(ScriptingParams parameters, RequestContext<ScriptingResult> requestContext)
        {
            try
            {
                // if a connection string wasn't provided as a parameter then
                // use the owner uri property to lookup its associated ConnectionInfo
                // and then build a connection string out of that
                ConnectionInfo connInfo = null;
                if (parameters.ConnectionString == null || parameters.ScriptOptions.ScriptCreateDrop == "ScriptSelect")
                {
                    ScriptingService.ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);
                    if (connInfo != null)
                    {
                        parameters.ConnectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
                    }
                    else
                    {
                        throw new Exception("Could not find ConnectionInfo");
                    }
                }
                      
                // if the scripting operation is for SELECT then handle that message differently
                // for SELECT we'll build the SQL directly whereas other scripting operations depend on SMO
                if (parameters.ScriptOptions.ScriptCreateDrop == "ScriptSelect")
                {
                    RunSelectTask(connInfo, parameters, requestContext);
                }
                else
                {
                    ScriptingScriptOperation operation = new ScriptingScriptOperation(parameters);
                    operation.PlanNotification += (sender, e) => requestContext.SendEvent(ScriptingPlanNotificationEvent.Type, e);
                    operation.ProgressNotification += (sender, e) => requestContext.SendEvent(ScriptingProgressNotificationEvent.Type, e);
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
            await requestContext.SendEvent(eventType, parameters);
            switch (scriptDestination)
            {
                case "ToEditor":
                    await requestContext.SendResult(new ScriptingResult { OperationId = operation.OperationId, Script = operation.ScriptText });
                    break;
                case "ToSingleFile":
                    await requestContext.SendResult(new ScriptingResult { OperationId = operation.OperationId });
                    break;
                default:
                    await requestContext.SendError(string.Format("Operation {0} failed", operation.ToString()));
                    break;
            }
        }

        private Urn BuildScriptingObjectUrn(
            Server server, 
            SqlConnectionStringBuilder connectionStringBuilder, 
            ScriptingObject scriptingObject)
        {
            string serverName = server.Name.ToUpper();

            // remove the port from server name if specified
            int commaPos = serverName.IndexOf(',');
            if (commaPos >= 0) 
            {
                serverName = serverName.Substring(0, commaPos);
            }

            // build the URN
            string urnString = string.Format(
                "Server[@Name='{0}']/Database[@Name='{1}']/{2}[@Name='{3}' {4}]",
                serverName,
                connectionStringBuilder.InitialCatalog,
                scriptingObject.Type,
                scriptingObject.Name,
                scriptingObject.Schema != null ? string.Format("and @Schema = '{0}'", scriptingObject.Schema) : string.Empty);                  

            return new Urn(urnString);
        }

        /// <summary>
        /// Runs the async task that performs the scripting operation.
        /// </summary>
        private void RunSelectTask(ConnectionInfo connInfo, ScriptingParams parameters, RequestContext<ScriptingResult> requestContext)
        {            
            ConnectionServiceInstance.ConnectionQueue.QueueBindingOperation(
                key: ConnectionServiceInstance.ConnectionQueue.AddConnectionContext(connInfo, "Scripting"),
                bindingTimeout: ScriptingOperationTimeout,
                bindOperation: (bindingContext, cancelToken) =>
                {
                    string script = string.Empty;
                    ScriptingObject scriptingObject = parameters.ScriptingObjects[0];
                    try
                    {
                        Server server = new Server(bindingContext.ServerConnection);
                        server.DefaultTextMode = true;

                        // build object URN
                        SqlConnectionStringBuilder connectionStringBuilder = new SqlConnectionStringBuilder(parameters.ConnectionString);
                        Urn objectUrn = BuildScriptingObjectUrn(server, connectionStringBuilder, scriptingObject);
                        string typeName = objectUrn.GetNameForType(scriptingObject.Type);

                        // select from service broker
                        if (string.Compare(typeName, "ServiceBroker", StringComparison.CurrentCultureIgnoreCase) == 0)
                        {
                            script = Scripter.SelectAllValuesFromTransmissionQueue(objectUrn);
                        }

                        // select from queues
                        else if (string.Compare(typeName, "Queues", StringComparison.CurrentCultureIgnoreCase) == 0 ||
                                 string.Compare(typeName, "SystemQueues", StringComparison.CurrentCultureIgnoreCase) == 0)
                        {
                            script = Scripter.SelectAllValues(objectUrn);
                        }

                        // select from table or view
                        else 
                        {   
                            Database db = server.Databases[connectionStringBuilder.InitialCatalog];
                            bool isDw = db.IsSqlDw;
                            script = new Scripter().SelectFromTableOrView(server, objectUrn, isDw);
                        }

                        // send script result to client
                        requestContext.SendResult(new ScriptingResult { Script = script });
                    }
                    catch (Exception e)
                    {
                        requestContext.SendError(e);
                    }

                    return null;
                });
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
            }).ContinueWithOnFaulted(null);
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
