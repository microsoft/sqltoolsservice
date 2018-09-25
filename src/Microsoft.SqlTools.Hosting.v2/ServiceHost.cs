//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.DataProtocol.Contracts;
using Microsoft.SqlTools.DataProtocol.Contracts.ServerCapabilities;
using Microsoft.SqlTools.Hosting.Channels;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Contracts.Internal;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Utility;

namespace Microsoft.SqlTools.Hosting
{
    public class ServiceHost : IServiceHost
    {
        private const int DefaultShutdownTimeoutSeconds = 120;
        
        #region Fields
        
        private int? shutdownTimeoutSeconds;

        internal readonly List<Func<InitializeParameters, IEventSender, Task>> initCallbacks;
        internal readonly List<Func<object, IEventSender, Task>> shutdownCallbacks;
        internal IJsonRpcHost jsonRpcHost;
        
        #endregion
        
        #region Construction

        /// <summary>
        /// Base constructor
        /// </summary>
        internal ServiceHost()
        {           
            shutdownCallbacks = new List<Func<object, IEventSender, Task>>();
            initCallbacks = new List<Func<InitializeParameters, IEventSender, Task>>();
        }

        /// <summary>
        /// Constructs a new service host that with ability to provide custom protocol channels
        /// </summary>
        /// <param name="protocolChannel">Channel to use for JSON RPC input/output</param>
        public ServiceHost(ChannelBase protocolChannel)
            : this()
        {
            Validate.IsNotNull(nameof(protocolChannel), protocolChannel);
            jsonRpcHost = new JsonRpcHost(protocolChannel);
            
            // Register any request that the service host will handle
            SetEventHandler(ExitNotification.Type, HandleExitNotification, true);
            SetAsyncRequestHandler(ShutdownRequest.Type, HandleShutdownRequest, true);
            SetAsyncRequestHandler(InitializeRequest.Type, HandleInitializeRequest, true);
        }

        /// <summary>
        /// Constructs a new service host intended to be used as a JSON RPC server. StdIn is used
        /// for receiving messages, StdOut is used for sending messages.
        /// </summary>
        /// <returns>Service host as a JSON RPC server over StdI/O</returns>
        public static ServiceHost CreateDefaultServer()
        {
            return new ServiceHost(new StdioServerChannel());
        }
        
        #endregion

        #region Properties
        
        public int ShutdownTimeoutSeconds
        {
            get => shutdownTimeoutSeconds ?? DefaultShutdownTimeoutSeconds;
            set => shutdownTimeoutSeconds = value;
        }

        public InitializeResponse InitializeResponse { get; set; }

        #endregion
        
        #region IServiceHost Implementations
        
        public void RegisterShutdownTask(Func<object, IEventSender, Task> shutdownCallback)
        {
            Validate.IsNotNull(nameof(shutdownCallback), shutdownCallback);
            shutdownCallbacks.Add(shutdownCallback);
        }

        public void RegisterInitializeTask(Func<InitializeParameters, IEventSender, Task> initializeCallback)
        {
            Validate.IsNotNull(nameof(initializeCallback), initializeCallback);
            initCallbacks.Add(initializeCallback);
        }
        
        #endregion
        
        #region IJsonRpcHost Implementation
        
        public void SendEvent<TParams>(
            EventType<TParams> eventType, 
            TParams eventParams)
        {
            jsonRpcHost.SendEvent(eventType, eventParams);
        }

        public Task<TResult> SendRequest<TParams, TResult>(
            RequestType<TParams, TResult> requestType, 
            TParams requestParams)
        {
            return jsonRpcHost.SendRequest(requestType, requestParams);
        }
        
        public void SetAsyncEventHandler<TParams>(
            EventType<TParams> eventType, 
            Func<TParams, EventContext, Task> eventHandler, 
            bool overrideExisting)
        {
            jsonRpcHost.SetAsyncEventHandler(eventType, eventHandler, overrideExisting);
        }
        
        public void SetEventHandler<TParams>(
            EventType<TParams> eventType, 
            Action<TParams, EventContext> eventHandler, 
            bool overrideExisting)
        {
            jsonRpcHost.SetEventHandler(eventType, eventHandler, overrideExisting);
        }
        
        public void SetAsyncRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType, 
            Func<TParams, RequestContext<TResult>, Task> requestHandler,
            bool overrideExisting)
        {
            jsonRpcHost.SetAsyncRequestHandler(requestType, requestHandler, overrideExisting);
        }
        
        public void SetRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType, 
            Action<TParams, RequestContext<TResult>> requestHandler,
            bool overrideExisting)
        {
            jsonRpcHost.SetRequestHandler(requestType, requestHandler, overrideExisting);
        }
        
        public void Start()
        {
            // Start the host
            jsonRpcHost.Start();
        }

        public void Stop()
        {
            jsonRpcHost.Stop();
        }

        public void WaitForExit()
        {
            jsonRpcHost.WaitForExit();
        }

        #endregion

        #region Request Handlers
        
        internal async Task HandleInitializeRequest(InitializeParameters initParams, RequestContext<InitializeResponse> requestContext)
        {
            Logger.Write(TraceEventType.Information, "Service host received intialize request");
            
            // Call all initialize methods provided by the service components
            IEnumerable<Task> initializeTasks = initCallbacks.Select(t => t(initParams, requestContext));
            
            // Respond to initialize once all tasks are completed
            await Task.WhenAll(initializeTasks);

            if (InitializeResponse == null)
            {
                InitializeResponse = new InitializeResponse
                {
                    Capabilities = new ServerCapabilities()
                };
            }
            requestContext.SendResult(InitializeResponse);
        }

        internal void HandleExitNotification(object exitParams, EventContext eventContext)
        {
            // Stop the server channel
            Stop();
        }
        
        internal async Task HandleShutdownRequest(object shutdownParams, RequestContext<object> requestContext)
        {
            Logger.Write(TraceEventType.Information, "Service host received shutdown request");
            
            // Call all the shutdown methods provided by the service components
            IEnumerable<Task> shutdownTasks = shutdownCallbacks.Select(t => t(shutdownParams, requestContext));
            
            // Shutdown once all tasks are completed, or after the timeout expires, whichever comes first
            TimeSpan shutdownTimeout = TimeSpan.FromSeconds(ShutdownTimeoutSeconds);
            await Task.WhenAny(Task.WhenAll(shutdownTasks), Task.Delay(shutdownTimeout));
            requestContext.SendResult(null);
        }

        #endregion
    }
}
