//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private ConcurrentDictionary<string, ObjectExplorerSession> sessionMap;
        private readonly Lazy<Dictionary<string, HashSet<ChildFactory>>> applicableNodeChildFactories;
        private IMultiServiceProvider serviceProvider;

        /// <summary>
        /// Singleton constructor
        /// </summary>
        public ObjectExplorerService()
        {
            sessionMap = new ConcurrentDictionary<string, ObjectExplorerSession>();
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
        /// Returns the session ids
        /// </summary>
        internal IReadOnlyCollection<string> SessionIds
        {
            get
            {
                return new ReadOnlyCollection<string>(sessionMap.Keys.ToList());
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
            serviceHost.SetRequestHandler(RefreshRequest.Type, HandleRefreshRequest);
            serviceHost.SetRequestHandler(CloseSessionRequest.Type, HandleCloseSessionRequest);
        }

       
        
        internal async Task HandleCreateSessionRequest(ConnectionDetails connectionDetails, RequestContext<CreateSessionResponse> context)
        {
            Logger.Write(LogLevel.Verbose, "HandleCreateSessionRequest");

            Func<Task<CreateSessionResponse>> doCreateSession = async () =>
            {
                Validate.IsNotNull(nameof(connectionDetails), connectionDetails);
                Validate.IsNotNull(nameof(context), context);
                return await Task.Factory.StartNew(() =>
                {
                    string uri = GenerateUri(connectionDetails);

                    return new CreateSessionResponse { SessionId = uri };
                });
            };

            CreateSessionResponse response = await HandleRequestAsync(doCreateSession, context, "HandleCreateSessionRequest");
            if (response != null)
            {
                RunCreateSessionTask(connectionDetails, response.SessionId);
            }

        }

        internal async Task HandleExpandRequest(ExpandParams expandParams, RequestContext<bool> context)
        {
            Logger.Write(LogLevel.Verbose, "HandleExpandRequest");

            Func<Task<bool>> expandNode = async () =>
            {
                Validate.IsNotNull(nameof(expandParams), expandParams);
                Validate.IsNotNull(nameof(context), context);

                string uri = expandParams.SessionId;
                ObjectExplorerSession session = null;
                NodeInfo[] nodes = null;
                ExpandResponse response;
                if (!sessionMap.TryGetValue(uri, out session))
                {
                    Logger.Write(LogLevel.Verbose, $"Cannot expand object explorer node. Couldn't find session for uri. {uri} ");
                    await serviceHost.SendEvent(ExpandCompleteNotification.Type, new ExpandResponse
                    {
                        SessionId = expandParams.SessionId,
                        NodePath = expandParams.NodePath,
                        ErrorMessage = $"Couldn't find session for session: {uri}"
                    });
                    return false;
                }
                else
                {
                    RunExpandTask(session, expandParams);
                    return true;
                }
            };
            await HandleRequestAsync(expandNode, context, "HandleExpandRequest");
        }

        internal async Task HandleRefreshRequest(RefreshParams refreshParams, RequestContext<bool> context)
        {
            Logger.Write(LogLevel.Verbose, "HandleRefreshRequest");
            Validate.IsNotNull(nameof(refreshParams), refreshParams);
            Validate.IsNotNull(nameof(context), context);

            string uri = refreshParams.SessionId;
            ObjectExplorerSession session = null;
            ExpandResponse response;
            if (!sessionMap.TryGetValue(uri, out session))
            {
                Logger.Write(LogLevel.Verbose, $"Cannot expand object explorer node. Couldn't find session for uri. {uri} ");
                await serviceHost.SendEvent(ExpandCompleteNotification.Type, new ExpandResponse
                {
                    SessionId = refreshParams.SessionId,
                    NodePath = refreshParams.NodePath,
                    ErrorMessage = $"Couldn't find session for session: {uri}"
                });
            }
            else
            {
                RunExpandTask(session, refreshParams, true);
            }
            await context.SendResult(true);
        }

        internal async Task HandleCloseSessionRequest(CloseSessionParams closeSessionParams, RequestContext<CloseSessionResponse> context)
        {

            Logger.Write(LogLevel.Verbose, "HandleCloseSessionRequest");
            Func<Task<CloseSessionResponse>> closeSession = () =>
            {
                Validate.IsNotNull(nameof(closeSessionParams), closeSessionParams);
                Validate.IsNotNull(nameof(context), context);
                return Task.Factory.StartNew(() =>
                {
                    string uri = closeSessionParams.SessionId;
                    ObjectExplorerSession session = null;
                    bool success = false;
                    if (!sessionMap.TryGetValue(uri, out session))
                    {
                        Logger.Write(LogLevel.Verbose, $"Cannot close object explorer session. Couldn't find session for uri. {uri} ");
                    }

                    if (session != null)
                    {
                        // refresh the nodes for given node path
                        CloseSession(uri);
                        success = true;
                    }

                    var response = new CloseSessionResponse() { Success = success, SessionId = uri };
                    return response;
                });
            };

            await HandleRequestAsync(closeSession, context, "HandleCloseSessionRequest");
        }

        internal void CloseSession(string uri)
        {
            ObjectExplorerSession session;
            if (sessionMap.TryGetValue(uri, out session))
            {
                // Remove the session from active sessions and disconnect
                sessionMap.TryRemove(session.Uri, out session);
                connectionService.Disconnect(new DisconnectParams()
                {
                    OwnerUri = uri
                });
            }
        }

        private void  RunCreateSessionTask(ConnectionDetails connectionDetails, string uri)
        {
            if (connectionDetails != null && !string.IsNullOrEmpty(uri))
            {
                Task task = CreateSessionAsync(connectionDetails, uri);
                CreateSessionTask = task;
                Task.Run(async () => await task);
            }
        }

        /// <summary>
        /// For tests only
        /// </summary>
        internal Task CreateSessionTask
        {
            get;
            private set;
        }

        private async Task<SessionCreatedParameters> CreateSessionAsync(ConnectionDetails connectionDetails, string uri)
        {
            ObjectExplorerSession session;
            if (!sessionMap.TryGetValue(uri, out session))
            {
                // Establish a connection to the specified server/database
                session = await DoCreateSession(connectionDetails, uri);
            }

            SessionCreatedParameters response;
            if (session != null)
            {
                // Else we have a session available, response with existing session information
                response = new SessionCreatedParameters
                {
                    Success = true,
                    RootNode = session.Root.ToNodeInfo(),
                    SessionId = uri,
                    ErrorMessage = session.ErrorMessage
                    
                };
                 await serviceHost.SendEvent(CreateSessionCompleteNotification.Type, response);
                return response;
            }
            return null;

        }

        internal async Task<NodeInfo[]> ExpandNode(ObjectExplorerSession session, string nodePath, bool forceRefresh = false)
        {
            return await Task.Factory.StartNew(() =>
            {
                NodeInfo[] nodes = null;
                TreeNode node = session.Root.FindNodeByPath(nodePath);
                if(node != null)
                {
                    if (forceRefresh)
                    {
                        nodes = node.Refresh().Select(x => x.ToNodeInfo()).ToArray();
                    }
                    else
                    {
                        nodes = node.Expand().Select(x => x.ToNodeInfo()).ToArray();
                    }
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
            connectionDetails.PersistSecurityInfo = true;
            ConnectParams connectParams = new ConnectParams() { OwnerUri = uri, Connection = connectionDetails };

            ConnectionCompleteParams connectionResult = await Connect(connectParams, uri);
            if (connectionResult == null)
            {
                // Connection failed and notification is already sent
                return null;
            }

            session = ObjectExplorerSession.CreateSession(connectionResult, serviceProvider);
            sessionMap.AddOrUpdate(uri, session, (key, oldSession) => session);
            return session;
        }
        

        private async Task<ConnectionCompleteParams> Connect(ConnectParams connectParams, string uri)
        {
            string connectionErrorMessage = string.Empty;
            try
            {
                // open connection based on request details
                ConnectionCompleteParams result = await connectionService.Connect(connectParams);
                connectionErrorMessage = result != null ? result.Messages : string.Empty;
                if (result != null && !string.IsNullOrEmpty(result.ConnectionId))
                {
                    return result;
                }
                else
                {
                    Logger.Write(LogLevel.Warning, $"Connection Failed for OE. connection error: {connectionErrorMessage}");
                    await serviceHost.SendEvent(CreateSessionCompleteNotification.Type, new SessionCreatedParameters
                    {
                        ErrorMessage = result.ErrorMessage,
                        SessionId = uri
                    });
                    return null;
                }
               
            }
            catch (Exception ex)
            {
                Logger.Write(LogLevel.Warning, $"Connection Failed for OE. connection error:{connectionErrorMessage} error: {ex.Message}");
                // Send a connection failed error message in this case.
                SessionCreatedParameters result = new SessionCreatedParameters()
                {
                    ErrorMessage = ex.ToString(),
                    SessionId = uri
                };
                await serviceHost.SendEvent(CreateSessionCompleteNotification.Type, result);
                return null;
            }
        }

        private void RunExpandTask(ObjectExplorerSession session, ExpandParams expandParams, bool forceRefresh = false)
        {
            Task task = ExpandNodeAsync(session, expandParams, forceRefresh);
            ExpandTask = task;
            Task.Run(async () =>
            {
                await task;
            });
        }

        /// <summary>
        /// For tests only
        /// </summary>
        internal Task ExpandTask
        {
            get;
            set;
        }

        private async Task ExpandNodeAsync(ObjectExplorerSession session, ExpandParams expandParams, bool forceRefresh = false)
        {
            NodeInfo[] nodes = null;
            nodes = await ExpandNode(session, expandParams.NodePath, forceRefresh);
            ExpandResponse response = new ExpandResponse() { Nodes = nodes, SessionId = session.Uri, NodePath = expandParams.NodePath };
            await serviceHost.SendEvent(ExpandCompleteNotification.Type, response);
        }

       

        private async Task<T> HandleRequestAsync<T>(Func<Task<T>> handler, RequestContext<T> requestContext, string requestType)
        {
            Logger.Write(LogLevel.Verbose, requestType);

            try
            {
                T result = await handler();
                await requestContext.SendResult(result);
                return result;
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
            return default(T);
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
                throw new InvalidOperationException(SqlTools.Hosting.SR.ServiceProviderNotSet);
            }
            if (connectionService == null)
            {
                throw new InvalidOperationException(SqlTools.Hosting.SR.ServiceProviderNotSet);
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

            public string ErrorMessage { get; set; }

            public static ObjectExplorerSession CreateSession(ConnectionCompleteParams response, IMultiServiceProvider serviceProvider)
            {
                ServerNode rootNode = new ServerNode(response, serviceProvider);
                var session = new ObjectExplorerSession(response.OwnerUri, rootNode, serviceProvider, serviceProvider.GetService<ConnectionService>());
                if (!ObjectExplorerUtils.IsSystemDatabaseConnection(response.ConnectionSummary.DatabaseName))
                {
                    // Assuming the databases are in a folder under server node
                    DatabaseTreeNode databaseNode = new DatabaseTreeNode(rootNode, response.ConnectionSummary.DatabaseName);
                    session.Root = databaseNode;
                }
               
                return session;
            }
            
        }
    }

}
