//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.Utility;
using static Microsoft.SqlTools.Utility.SqlConstants;
using System.Linq;
using Microsoft.SqlTools.SqlCore.Scripting;
using Microsoft.SqlTools.SqlCore.Scripting.Contracts;

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

        private readonly Lazy<ConcurrentDictionary<string, ScriptingOperation>> operations =
            new Lazy<ConcurrentDictionary<string, ScriptingOperation>>(() => new ConcurrentDictionary<string, ScriptingOperation>());

        private IEventSender eventSender;

        private bool disposed;

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
            this.eventSender = serviceHost;

            serviceHost.RegisterRequestHandler(ScriptingRequest.Type, this.HandleScriptExecuteRequest);
            serviceHost.RegisterRequestHandler(ScriptingCancelRequest.Type, this.HandleScriptCancelRequest);
            serviceHost.RegisterRequestHandler(ScriptingListObjectsRequest.Type, this.HandleListObjectsRequest);

            // Register handler for shutdown event
            serviceHost.RegisterShutdownTask(shutdownParams =>
            {
                this.Dispose();
                return Task.FromResult(0);
            });
        }

        /// <summary>
        /// Handles request to execute start the list objects operation.
        /// </summary>
        private async Task<ScriptingListObjectsResult> HandleListObjectsRequest(ScriptingListObjectsParams parameters)
        {
            ScriptingListObjectsOperation operation = new ScriptingListObjectsOperation(parameters);
            operation.CompleteNotification += async (sender, e) => await this.SendEvent(ScriptingListObjectsCompleteEvent.Type, e);

            RunTask(operation);

            return new ScriptingListObjectsResult { OperationId = operation.OperationId };
        }

        /// <summary>
        /// Handles request to start the scripting operation
        /// </summary>
        public Task<ScriptingResult> HandleScriptExecuteRequest(ScriptingParams parameters)
        {
            SmoScriptingOperation operation = null;
            // if a connection string wasn't provided as a parameter then
            // use the owner uri property to lookup its associated ConnectionInfo
            // and then build a connection string out of that
            ConnectionInfo connInfo = null;
            string accessToken = null;
            ServerConnection scriptingServerConnection = null;

            if (parameters.ConnectionString == null)
            {
                ScriptingService.ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);

                if (connInfo != null)
                {
                    parameters.ConnectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);

                    // Set Access Token only when authentication type is AzureMFA.
                    if (connInfo.ConnectionDetails.AuthenticationType == AzureMFA)
                    {
                        // If using AzureTokenFetcher, get the token and open a ServerConnection that can be used for scripting with SMO.
                        if (connInfo.AzureTokenFetcher != null)
                        {
                            scriptingServerConnection = ConnectionServiceInstance.OpenServerConnectionInternal(connInfo);
                            (accessToken, _) = connInfo.AzureTokenFetcher().GetAwaiter().GetResult();
                        }
                        else
                        {
                            accessToken = connInfo.ConnectionDetails.AzureAccountToken;
                        }
                    }
                }
                else
                {
                    throw new Exception("Could not find ConnectionInfo");
                }
            }

            // Create a temporary and random path to handle this operation
            parameters.FilePath ??= Path.GetTempFileName();

            if (!ShouldCreateScriptAsOperation(parameters))
            {
                operation = new ScriptingScriptOperation(parameters, accessToken);
            }
            else
            {
                operation = scriptingServerConnection != null
                    ? new ScriptAsScriptingOperation(parameters, scriptingServerConnection)
                    : new ScriptAsScriptingOperation(parameters, accessToken);
            }

            TaskCompletionSource<ScriptingResult> completionSource =
                parameters.ReturnScriptAsynchronously ? null : new TaskCompletionSource<ScriptingResult>();

            operation.PlanNotification += async (sender, e) => await this.SendEvent(ScriptingPlanNotificationEvent.Type, e);
            operation.ProgressNotification += async (sender, e) => await this.SendEvent(ScriptingProgressNotificationEvent.Type, e);
            operation.CompleteNotification += async (sender, e) => await this.SendScriptingCompleteEvent(ScriptingCompleteEvent.Type, e, operation, parameters, completionSource);

            RunTask(operation, e => completionSource?.TrySetException(RpcErrorException.Create(e)));

            // If ReturnScriptAsynchronously is enabled, return operation ID immediately
            if (parameters.ReturnScriptAsynchronously)
            {
                return Task.FromResult(new ScriptingResult { OperationId = operation.OperationId });
            }

            return completionSource.Task;
        }

        private bool ShouldCreateScriptAsOperation(ScriptingParams parameters)
        {
            // Scripting as operation should be used to script one object.
            // Scripting data and scripting to file is not supported by scripting as operation
            // To script Select, alter and execute use scripting as operation. The other operation doesn't support those types
            if ((parameters.ScriptingObjects != null && parameters.ScriptingObjects.Count == 1 && parameters.ScriptOptions != null
                && parameters.ScriptOptions.TypeOfDataToScript == "SchemaOnly" && parameters.ScriptDestination == "ToEditor") ||
                parameters.Operation == ScriptingOperationType.Select || parameters.Operation == ScriptingOperationType.Execute ||
                parameters.Operation == ScriptingOperationType.Alter)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Handles request to cancel a script operation.
        /// </summary>
        public async Task<ScriptingCancelResult> HandleScriptCancelRequest(ScriptingCancelParams parameters)
        {
            ScriptingOperation operation = null;
            if (this.ActiveOperations.TryRemove(parameters.OperationId, out operation))
            {
                operation.Cancel();
            }
            else
            {
                Logger.Information(string.Format("Operation {0} was not found", operation.OperationId));
            }

            return new ScriptingCancelResult();
        }

        private async Task SendScriptingCompleteEvent(
            EventType<ScriptingCompleteParams> eventType,
            ScriptingCompleteParams parameters,
            SmoScriptingOperation operation,
            ScriptingParams scriptingParams,
            TaskCompletionSource<ScriptingResult> completionSource)
        {
            // If ReturnScriptAsynchronously is enabled, include script in the complete event
            if (scriptingParams.ReturnScriptAsynchronously)
            {
                parameters.Script = operation.ScriptText;
                await this.SendEvent(eventType, parameters);
                return;
            }

            await this.SendEvent(eventType, parameters);

            if (parameters.HasError)
            {
                completionSource?.TrySetException(RpcErrorException.Create(parameters.ErrorMessage));
                return;
            }

            switch (scriptingParams.ScriptDestination)
            {
                case "ToEditor":
                    completionSource?.TrySetResult(new ScriptingResult { OperationId = operation.OperationId, Script = operation.ScriptText });
                    return;
                case "ToSingleFile":
                    completionSource?.TrySetResult(new ScriptingResult { OperationId = operation.OperationId });
                    return;
                default:
                    completionSource?.TrySetException(RpcErrorException.Create(string.Format("Operation {0} failed", operation)));
                    return;
            }
        }

        /// <summary>
        /// Runs the async task that performs the scripting operation.
        /// </summary>
        private void RunTask(ScriptingOperation operation, Action<Exception> failureHandler = null)
        {
            ScriptingTask = Task.Run(async () =>
            {
                try
                {
                    this.ActiveOperations[operation.OperationId] = operation;
                    operation.Execute();
                }
                catch (Exception e)
                {
                    failureHandler?.Invoke(e);
                    throw;
                }
                finally
                {
                    ScriptingOperation temp;
                    this.ActiveOperations.TryRemove(operation.OperationId, out temp);
                }
            }).ContinueWithOnFaulted(t => Logger.Error(t.Exception.ToString()));
        }

        private Task SendEvent<TParams>(EventType<TParams> eventType, TParams parameters)
        {
            return this.eventSender?.SendEvent(eventType, parameters) ?? Task.CompletedTask;
        }

        internal Task ScriptingTask { get; set; }

        /// <summary>
        /// Disposes the scripting service and all active scripting operations.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                foreach (ScriptingScriptOperation operation in this.ActiveOperations.Values.Cast<ScriptingScriptOperation>())
                {
                    operation.Dispose();
                }
            }
        }
    }
}
