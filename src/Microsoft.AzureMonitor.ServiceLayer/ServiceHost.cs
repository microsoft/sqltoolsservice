using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.Utility;

namespace Microsoft.AzureMonitor.ServiceLayer
{
    public class ServiceHost : ServiceHostBase
    {
        private const string ProviderName = "AzureMonitor";
        private const string ProviderDescription = "Microsoft Azure Monitor";
        private const string ProviderProtocolVersion = "1.0";
        private const int ShutdownTimeoutInSeconds = 120;
        
        private static readonly string[] _completionTriggerCharacters = { ".", "-", ":", "\\", "[", "\"" };

        public delegate Task ShutdownCallback(object shutdownParams, RequestContext<object> shutdownRequestContext);

        public delegate Task InitializeCallback(InitializeRequest startupParams, RequestContext<InitializeResult> requestContext);
        
        private readonly List<ShutdownCallback> _shutdownCallbacks;
        
        private readonly List<InitializeCallback> _initializeCallbacks;

        private static readonly Lazy<ServiceHost> _instance = new Lazy<ServiceHost>(() => new ServiceHost());
        public static ServiceHost Instance => _instance.Value;
        
        public ServiceHost() : base(new StdioServerChannel())
        {
            // Initialize the shutdown activities
            _shutdownCallbacks = new List<ShutdownCallback>();
            _initializeCallbacks = new List<InitializeCallback>();
        }

        /// <summary>
        /// Provide initialization that must occur after the service host is started
        /// </summary>
        public void InitializeRequestHandlers()
        {
            // Register the requests that this service host will handle
            SetRequestHandler(InitializeRequest.Type, HandleInitializeRequest);
            SetRequestHandler(CapabilitiesRequest.Type, HandleCapabilitiesRequest);
            SetRequestHandler(ShutdownRequest.Type, HandleShutdownRequest);
            SetRequestHandler(VersionRequest.Type, HandleVersionRequest);
        }
        
        /// <summary>
        /// Adds a new callback to be called when the shutdown request is submitted
        /// </summary>
        /// <param name="callback">Callback to perform when a shutdown request is submitted</param>
        public void RegisterShutdownTask(ShutdownCallback callback)
        {
            _shutdownCallbacks.Add(callback);
        }

        /// <summary>
        /// Add a new method to be called when the initialize request is submitted
        /// </summary>
        /// <param name="callback">Callback to perform when an initialize request is submitted</param>
        public void RegisterInitializeTask(InitializeCallback callback)
        {
            _initializeCallbacks.Add(callback);
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
            var initializeTasks = _initializeCallbacks.Select(t => t(initializeParams, requestContext));
            await Task.WhenAll(initializeTasks);

            var result = new InitializeResult
            {
                Capabilities = new ServerCapabilities
                {
                    TextDocumentSync = TextDocumentSyncKind.Incremental,
                    DefinitionProvider = true,
                    ReferencesProvider = false,
                    DocumentFormattingProvider = true,
                    DocumentRangeFormattingProvider = true,
                    DocumentHighlightProvider = false,
                    HoverProvider = true,
                    CompletionProvider = new CompletionOptions
                    {
                        ResolveProvider = true,
                        TriggerCharacters = _completionTriggerCharacters
                    },
                    SignatureHelpProvider = new SignatureHelpOptions
                    {
                        TriggerCharacters = new[] {" ", ","}
                    }
                }
            };

            // Send back what this server can do
            await requestContext.SendResult(result);
        }
        
        /// <summary>
        /// Handles a request for the capabilities request
        /// </summary>
        private async Task HandleCapabilitiesRequest(CapabilitiesRequest initializeParams, RequestContext<CapabilitiesResult> requestContext)
        {
            var result = new CapabilitiesResult
            {
                Capabilities = new DmpServerCapabilities
                {
                    ProtocolVersion = ProviderProtocolVersion,
                    ProviderName = ProviderName,
                    ProviderDisplayName = ProviderDescription,
                    ConnectionProvider = ConnectionProviderOptionsHelper.BuildConnectionProviderOptions(),
                    Features = FeaturesMetadataProviderHelper.CreateFeatureMetadataProviders()
                }
            }; 
            
            await requestContext.SendResult(result);            
        }
        
        /// <summary>
        /// Handles the shutdown event for the Language Server
        /// </summary>
        private async Task HandleShutdownRequest(object shutdownParams, RequestContext<object> requestContext)
        {
            Logger.Write(TraceEventType.Information, "Service host is shutting down...");

            // Call all the shutdown methods provided by the service components
            Task[] shutdownTasks = _shutdownCallbacks.Select(t => t(shutdownParams, requestContext)).ToArray();
            TimeSpan shutdownTimeout = TimeSpan.FromSeconds(ShutdownTimeoutInSeconds);
            // shut down once all tasks are completed, or after the timeout expires, whichever comes first.
            await Task.WhenAny(Task.WhenAll(shutdownTasks), Task.Delay(shutdownTimeout)).ContinueWith(t => Environment.Exit(0));
        }
        
        /// <summary>
        /// Handles the version request. Sends back the server version as result.
        /// </summary>
        private static async Task HandleVersionRequest(object versionRequestParams, RequestContext<string> requestContext)
        {
            await requestContext.SendResult(Assembly.GetEntryAssembly()?.GetName().Version?.ToString());
        }
    }
}