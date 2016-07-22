//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlTools.EditorServices.Utility;
using Microsoft.SqlTools.ServiceLayer.ServiceHost.Contracts;
using Microsoft.SqlTools.ServiceLayer.ServiceHost.Protocol;
using Microsoft.SqlTools.ServiceLayer.ServiceHost.Protocol.Channel;

namespace Microsoft.SqlTools.ServiceLayer.ServiceHost
{
    /// <summary>
    /// SQL Tools VS Code Language Server request handler
    /// </summary>
    public class ServiceHost : ServiceHostBase
    {
        #region Singleton Instance Code

        /// <summary>
        /// Singleton instance of the instance
        /// </summary>
        private static ServiceHost instance;

        /// <summary>
        /// Creates or retrieves the current instance of the ServiceHost
        /// </summary>
        /// <returns>Instance of the service host</returns>
        public static ServiceHost Create()
        {
            if (instance == null)
            {
                instance = new ServiceHost();
            }
            return instance;
        }

        /// <summary>
        /// Constructs new instance of ServiceHost using the host and profile details provided.
        /// Access is private to ensure only one instance exists at a time.
        /// </summary>
        private ServiceHost() : base(new StdioServerChannel())
        {
            // Initialize the shutdown activities
            shutdownActivities = new List<ShutdownHandler>();
            initializeActivities = new List<InitializeHandler>();

            // Register the requests that this service host will handle
            this.SetRequestHandler(InitializeRequest.Type, this.HandleInitializeRequest);
            this.SetRequestHandler(ShutdownRequest.Type, this.HandleShutdownRequest);
        }

        #endregion

        #region Member Variables

        public delegate Task ShutdownHandler(object shutdownParams, RequestContext<object> shutdownRequestContext);

        public delegate Task InitializeHandler(InitializeRequest startupParams, RequestContext<InitializeResult> requestContext);

        private readonly List<ShutdownHandler> shutdownActivities;

        private readonly List<InitializeHandler> initializeActivities;

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a new method to be called when the shutdown request is submitted
        /// </summary>
        /// <param name="activity"></param>
        public void RegisterShutdownTask(ShutdownHandler activity)
        {
            shutdownActivities.Add(activity);
        }

        /// <summary>
        /// Add a new method to be called when the initialize request is submitted
        /// </summary>
        /// <param name="activity"></param>
        public void RegisterInitializeTask(InitializeHandler activity)
        {
            initializeActivities.Add(activity);
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
            Task[] shutdownTasks = shutdownActivities.Select(t => t(shutdownParams, requestContext)).ToArray();
            await Task.WhenAll(shutdownTasks);
        }

        /// <summary>
        /// Handles the initialization request
        /// </summary>
        /// <param name="initializeParams"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        private async Task HandleInitializeRequest(InitializeRequest initializeParams, RequestContext<InitializeResult> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleInitializationRequest");

            // Call all tasks that registered on the initialize request
            var initializeTasks = initializeActivities.Select(t => t(initializeParams, requestContext));
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
                        DocumentSymbolProvider = true,
                        WorkspaceSymbolProvider = true,
                        HoverProvider = true,
                        CompletionProvider = new CompletionOptions
                        {
                            ResolveProvider = true,
                            TriggerCharacters = new string[] { ".", "-", ":", "\\" }
                        },
                        SignatureHelpProvider = new SignatureHelpOptions
                        {
                            TriggerCharacters = new string[] { " " } // TODO: Other characters here?
                        }
                    }
                });
        }

        #endregion
    }
}
