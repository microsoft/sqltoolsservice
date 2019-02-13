//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Credentials;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Cms;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.FileBrowser;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Metadata;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Microsoft.SqlTools.ServiceLayer.Security;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;

namespace Microsoft.SqlTools.ServiceLayer
{
    /// <summary>
    /// Provides support for starting up a service host. This is a common responsibility
    /// for both the main service program and test driver that interacts with it
    /// </summary>
    public static class HostLoader
    {
        private static object lockObject = new object();
        private static bool isLoaded;

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
            ExtensionServiceProvider serviceProvider = ExtensionServiceProvider.CreateDefaultServiceProvider();
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

            EditDataService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(EditDataService.Instance);

            MetadataService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(MetadataService.Instance);

            ScriptingService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(ScriptingService.Instance);

            AdminService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(AdminService.Instance);

            AgentService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(AgentService.Instance);

            DisasterRecoveryService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(DisasterRecoveryService.Instance);

            FileBrowserService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(FileBrowserService.Instance);

            ProfilerService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(ProfilerService.Instance);

            SecurityService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(SecurityService.Instance);

            DacFxService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(DacFxService.Instance);

            CmsService.Instance.InitializeService(serviceHost);
            serviceProvider.RegisterSingleService(CmsService.Instance);

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
