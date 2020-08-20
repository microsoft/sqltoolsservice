//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Credentials;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.Kusto.ServiceLayer.Admin;
using Microsoft.Kusto.ServiceLayer.Metadata;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.Hosting;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.QueryExecution;
using Microsoft.Kusto.ServiceLayer.Scripting;
using Microsoft.Kusto.ServiceLayer.SqlContext;
using Microsoft.Kusto.ServiceLayer.Workspace;
using SqlToolsContext = Microsoft.SqlTools.ServiceLayer.SqlContext.SqlToolsContext;

namespace Microsoft.Kusto.ServiceLayer
{
    /// <summary>
    /// Provides support for starting up a service host. This is a common responsibility
    /// for both the main service program and test driver that interacts with it
    /// </summary>
    public static class HostLoader
    {
        private static object lockObject = new object();
        private static bool isLoaded;

        private static readonly string[] inclusionList =
        {
            "microsofsqltoolscredentials.dll",
            "microsoft.sqltools.hosting.dll",
            "microsoftkustoservicelayer.dll"
        };

        internal static ServiceHost CreateAndStartServiceHost(SqlToolsContext sqlToolsContext)
        {
            ServiceHost serviceHost = ServiceHost.Instance;
            lock (lockObject)
            {
                if (!isLoaded)
                {
                    // Grab the instance of the service host
                    serviceHost.Initialize();

                    InitializeRequestHandlersAndServices(serviceHost, sqlToolsContext);

                    // Start the service only after all request handlers are setup. This is vital
                    // as otherwise the Initialize event can be lost - it's processed and discarded before the handler
                    // is hooked up to receive the message
                    serviceHost.Start().Wait();
                    isLoaded = true;
                }
            }
            return serviceHost;
        }

        private static void InitializeRequestHandlersAndServices(ServiceHost serviceHost, SqlToolsContext sqlToolsContext)
        {
            // Load extension provider, which currently finds all exports in current DLL. Can be changed to find based
            // on directory or assembly list quite easily in the future
            ExtensionServiceProvider serviceProvider = ExtensionServiceProvider.CreateDefaultServiceProvider(inclusionList);
            serviceProvider.RegisterSingleService(sqlToolsContext);
            serviceProvider.RegisterSingleService(serviceHost);
            var metadataFactory = serviceProvider.GetService<IMetadataFactory>();
            var dataSourceFactory = serviceProvider.GetService<IDataSourceFactory>();
            var scripter = serviceProvider.GetService<IScripter>();

            // Initialize and register singleton services so they're accessible for any MEF service. In the future, these
            // could be updated to be IComposableServices, which would avoid the requirement to define a singleton instance
            // and instead have MEF handle discovery & loading
            WorkspaceService<SqlToolsSettings>.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(WorkspaceService<SqlToolsSettings>.Instance);

            LanguageService.Instance.InitializeService(serviceHost, sqlToolsContext, dataSourceFactory);
            serviceProvider.RegisterSingleService(LanguageService.Instance);

            ConnectionService.Instance.InitializeService(serviceHost, metadataFactory, dataSourceFactory);
            serviceProvider.RegisterSingleService(ConnectionService.Instance);

            CredentialService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(CredentialService.Instance);

            QueryExecutionService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(QueryExecutionService.Instance);

            ScriptingService.Instance.InitializeService(serviceHost, dataSourceFactory, scripter);
            serviceProvider.RegisterSingleService(ScriptingService.Instance);

            AdminService.Instance.InitializeService(serviceHost, metadataFactory);
            serviceProvider.RegisterSingleService(AdminService.Instance);

            MetadataService.Instance.InitializeService(serviceHost, metadataFactory);
            serviceProvider.RegisterSingleService(MetadataService.Instance);
            
            InitializeHostedServices(serviceProvider, serviceHost);
            serviceHost.ServiceProvider = serviceProvider;

            serviceHost.InitializeRequestHandlers();
        }

        /// <summary>
        /// Internal to support testing. Initializes <see cref="IHostedService"/> instances in the service,
        /// and registers them for their preferred service type
        /// </summary>
        internal static void InitializeHostedServices(RegisteredServiceProvider provider, IProtocolEndpoint host)
        {
            // Pre-register all services before initializing. This ensures that if one service wishes to reference
            // another one during initialization, it will be able to safely do so
            foreach (IHostedService service in provider.GetServices<IHostedService>())
            {
                provider.RegisterSingleService(service.ServiceType, service);
            }

            ServiceHost serviceHost = host as ServiceHost;
            foreach (IHostedService service in provider.GetServices<IHostedService>())
            {
                // Initialize all hosted services, and register them in the service provider for their requested
                // service type. This ensures that when searching for the ConnectionService you can get it without
                // searching for an IHostedService of type ConnectionService
                service.InitializeService(host);

                IDisposable disposable = service as IDisposable;
                if (serviceHost != null && disposable != null)
                {
                    serviceHost.RegisterShutdownTask(async (shutdownParams, shutdownRequestContext) =>
                    {
                        disposable.Dispose();
                        await Task.FromResult(0);
                    });
                }
            }
        }
    }
}
