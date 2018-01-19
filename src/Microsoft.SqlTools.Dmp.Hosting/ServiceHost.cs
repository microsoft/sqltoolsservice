//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Dmp.Contracts;
using Microsoft.SqlTools.Dmp.Contracts.Hosting;
using Microsoft.SqlTools.Dmp.Hosting.Channels;
using Microsoft.SqlTools.Dmp.Hosting.Protocol;
using Microsoft.SqlTools.Dmp.Hosting.Utility;

namespace Microsoft.SqlTools.Dmp.Hosting
{
    public class ServiceHost : IServiceHost
    {
        private const int DefaultShutdownTimeoutSeconds = 120;
        
        #region Fields

        private readonly LanguageServiceCapabilities languageServiceCapabilities;
        private readonly ProviderDetails providerDetails;
        private int? shutdownTimeoutSeconds;

        internal readonly List<Func<InitializeParams, IEventSender, Task>> initializeCallbacks;
        internal readonly List<Func<object, IEventSender, Task>> shutdownCallbacks;
        internal IJsonRpcHost jsonRpcHost;
        
        #endregion
        
        #region Construction

        /// <summary>
        /// Base constructor, allows for providing non-standard json rpc host  
        /// </summary>
        internal ServiceHost(ProviderDetails details, LanguageServiceCapabilities capabilities)
        {
            Validate.IsNotNull(nameof(details), details);
            Validate.IsNotNull(nameof(capabilities), capabilities);
            
            languageServiceCapabilities = capabilities;
            providerDetails = details;
            
            initializeCallbacks = new List<Func<InitializeParams, IEventSender, Task>>();
            shutdownCallbacks = new List<Func<object, IEventSender, Task>>();
        }

        /// <summary>
        /// Constructs a new service host that with ability to provide custom protocol channels
        /// </summary>
        /// <param name="protocolChannel">Channel to use for JSON RPC input/output</param>
        /// <param name="details">Details about the protocol this service host will provide</param>
        /// <param name="capabilities">Language service capabilities</param>
        public ServiceHost(ChannelBase protocolChannel, ProviderDetails details, LanguageServiceCapabilities capabilities)
            : this(details, capabilities)
        {
            Validate.IsNotNull(nameof(protocolChannel), protocolChannel);
            jsonRpcHost = new JsonRpcHost(protocolChannel);
        }

        /// <summary>
        /// Constructs a new service host intended to be used as a JSON RPC server. StdIn is used
        /// for receiving messages, StdOut is used for sending messages.
        /// </summary>
        /// <param name="details">Details about the protocol this service host will provide</param>
        /// <param name="capabilities">Language service capabilities</param>
        /// <returns>Service host as a JSON RPC server over StdI/O</returns>
        public static ServiceHost CreateDefaultServer(ProviderDetails details, LanguageServiceCapabilities capabilities)
        {
            return new ServiceHost(new StdioServerChannel(), details, capabilities);
        }
        
        #endregion

        #region Properties
        
        public int ShutdownTimeoutSeconds
        {
            get => shutdownTimeoutSeconds ?? DefaultShutdownTimeoutSeconds;
            set => shutdownTimeoutSeconds = value;
        }

        #endregion
        
        #region IServiceHost Implementations
        
        public void RegisterInitializeTask(Func<InitializeParams, IEventSender, Task> initializeCallback)
        {
            Validate.IsNotNull(nameof(initializeCallback), initializeCallback);
            initializeCallbacks.Add(initializeCallback);
        }
        
        public void RegisterShutdownTask(Func<object, IEventSender, Task> shutdownCallback)
        {
            Validate.IsNotNull(nameof(shutdownCallback), shutdownCallback);
            shutdownCallbacks.Add(shutdownCallback);
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
            // Register any request that the service host will handle
            SetEventHandler(ExitNotification.Type, HandleExitNotification, true);
            SetAsyncRequestHandler(InitializeRequest.Type, HandleInitializeRequest, true);
            SetAsyncRequestHandler(ShutdownRequest.Type, HandleShutdownRequest, true);
            SetRequestHandler(VersionRequest.Type, HandleVersionRequest, true);
            
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

        internal void HandleExitNotification(object exitParams, EventContext eventContext)
        {
            // Stop the server channel
            Stop();
        }
        
        internal async Task HandleInitializeRequest(InitializeParams initializeParams, RequestContext<InitializeResult> requestContext)
        {
            Logger.Write(LogLevel.Normal, "Service host received initialize request...");
            
            // Call all tasks that registered on the initialize request
            IEnumerable<Task> initTasks = initializeCallbacks.Select(t => t(initializeParams, requestContext));
            await Task.WhenAll(initTasks);
            
            // Respond with what this service host can do as a language service
            requestContext.SendResult(new InitializeResult {Capabilities = languageServiceCapabilities});
        }
        
        internal async Task HandleShutdownRequest(object shutdownParams, RequestContext<object> requestContext)
        {
            Logger.Write(LogLevel.Normal, "Service host received shutdown request");
            
            // Call all the shutdown methods provided by the service components
            IEnumerable<Task> shutdownTasks = shutdownCallbacks.Select(t => t(shutdownParams, requestContext));
            
            // Shutdown once all tasks are completed, or after the timeout expires, whichever comes first
            TimeSpan shutdownTimeout = TimeSpan.FromSeconds(ShutdownTimeoutSeconds);
            await Task.WhenAny(Task.WhenAll(shutdownTasks), Task.Delay(shutdownTimeout));
            requestContext.SendResult(null);
        }

        internal void HandleVersionRequest(object versionParams, RequestContext<string> requestContext)
        {
            requestContext.SendResult(providerDetails.ProviderProtocolVersion);
        }

        #endregion
    }
}

