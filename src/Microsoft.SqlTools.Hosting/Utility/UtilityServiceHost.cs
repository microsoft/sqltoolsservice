//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Protocol.Channel;

namespace Microsoft.SqlTools.Utility
{
    /// <summary>
    /// SQL Tools Service request handler for any utility services. Provides the entire JSON RPC
    /// implementation for sending/receiving JSON requests and dispatching the requests to
    /// handlers that are registered prior to startup.
    /// </summary>
    public sealed class UtilityServiceHost : ServiceHostBase
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
        private static readonly Lazy<UtilityServiceHost> instance = new Lazy<UtilityServiceHost>(() => new UtilityServiceHost());

        /// <summary>
        /// Current instance of the ServiceHost
        /// </summary>
        public static UtilityServiceHost Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Constructs new instance of ServiceHost using the host and profile details provided.
        /// Access is private to ensure only one instance exists at a time.
        /// </summary>
        private UtilityServiceHost() : base(new ServerChannel())
        {
            // Initialize the shutdown activities
            shutdownCallbacks = new List<ShutdownCallback>();
            initializeCallbacks = new List<InitializeCallback>();
        }

        /// <summary>
        /// Provide initialization that must occur after the service host is started
        /// </summary>
        public void InitializeRequestHandlers()
        {
            // Register the requests that this service host will handle
            this.RegisterRequestHandler(InitializeRequest.Type, this.HandleInitializeRequest);
            this.RegisterRequestHandler(ShutdownRequest.Type, this.HandleShutdownRequest);
            this.RegisterRequestHandler(VersionRequest.Type, HandleVersionRequest);
        }

        #endregion

        #region Member Variables

        /// <summary>
        /// Delegate definition for the host shutdown event
        /// </summary>
        /// <param name="shutdownParams"></param>
        public delegate Task ShutdownCallback(object shutdownParams);

        /// <summary>
        /// Delegate definition for the host initialization event
        /// </summary>
        /// <param name="startupParams"></param>
        public delegate Task InitializeCallback(InitializeRequest startupParams);

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
        private async Task<object> HandleShutdownRequest(object shutdownParams)
        {
            Logger.Information("Service host is shutting down...");

            // Call all the shutdown methods provided by the service components
            Task[] shutdownTasks = shutdownCallbacks.Select(t => t(shutdownParams)).ToArray();
            TimeSpan shutdownTimeout = TimeSpan.FromSeconds(ShutdownTimeoutInSeconds);
            // shut down once all tasks are completed, or after the timeout expires, whichever comes first.
            await Task.WhenAny(Task.WhenAll(shutdownTasks), Task.Delay(shutdownTimeout)).ContinueWith(t => Environment.Exit(0));

            return null;
        }

        /// <summary>
        /// Handles the initialization request
        /// </summary>
        /// <param name="initializeParams"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        internal async Task<InitializeResult> HandleInitializeRequest(InitializeRequest initializeParams)
        {
            // Call all tasks that registered on the initialize request
            var initializeTasks = initializeCallbacks.Select(t => t(initializeParams));
            await Task.WhenAll(initializeTasks);

            // TODO: Figure out where this needs to go to be agnostic of the language

            // Send back what this server can do
            return new InitializeResult
                {
                    Capabilities = new ServerCapabilities
                    {
                        DefinitionProvider = false,
                        ReferencesProvider = false,
                        DocumentFormattingProvider = false,
                        DocumentRangeFormattingProvider = false,
                        DocumentHighlightProvider = false,
                        HoverProvider = false
                    }
                };
        }

        /// <summary>
        /// Handles the version request. Sends back the server version as result.
        /// </summary>
        private static Task<string> HandleVersionRequest(
          object versionRequestParams)
        {
            return Task.FromResult(serviceVersion.ToString());
        }

        #endregion
    }
}
