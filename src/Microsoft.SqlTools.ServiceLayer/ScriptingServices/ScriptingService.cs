//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.ScriptingServices.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ScriptingServices
{
    /// <summary>
    /// Service for scripting a database and/or database objects.
    /// </summary>
    public sealed class ScriptingService : IDisposable
    {
        private static readonly Lazy<ScriptingService> instance = new Lazy<ScriptingService>(() => new ScriptingService());

        private bool disposed;

        private readonly Lazy<ConcurrentDictionary<string, ScriptingOperation>> operations = 
            new Lazy<ConcurrentDictionary<string, ScriptingOperation>>(() => new ConcurrentDictionary<string, ScriptingOperation>());

        /// <summary>
        /// Singleton instance of the query execution service
        /// </summary>
        public static ScriptingService Instance { get { return instance.Value; } }

        internal ScriptingService() { }

        /// <summary>
        /// The collection of active operations
        /// </summary>
        internal ConcurrentDictionary<string, ScriptingOperation> ActiveOperations { get { return operations.Value; } }

        /// <summary>
        /// Initializes the service with the service host, registers request handlers and shutdown event handler.
        /// </summary>
        /// <param name="serviceHost">The service host instance to register with.</param>
        public void InitializeService(ServiceHost serviceHost)
        {
            // Register handlers for requests
            serviceHost.SetRequestHandler(ScriptingRequest.Type, HandleExecuteRequest);
            serviceHost.SetRequestHandler(ScriptingCancelRequest.Type, HandleDatabaseScriptCancelRequest);

            // Register handler for shutdown event
            serviceHost.RegisterShutdownTask((shutdownParams, requestContext) =>
            {
                this.Dispose();
                return Task.FromResult(0);
            });
        }

        /// <summary>
        /// Handles request to execute start the database script operation.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Await.Warning", "CS4014:Await.Warning", Justification = "Not using await for ScriptingOperation.Execute() since this a long running operation.")]
        public async Task HandleExecuteRequest(ScriptingParams parameters, RequestContext<ScriptingResult> requestContext)
        {
            try
            {
                ScriptingOperation operation = new ScriptingOperation(parameters, requestContext);
                this.ActiveOperations[operation.OperationId] = operation;
                await requestContext.SendResult(new ScriptingResult { OperationId = operation.OperationId });
                operation.Execute().ContinueWith((t) => this.ActiveOperations.TryRemove(operation.OperationId, out operation));
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        /// <summary>
        /// Handles request to cancel the database script operation
        /// </summary>
        public async Task HandleDatabaseScriptCancelRequest(ScriptingCancelParams parameters, RequestContext<ScriptingCancelResult> requestContext)
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
                await requestContext.SendError(e.Message);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                foreach (ScriptingOperation operation in this.ActiveOperations.Values)
                {
                    operation.Dispose();
                }

                disposed = true;
            }
        }
    }
}
