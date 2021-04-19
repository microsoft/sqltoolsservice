//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AzureMonitor.ServiceLayer.Connection;
using Microsoft.AzureMonitor.ServiceLayer.DataSource;
using Microsoft.AzureMonitor.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.Extensibility;

namespace Microsoft.AzureMonitor.ServiceLayer
{
    /// <summary>
    /// Provides support for starting up a service host. This is a common responsibility
    /// for both the main service program and test driver that interacts with it
    /// </summary>
    public static class HostLoader
    {
        private static readonly object lockObject = new object();
        private static bool _isLoaded;
        private static readonly string[] inclusionList =
        {
            "microsoft.sqltools.hosting.dll",
            "microsoftazuremonitorservicelayer.dll"
        };

        internal static ServiceHost CreateAndStartServiceHost()
        {
            ServiceHost serviceHost = ServiceHost.Instance;
            lock (lockObject)
            {
                if (!_isLoaded)
                {
                    // Grab the instance of the service host
                    serviceHost.Initialize();

                    InitializeRequestHandlersAndServices(serviceHost);

                    // Start the service only after all request handlers are setup. This is vital
                    // as otherwise the Initialize event can be lost - it's processed and discarded before the handler
                    // is hooked up to receive the message
                    serviceHost.Start().Wait();
                    _isLoaded = true;
                }
            }

            return serviceHost;
        }

        private static void InitializeRequestHandlersAndServices(ServiceHost serviceHost)
        {
            // Load extension provider, which currently finds all exports in current DLL. Can be changed to find based
            // on directory or assembly list quite easily in the future
            ExtensionServiceProvider serviceProvider = ExtensionServiceProvider.CreateDefaultServiceProvider(inclusionList);
            serviceProvider.RegisterSingleService(serviceHost);    
            
            // Initialize and register singleton services so they're accessible for any MEF service. In the future, these
            // could be updated to be IComposableServices, which would avoid the requirement to define a singleton instance
            // and instead have MEF handle discovery & loading
            ConnectionService.Instance.InitializeService(serviceHost, new DataSourceFactory());
            serviceProvider.RegisterSingleService(ConnectionService.Instance);
            
            ObjectExplorerService.Instance.InitializeService(serviceHost, ConnectionService.Instance);
            serviceProvider.RegisterSingleService(ObjectExplorerService.Instance);
            
            serviceHost.InitializeRequestHandlers();
        }
    }
}
