//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Channel;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Hosting
{
    /// <summary>
    /// SQL Tools VS Code Language Server request handler. Provides the entire JSON RPC
    /// implementation for sending/receiving JSON requests and dispatching the requests to
    /// handlers that are registered prior to startup.
    /// </summary>
    public sealed class ServiceHost : ServiceHostBase
    {
        /// <summary>
        /// This timeout limits the amount of time that shutdown tasks can take to complete
        /// prior to the process shutting down.
        /// </summary>
        private const int ShutdownTimeoutInSeconds = 120;

        #region Singleton Instance Code

        /// <summary>
        /// Singleton instance of the service host for internal storage
        /// </summary>
        private static readonly Lazy<ServiceHost> instance = new Lazy<ServiceHost>(() => new ServiceHost());

        /// <summary>
        /// Current instance of the ServiceHost
        /// </summary>
        public static ServiceHost Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Constructs new instance of ServiceHost using the host and profile details provided.
        /// Access is private to ensure only one instance exists at a time.
        /// </summary>
        private ServiceHost() : base(new StdioServerChannel())
        {
            // Initialize the shutdown activities
            shutdownCallbacks = new List<ShutdownCallback>();
            initializeCallbacks = new List<InitializeCallback>();
        }

        /// <summary>
        /// Provide initialization that must occur after the service host is started
        /// </summary>
        public void Initialize()
        {
            // Register the requests that this service host will handle
            this.SetRequestHandler(InitializeRequest.Type, this.HandleInitializeRequest);
            this.SetRequestHandler(ShutdownRequest.Type, this.HandleShutdownRequest);
            this.SetRequestHandler(VersionRequest.Type, HandleVersionRequest);
        }

        #endregion

        #region Member Variables

        /// <summary>
        /// Delegate definition for the host shutdown event
        /// </summary>
        /// <param name="shutdownParams"></param>
        /// <param name="shutdownRequestContext"></param>
        public delegate Task ShutdownCallback(object shutdownParams, RequestContext<object> shutdownRequestContext);

        /// <summary>
        /// Delegate definition for the host initialization event
        /// </summary>
        /// <param name="startupParams"></param>
        /// <param name="requestContext"></param>
        public delegate Task InitializeCallback(InitializeRequest startupParams, RequestContext<InitializeResult> requestContext);

        private readonly List<ShutdownCallback> shutdownCallbacks;

        private readonly List<InitializeCallback> initializeCallbacks;

        private static readonly Version serviceVersion = Assembly.GetEntryAssembly().GetName().Version;

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a new callback to be called when the shutdown request is submitted
        /// </summary>
        /// <param name="callback">Callback to perform when a shutdown request is submitted</param>
        public void RegisterShutdownTask(ShutdownCallback callback)
        {
            shutdownCallbacks.Add(callback);
        }

        /// <summary>
        /// Add a new method to be called when the initialize request is submitted
        /// </summary>
        /// <param name="callback">Callback to perform when an initialize request is submitted</param>
        public void RegisterInitializeTask(InitializeCallback callback)
        {
            initializeCallbacks.Add(callback);
        }

        #endregion

        #region Request Handlers

        /// <summary>
        /// Handles the shutdown event for the Language Server
        /// </summary>
        private async Task HandleShutdownRequest(object shutdownParams, RequestContext<object> requestContext)
        {
            Logger.Write(LogLevel.Normal, "Service host is shutting down...");

            // Call all the shutdown methods provided by the service components
            Task[] shutdownTasks = shutdownCallbacks.Select(t => t(shutdownParams, requestContext)).ToArray();
            TimeSpan shutdownTimeout = TimeSpan.FromSeconds(ShutdownTimeoutInSeconds);
            // shut down once all tasks are completed, or after the timeout expires, whichever comes first.
            await Task.WhenAny(Task.WhenAll(shutdownTasks), Task.Delay(shutdownTimeout)).ContinueWith(t => Environment.Exit(0));
        }

        /// <summary>
        /// Handles the initialization request
        /// </summary>
        /// <param name="initializeParams"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        private async Task HandleInitializeRequest(InitializeRequest initializeParams, RequestContext<InitializeResult> requestContext)
        {
            // Call all tasks that registered on the initialize request
            var initializeTasks = initializeCallbacks.Select(t => t(initializeParams, requestContext));
            await Task.WhenAll(initializeTasks);

            // TODO: Figure out where this needs to go to be agnostic of the language

            // Send back what this server can do
            await requestContext.SendResult(
                new InitializeResult
                {
                    Capabilities = new ServerCapabilities
                    {
                        TextDocumentSync = TextDocumentSyncKind.Incremental,
                        DefinitionProvider = true,
                        ReferencesProvider = true,
                        DocumentHighlightProvider = true,
                        HoverProvider = true,             
                        CompletionProvider = new CompletionOptions
                        {
                            ResolveProvider = true,
                            TriggerCharacters = new string[] { ".", "-", ":", "\\", ",", " " }
                        },
                        SignatureHelpProvider = new SignatureHelpOptions
                        {
                            TriggerCharacters = new string[] { " ", "," }
                        }
                    }
                });
        }

        /// <summary>
        /// Handles the version request. Sends back the server version as result.
        /// </summary>
        private static async Task HandleVersionRequest(
          object versionRequestParams,
          RequestContext<string> requestContext)
        {
            await requestContext.SendResult(serviceVersion.ToString());
        }

        #endregion
    }
}
