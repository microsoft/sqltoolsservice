//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Credentials;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Extensibility;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;

namespace Microsoft.SqlTools.ServiceLayer
{
    /// <summary>
    /// Main application class for SQL Tools API Service Host executable
    /// </summary>
    internal class Program
    {
        private static SqlToolsContext sqlToolsContext;
        private static ServiceHost serviceHost;
        private static ExtensionServiceProvider serviceProvider;
        /// <summary>
        /// Main entry point into the SQL Tools API Service Host
        /// </summary>
        internal static void Main(string[] args)
        {
            // read command-line arguments
            CommandOptions commandOptions = new CommandOptions(args);
            if (commandOptions.ShouldExit)
            {
                return;
            }

            // turn on Verbose logging during early development
            // we need to switch to Normal when preparing for public preview
            Logger.Initialize(minimumLogLevel: LogLevel.Verbose, isEnabled: commandOptions.EnableLogging);
            Logger.Write(LogLevel.Normal, "Starting SQL Tools Service Host");

            // set up the host details and profile paths 
            var hostDetails = new HostDetails(version: new Version(1,0));

            sqlToolsContext = new SqlToolsContext(hostDetails);


            // Grab the instance of the service host
            serviceHost = ServiceHost.Instance;

            // Start the service
            serviceHost.Start().Wait();

            // Initialize the services that will be hosted here
            InitializeServices();

            serviceHost.Initialize();
            serviceHost.WaitForExit();
        }

        private static void InitializeServices()
        {

            // Load extension provider, which currently finds all exports in current DLL. Can be changed to find based
            // on directory or assembly list quite easily in the future
            serviceProvider = ExtensionServiceProvider.CreateDefaultServiceProvider();
            serviceProvider.RegisterSingleService(sqlToolsContext);
            serviceProvider.RegisterSingleService(serviceHost);

            // Initialize and register singleton services so they're accessible for any MEF service. In the future, these
            // could be updated to be IComposableServices, which would avoid the requirement to define a singleton instance
            // and instead have MEF handle discovery & loading
            WorkspaceService<SqlToolsSettings>.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(WorkspaceService<SqlToolsSettings>.Instance);

            LanguageService.Instance.InitializeService(serviceHost, sqlToolsContext);
            serviceProvider.RegisterSingleService(LanguageService.Instance);

            ConnectionService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(ConnectionService.Instance);

            CredentialService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(CredentialService.Instance);

            QueryExecutionService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(QueryExecutionService.Instance);

            InitializeHostedServices(serviceProvider, serviceHost);
        }

        /// <summary>
        /// Internal to support testing. Initializes <see cref="IHostedService"/> instances in the service,
        /// an registers them for their preferred service type
        /// </summary>
        internal static void InitializeHostedServices(RegisteredServiceProvider provider, IProtocolEndpoint host)
        {
            // Pre-register all services before initializing. This ensures that if one service wishes to reference
            // another one during initialization, it will be able to safely do so
            foreach (IHostedService service in provider.GetServices<IHostedService>())
            {
                provider.RegisterSingleService(service.ServiceType, service);
            }

            foreach (IHostedService service in provider.GetServices<IHostedService>())
            {
                // Initialize all hosted services, and register them in the service provider for their requested
                // service type. This ensures that when searching for the ConnectionService you can get it without
                // searching for an IHostedService of type ConnectionService
                service.InitializeService(host);
            }
        }
    }
}
