//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.Extensibility
{
    public class ExtensionServiceHost<T> : ServiceHostBase
    {

        private ExtensibleServiceHostOptions<T> options;
        private static bool isLoaded;
        public ExtensionServiceProvider serviceProvider;
        private List<T> initializedServices = new List<T>();

        public ExtensionServiceHost(
        ExtensibleServiceHostOptions<T> options
        ) : base(new StdioServerChannel())
        {
            // Initialize the shutdown activities
            shutdownCallbacks = new List<ShutdownCallback>();
            initializeCallbacks = new List<InitializeCallback>();
            this.options = options;
            // Grab the instance of the service host
            this.Initialize();

            // Start the service only after all request handlers are setup. This is vital
            // as otherwise the Initialize event can be lost - it's processed and discarded before the handler
            // is hooked up to receive the message
            this.Start().Wait();
            isLoaded = true;

        }

        private void Initialize()
        {
            base.Initialize();
            this.serviceProvider = ExtensionServiceProvider.CreateFromAssembliesInDirectory(options.ExtensionServiceAssemblyDirectory, options.ExtensionServiceAssemblyDllFileNames);
            var hostDetails = new HostDetails(
                  name: options.HostName,
                  profileId: options.HostProfileId,
                  version: options.HostVersion);

            SqlToolsContext sqlToolsContext = new SqlToolsContext(hostDetails);
            serviceProvider.RegisterSingleService(sqlToolsContext);
            serviceProvider.RegisterSingleService(this);
            this.InitializeHostedServices();
            this.InitializeRequestHandlers();
        }

        private void InitializeRequestHandlers()
        {
            // Register the requests that this service host will handle
            this.SetRequestHandler(InitializeRequest.Type, this.HandleInitializeRequest);
            this.SetRequestHandler(ShutdownRequest.Type, this.HandleShutdownRequest);
            this.SetRequestHandler(VersionRequest.Type, this.HandleVersionRequest);
        }

        private void InitializeHostedServices()
        {
            // Pre-register all services before initializing. This ensures that if one service wishes to reference
            // another one during initialization, it will be able to safely do so
            foreach (T service in this.serviceProvider.GetServices<T>())
            {
                if(isServiceInitiazlied(service))
                {
                    continue;
                }
                Logger.Verbose("Registering service: " + service.GetType());
                this.RegisterService(service);
            }

            foreach (T service in this.serviceProvider.GetServices<T>())
            {
                if(isServiceInitiazlied(service))
                {
                    continue;
                }
                Logger.Verbose("Initializing service: " + service.GetType());
                // Initialize all hosted services, and register them in the service provider for their requested
                // service type. This ensures that when searching for the ConnectionService you can get it without
                // searching for an IHostedService of type ConnectionService
                this.InitializeService(service);
            }
        }

        private bool isServiceInitiazlied(T service)
        {
            foreach(T s in this.initializedServices)
            {
                if(s.GetType() == service.GetType())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Delegate definition for the host shutdown event
        /// </summary>
        public delegate Task ShutdownCallback(object shutdownParams, RequestContext<object> shutdownRequestContext);

        /// <summary>
        /// Delegate definition for the host initialization event
        /// </summary>
        public delegate Task InitializeCallback(InitializeRequest startupParams, RequestContext<InitializeResult> requestContext);

        private readonly List<ShutdownCallback> shutdownCallbacks;

        private readonly List<InitializeCallback> initializeCallbacks;

        private readonly Version serviceVersion = Assembly.GetEntryAssembly().GetName().Version;

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


        /// <summary>
        /// Handles the shutdown event for the Language Server
        /// </summary>
        private async Task HandleShutdownRequest(object shutdownParams, RequestContext<object> requestContext)
        {
            Logger.Write(TraceEventType.Information, "Service host is shutting down...");

            // Call all the shutdown methods provided by the service components
            Task[] shutdownTasks = shutdownCallbacks.Select(t => t(shutdownParams, requestContext)).ToArray();
            TimeSpan shutdownTimeout = TimeSpan.FromSeconds(options.ShutdownTimeoutInSeconds);
            // shut down once all tasks are completed, or after the timeout expires, whichever comes first.
            await Task.WhenAny(Task.WhenAll(shutdownTasks), Task.Delay(shutdownTimeout)).ContinueWith(t => Environment.Exit(0));
        }

        /// <summary>
        /// Handles the initialization request
        /// </summary>
        private async Task HandleInitializeRequest(InitializeRequest initializeParams, RequestContext<InitializeResult> requestContext)
        {
            try
            {
                // Call all tasks that registered on the initialize request
                var initializeTasks = initializeCallbacks.Select(t => t(initializeParams, requestContext));
                await Task.WhenAll(initializeTasks);

                // TODO: Figure out where this needs to go to be agnostic of the language

                // Send back what this server can do
                await requestContext.SendResult(
                    new InitializeResult
                    {
                        Capabilities = options.ServerCapabilities
                    });
            }
            catch (Exception e)
            {
                Logger.Error(e);
                await requestContext.SendError(e.Message);
            }
        }

        /// <summary>
        /// Handles the version request. Sends back the server version as result.
        /// </summary>
        private async Task HandleVersionRequest(object versionRequestParams, RequestContext<string> requestContext)
        {
            await requestContext.SendResult(serviceVersion.ToString());
        }


        /// <summary>
        /// Loads and initializes the services from the given assemblies
        /// </summary>
        /// <param name="assemblyPaths">path of the dll files</param>
        public void LoadAndIntializeServicesFromAssesmblies(string[] assemblyPaths)
        {
            this.serviceProvider.AddAssemblies<IHostedService>(options.ExtensionServiceAssemblyDirectory, assemblyPaths);
            this.InitializeHostedServices();
        }

        /// <summary>
        /// Registers and initializes the given service
        /// </summary>
        /// <param name="service">service to be initialized</param>
        public void RegisterService(T service)
        {
            this.serviceProvider.RegisterSingleService(service.GetType(), service);
           
        }

        public void InitializeService(T service)
        {
            this.initializedServices.Add(service);
            this.options.InitializeServiceCallback(this, service);
        }

        /// <summary>
        /// Registers and initializes the given services
        /// </summary>
        /// <param name="services">services to be initalized</param>
        public void RegisterAndInitializedServices(IEnumerable<T> services)
        {
            foreach (T service in services)
            {
                this.RegisterService(service);
                this.InitializeService(service);
            }
        }

        /// <summary>
        /// Register and initializes the given service
        /// </summary>
        /// <param name="service">service to be initialized</param>
        public void RegisterAndInitializeService(T service)
        {
            this.RegisterService(service);
            this.InitializeService(service);
        }
    }




    public class ExtensibleServiceHostOptions<T>
    {
        /// <summary>
        /// The folder where the extension service assemblies are located. By default it is 
        /// the folder where the current server assembly is located.
        /// </summary>
        public string ExtensionServiceAssemblyDirectory { get; set; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// The dlls that contain the extension services. 
        /// </summary>
        public string[] ExtensionServiceAssemblyDllFileNames { get; set; } = new string[0];

        /// <summary>
        /// Host name for the services.
        /// </summary>
        public string HostName { get; set; } = HostDetails.DefaultHostName;

        /// <summary>
        ///  Gets the profile ID of the host, used to determine the 
        ///  host-specific profile path.
        /// </summary>
        public string HostProfileId { get; set; } = HostDetails.DefaultHostProfileId;

        /// <summary>
        /// Gets the version of the host.
        /// </summary>
        public Version HostVersion { get; set; } = HostDetails.DefaultHostVersion;

        /// <summary>
        /// Data protocol capabilities that the server supports.
        /// </summary>
        public ServerCapabilities ServerCapabilities { get; set; } = new ServerCapabilities
        {
            DefinitionProvider = false,
            ReferencesProvider = false,
            DocumentFormattingProvider = false,
            DocumentRangeFormattingProvider = false,
            DocumentHighlightProvider = false,
            HoverProvider = false
        };

        /// <summary>
        /// Timeout in seconds for the shutdown request. Default is 120 seconds.
        /// </summary>
        public int ShutdownTimeoutInSeconds { get; set; } = 120;

        public delegate void InitializeService(ExtensionServiceHost<T> serviceHost, T service);
        
        /// <summary>
        /// Service initialization callback. The caller must define this callback to initialize the service.
        /// </summary>
        /// <value></value>
        public InitializeService InitializeServiceCallback { get; set; }
    }
}