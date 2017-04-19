//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer
{
    /// <summary>
    /// A Service to support querying server and database information as an Object Explorer tree.
    /// The APIs used for this are modeled closely on the VSCode TreeExplorerNodeProvider API.
    /// </summary>
    [Export(typeof(IHostedService))]
    public class ObjectExplorerService : HostedService<ObjectExplorerService>, IComposableService
    {
        internal const string uriPrefix = "objectexplorer://";
        
        // Instance of the connection service, used to get the connection info for a given owner URI
        private ConnectionService connectionService;
        private IProtocolEndpoint serviceHost;
        private Dictionary<string, ObjectExplorerSession> sessionMap;
        private readonly Lazy<Dictionary<string, HashSet<ChildFactory>>> applicableNodeChildFactories;
        private IMultiServiceProvider serviceProvider;

        /// <summary>
        /// Singleton constructor
        /// </summary>
        public ObjectExplorerService()
        {
            sessionMap = new Dictionary<string, ObjectExplorerSession>();
            applicableNodeChildFactories = new Lazy<Dictionary<string, HashSet<ChildFactory>>>(() => PopulateFactories());
        }

        /// <summary>
        /// Internal for testing only
        /// </summary>
        internal ObjectExplorerService(ExtensionServiceProvider serviceProvider)
            : this()
        {
            SetServiceProvider(serviceProvider);
        }

        private Dictionary<string, HashSet<ChildFactory>> ApplicableNodeChildFactories
        {
            get
            {
                return applicableNodeChildFactories.Value;
            }
        }
    
        /// <summary>
        /// As an <see cref="IComposableService"/>, this will be set whenever the service is initialized
        /// via an <see cref="IMultiServiceProvider"/>
        /// </summary>
        /// <param name="provider"></param>
        public override void SetServiceProvider(IMultiServiceProvider provider)
        {
            Validate.IsNotNull(nameof(provider), provider);
            serviceProvider = provider;
            connectionService = provider.GetService<ConnectionService>();
        }

        /// <summary>
        /// Initializes the service with the service host and registers request handlers.
        /// </summary>
        /// <param name="serviceHost">The service host instance to register with</param>
        public override void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Write(LogLevel.Verbose, "ObjectExplorer service initialized");
            this.serviceHost = serviceHost;
            // Register handlers for requests
            serviceHost.SetRequestHandler(CreateSessionRequest.Type, HandleCreateSessionRequest);
            serviceHost.SetRequestHandler(ExpandRequest.Type, HandleExpandRequest);
        }
        
        internal async Task HandleCreateSessionRequest(ConnectionDetails connectionDetails, RequestContext<CreateSessionResponse> context)
        {
            Logger.Write(LogLevel.Verbose, "HandleCreateSessionRequest");
            Func<Task<CreateSessionResponse>> doCreateSession = async () =>
            {
                Validate.IsNotNull(nameof(connectionDetails), connectionDetails);
                Validate.IsNotNull(nameof(context), context);

                string uri = GenerateUri(connectionDetails);

                ObjectExplorerSession session;
                if (!sessionMap.TryGetValue(uri, out session))
                {
                    // Establish a connection to the specified server/database
                    session = await DoCreateSession(connectionDetails, uri);
                }

                CreateSessionResponse response;
                if (session == null)
                {
                    response = new CreateSessionResponse() { Success = false };
                }
                else
                {
                    // Else we have a session available, response with existing session information
                    response = new CreateSessionResponse()
                    {
                        Success = true,
                        RootNode = session.Root.ToNodeInfo(),
                        SessionId = session.Uri
                    };
                }
                return response;
            };

            await HandleRequestAsync(doCreateSession, context, "HandleCreateSessionRequest");
        }

        internal async Task<NodeInfo[]> ExpandNode(ObjectExplorerSession session, string nodePath)
        {
            return await Task.Factory.StartNew(() =>
            {
                NodeInfo[] nodes = null;
                TreeNode node = session.Root.FindNodeByPath(nodePath);
                if(node != null)
                {
                    nodes = node.Expand().Select(x => x.ToNodeInfo()).ToArray();
                }
                return nodes; 
            });
        }

        /// <summary>
        /// Establishes a new session and stores its information
        /// </summary>
        /// <returns><see cref="ObjectExplorerSession"/> object if successful, null if unsuccessful</returns>
        internal async Task<ObjectExplorerSession> DoCreateSession(ConnectionDetails connectionDetails, string uri)
        {
            ObjectExplorerSession session;

            ConnectParams connectParams = new ConnectParams() { OwnerUri = uri, Connection = connectionDetails };

            ConnectionCompleteParams connectionResult = await Connect(connectParams);
            if (connectionResult == null)
            {
                return null;
            }

            session = ObjectExplorerSession.CreateSession(connectionResult, serviceProvider);
            sessionMap[uri] = session;
            return session;
        }
        

        private async Task<ConnectionCompleteParams> Connect(ConnectParams connectParams)
        {
            try
            {
                // open connection based on request details
                ConnectionCompleteParams result = await connectionService.Connect(connectParams);
                if(result != null && !string.IsNullOrEmpty(result.ConnectionId))
                {
                    return result;
                }
                else
                {
                    await serviceHost.SendEvent(ConnectionCompleteNotification.Type, result);
                    return null;
                }
               
            }
            catch (Exception ex)
            {
                // Send a connection failed error message in this case.
                ConnectionCompleteParams result = new ConnectionCompleteParams()
                {
                    Messages = ex.ToString()
                };
                await serviceHost.SendEvent(ConnectionCompleteNotification.Type, result);
                return null;
            }
        }

        internal async Task HandleExpandRequest(ExpandParams expandParams, RequestContext<ExpandResponse> context)
        {
            Logger.Write(LogLevel.Verbose, "HandleExpandRequest");
            Func<Task<ExpandResponse>> expandNode = async () =>
            {
                Validate.IsNotNull(nameof(expandParams), expandParams);
                Validate.IsNotNull(nameof(context), context);

                string uri = expandParams.SessionId;
                ObjectExplorerSession session = null;
                NodeInfo[] nodes = null;
                if (sessionMap.ContainsKey(uri))
                {
                    session = sessionMap[uri];
                }
                else
                {
                    //TODO: error
                }

             
                if (session != null)
                {
                    // Establish a connection to the specified server/database
                    nodes = await ExpandNode(session, expandParams.NodePath);
                }

                ExpandResponse response;
                response = new ExpandResponse() { Nodes = nodes, SessionId = uri };
                return response;
            };

            await HandleRequestAsync(expandNode, context, "HandleExpandRequest");
        }
        
        private async Task HandleRequestAsync<T>(Func<Task<T>> handler, RequestContext<T> requestContext, string requestType)
        {
            Logger.Write(LogLevel.Verbose, requestType);

            try
            {
                T result = await handler();
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        /// <summary>
        /// Generates a URI for object explorer using a similar pattern to Mongo DB (which has URI-based database definition)
        /// as this should ensure uniqueness
        /// </summary>
        /// <param name="details"></param>
        /// <returns>string representing a URI</returns>
        /// <remarks>Internal for testing purposes only</remarks>
        internal static string GenerateUri(ConnectionDetails details)
        {
            Validate.IsNotNull("details", details);
            string uri = string.Format(CultureInfo.InvariantCulture, "{0}{1}", uriPrefix, Uri.EscapeUriString(details.ServerName));
            uri = AppendIfExists(uri, "databaseName", details.DatabaseName);
            uri = AppendIfExists(uri, "user", details.UserName);
            return uri;
        }

        private static string AppendIfExists(string uri, string propertyName, string propertyValue)
        {
            if (!string.IsNullOrEmpty(propertyValue))
            {
                uri += string.Format(CultureInfo.InvariantCulture, ";{0}={1}", propertyName, Uri.EscapeUriString(propertyValue));
            }
            return uri;
        }
        
        public IEnumerable<ChildFactory> GetApplicableChildFactories(TreeNode item)
        {
            if (ApplicableNodeChildFactories != null)
            {
                HashSet<ChildFactory> applicableFactories;
                if (ApplicableNodeChildFactories.TryGetValue(item.NodeTypeId.ToString(), out applicableFactories))
                {
                    return applicableFactories;
                }
            }
            return null;
        }

        internal Dictionary<string, HashSet<ChildFactory>> PopulateFactories()
        {
            VerifyServicesInitialized();

            var childFactories = new Dictionary<string, HashSet<ChildFactory>>();
            // Create our list of all NodeType to ChildFactory objects so we can expand appropriately
            foreach (var factory in serviceProvider.GetServices<ChildFactory>())
            {
                var parents = factory.ApplicableParents();
                if (parents != null)
                {
                    foreach (var parent in parents)
                    {
                        AddToApplicableChildFactories(childFactories, factory, parent);
                    }
                }
            }
            return childFactories;
        }

        private void VerifyServicesInitialized()
        {
            if (serviceProvider == null)
            {
                throw new InvalidOperationException(SqlTools.Hosting.Localization.sr.ServiceProviderNotSet);
            }
            if (connectionService == null)
            {
                throw new InvalidOperationException(SqlTools.Hosting.Localization.sr.ServiceProviderNotSet);
            }
        }

        private static void AddToApplicableChildFactories(Dictionary<string, HashSet<ChildFactory>> childFactories, ChildFactory factory, string parent)
        {
            HashSet<ChildFactory> applicableFactories;
            if (!childFactories.TryGetValue(parent, out applicableFactories))
            {
                applicableFactories = new HashSet<ChildFactory>();
                childFactories[parent] = applicableFactories;
            }
            applicableFactories.Add(factory);
        }

        internal class ObjectExplorerSession
        {
            private ConnectionService connectionService;
            private IMultiServiceProvider serviceProvider;

            // TODO decide whether a cache is needed to handle lookups in elements with a large # children
            //private const int Cachesize = 10000;
            //private Cache<string, NodeMapping> cache;

            public ObjectExplorerSession(string uri, TreeNode root, IMultiServiceProvider serviceProvider, ConnectionService connectionService)
            {
                Validate.IsNotNullOrEmptyString("uri", uri);
                Validate.IsNotNull("root", root);
                Uri = uri;
                Root = root;
                this.serviceProvider = serviceProvider;
                this.connectionService = connectionService;
            }
            
            public string Uri { get; private set; }
            public TreeNode Root { get; private set; }

            public static ObjectExplorerSession CreateSession(ConnectionCompleteParams response, IMultiServiceProvider serviceProvider)
            {
                TreeNode rootNode = new ServerNode(response, serviceProvider);
                var session = new ObjectExplorerSession(response.OwnerUri, rootNode, serviceProvider, serviceProvider.GetService<ConnectionService>());
                if (!ObjectExplorerUtils.IsSystemDatabaseConnection(response.ConnectionSummary.DatabaseName))
                {
                    var children = rootNode.Expand();
                    var databasesRoot = children.FirstOrDefault(x => x.NodeTypeId == NodeTypes.Databases);
                    var databasesChildren = databasesRoot.Expand();
                    var databases = databasesChildren.Where(x => x.NodeType == NodeTypes.Database.ToString());
                    var databaseNode = databases.FirstOrDefault(d => d.Label == response.ConnectionSummary.DatabaseName);
                    databaseNode.Label = rootNode.Label;
                    session.Root = databaseNode;
                }
                return session;
            }
            
        }
    }

}
