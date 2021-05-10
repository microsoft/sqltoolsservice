//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel;
using Microsoft.Kusto.ServiceLayer.SqlContext;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.Workspace;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer
{
    /// <summary>
    /// A Service to support querying server and database information as an Object Explorer tree.
    /// The APIs used for this are modeled closely on the VSCode TreeExplorerNodeProvider API.
    /// </summary>
    public partial class ObjectExplorerService : IDisposable
    {
        private IConnectedBindingQueue _connectedBindingQueue;
        private static readonly Lazy<ObjectExplorerService> _instance = new Lazy<ObjectExplorerService>(() => new ObjectExplorerService());

        // Instance of the connection service, used to get the connection info for a given owner URI
        private ConnectionService _connectionService;
        private IProtocolEndpoint _serviceHost;
        private readonly ConcurrentDictionary<string, ObjectExplorerSession> _sessionMap;
        private IMultiServiceProvider _serviceProvider;
        private string connectionName = "ObjectExplorer";

        /// <summary>
        /// This timeout limits the amount of time that object explorer tasks can take to complete
        /// </summary>
        private ObjectExplorerSettings _settings;
        
        public static ObjectExplorerService Instance => _instance.Value;

        /// <summary>
        /// Singleton constructor
        /// </summary>
        public ObjectExplorerService()
        {
            _sessionMap = new ConcurrentDictionary<string, ObjectExplorerSession>();
            NodePathGenerator.Initialize();
        }

        /// <summary>
        /// Returns the session ids
        /// </summary>
        internal IReadOnlyCollection<string> SessionIds
        {
            get
            {
                return new ReadOnlyCollection<string>(_sessionMap.Keys.ToList());
            }
        }

        /// <summary>
        /// Initializes the service with the service host and registers request handlers.
        /// </summary>
        /// <param name="serviceHost">The service host instance to register with</param>
        /// <param name="connectedBindingQueue"></param>
        /// <param name="workspaceService"></param>
        /// <param name="connectionService"></param>
        public void InitializeService(IProtocolEndpoint serviceHost, IConnectedBindingQueue connectedBindingQueue, WorkspaceService<SqlToolsSettings> workspaceService, ConnectionService connectionService, IMultiServiceProvider serviceProvider)
        {
            Logger.Write(TraceEventType.Verbose, "ObjectExplorer service initialized");
            _serviceHost = serviceHost;
            _connectedBindingQueue = connectedBindingQueue;
            _connectionService = connectionService;
            _serviceProvider = serviceProvider;
            
            _connectedBindingQueue.OnUnhandledException += OnUnhandledException;
            _connectionService.RegisterConnectedQueue(connectionName, _connectedBindingQueue);

            // Register handlers for requests
            serviceHost.SetRequestHandler(CreateSessionRequest.Type, HandleCreateSessionRequest);
            serviceHost.SetRequestHandler(ExpandRequest.Type, HandleExpandRequest);
            serviceHost.SetRequestHandler(RefreshRequest.Type, HandleRefreshRequest);
            serviceHost.SetRequestHandler(CloseSessionRequest.Type, HandleCloseSessionRequest);
            serviceHost.SetRequestHandler(FindNodesRequest.Type, HandleFindNodesRequest);
            
            if (workspaceService != null)
            {
                workspaceService.RegisterConfigChangeCallback(HandleDidChangeConfigurationNotification);
            }
        }


        /// <summary>
        /// Ensure formatter settings are always up to date
        /// </summary>
        public Task HandleDidChangeConfigurationNotification(
            SqlToolsSettings newSettings,
            SqlToolsSettings oldSettings,
            EventContext eventContext)
        {
            // update the current settings to reflect any changes (assuming formatter settings exist)
            _settings = newSettings?.SqlTools?.ObjectExplorer ?? _settings;
            return Task.FromResult(true);
        }


        private async Task HandleCreateSessionRequest(ConnectionDetails connectionDetails, RequestContext<CreateSessionResponse> context)
        {
            Logger.Write(TraceEventType.Verbose, "HandleCreateSessionRequest");
            
            try
            {
                Validate.IsNotNull(nameof(connectionDetails), connectionDetails);
                Validate.IsNotNull(nameof(context), context);
                
                string uri = GenerateUri(connectionDetails);

                var sessionResponse = new CreateSessionResponse
                {
                    SessionId = uri
                };
                
                await context.SendResult(sessionResponse);
                
                Parallel.Invoke(async () => await CreateSessionAsync(connectionDetails, uri));
            }
            catch (Exception ex)
            {
                await context.SendError(ex.ToString());
            }

        }

        private async Task HandleExpandRequest(ExpandParams expandParams, RequestContext<bool> context)
        {
            Logger.Write(TraceEventType.Verbose, "HandleExpandRequest");

            try
            {
                Validate.IsNotNull(nameof(expandParams), expandParams);
                Validate.IsNotNull(nameof(context), context);
                
                Parallel.Invoke(async () => await Expand(expandParams));
                await context.SendResult(true);
            }
            catch (Exception ex)
            {
                await context.SendError(ex.ToString());
            }
            
        }

        private async Task Expand(ExpandParams expandParams)
        {
            if (!_sessionMap.TryGetValue(expandParams.SessionId, out ObjectExplorerSession session))
            {
                Logger.Write(TraceEventType.Verbose, $"Cannot expand object explorer node. Couldn't find session for uri. {expandParams.SessionId} ");
                var errorResponse = new ExpandResponse
                {
                    SessionId = expandParams.SessionId,
                    NodePath = expandParams.NodePath,
                    ErrorMessage = $"Couldn't find session for session: {expandParams.SessionId}"
                };

                await _serviceHost.SendEvent(ExpandCompleteNotification.Type, errorResponse);
            }

            await RunExpandTask(session, expandParams);
        }

        private async Task HandleRefreshRequest(RefreshParams refreshParams, RequestContext<bool> context)
        {
            Logger.Write(TraceEventType.Verbose, "HandleRefreshRequest");
            
            try
            {
                Validate.IsNotNull(nameof(refreshParams), refreshParams);
                Validate.IsNotNull(nameof(context), context);
                
                Parallel.Invoke(async () => await Refresh(refreshParams));
                await context.SendResult(true);
            }
            catch (Exception ex)
            {
                await context.SendError(ex.ToString());
            }
        }

        private async Task Refresh(RefreshParams refreshParams)
        {
            string uri = refreshParams.SessionId;
            if (!_sessionMap.TryGetValue(uri, out ObjectExplorerSession session))
            {
                Logger.Write(TraceEventType.Verbose, $"Cannot expand object explorer node. Couldn't find session for uri. {uri} ");
                var errorResponse = new ExpandResponse
                {
                    SessionId = refreshParams.SessionId,
                    NodePath = refreshParams.NodePath,
                    ErrorMessage = $"Couldn't find session for session: {uri}"
                };
                
                await _serviceHost.SendEvent(ExpandCompleteNotification.Type, errorResponse);
            }
            else
            {
                await RunExpandTask(session, refreshParams, true);
            }
        }

        private async Task HandleCloseSessionRequest(CloseSessionParams closeSessionParams, RequestContext<CloseSessionResponse> context)
        {
            Logger.Write(TraceEventType.Verbose, "HandleCloseSessionRequest");
            try
            {
                Validate.IsNotNull(nameof(closeSessionParams), closeSessionParams);
                Validate.IsNotNull(nameof(context), context);

                bool success = false;
                
                Parallel.Invoke(() => success = CloseSession(closeSessionParams.SessionId));
                
                var closeSessionResponse = new CloseSessionResponse()
                {
                    Success = success, 
                    SessionId = closeSessionParams.SessionId
                };

                await context.SendResult(closeSessionResponse);
            }
            catch (Exception ex)
            {
                await context.SendError(ex.ToString());
            }
        }

        private async Task HandleFindNodesRequest(FindNodesParams findNodesParams, RequestContext<FindNodesResponse> context)
        {
            var foundNodes = FindNodes(findNodesParams.SessionId, findNodesParams.Type, findNodesParams.Schema, findNodesParams.Name, findNodesParams.Database, findNodesParams.ParentObjectNames);
            if (foundNodes == null)
            {
                foundNodes = new List<TreeNode>();
            }
            await context.SendResult(new FindNodesResponse { Nodes = foundNodes.Select(node => node.ToNodeInfo()).ToList() });
        }

        private bool CloseSession(string uri)
        {
            if (_sessionMap.TryGetValue(uri, out ObjectExplorerSession session))
            {
                // Remove the session from active sessions and disconnect
                if(_sessionMap.TryRemove(session.Uri, out session))
                {
                    if (session != null && session.ConnectionInfo != null)
                    {
                        _connectedBindingQueue.RemoveBindingContext(session.ConnectionInfo);
                    }
                }
                _connectionService.Disconnect(new DisconnectParams()
                {
                    OwnerUri = uri
                });
                
                return true;
            }

            Logger.Write(TraceEventType.Verbose, $"Cannot close object explorer session. Couldn't find session for uri. {uri} ");
            return false;
        }

        private async Task CreateSessionAsync(ConnectionDetails connectionDetails, string uri)
        {
            Logger.Write(TraceEventType.Information, "Creating OE session");
            var cancellationToken = new CancellationTokenSource();
            
            if (!_sessionMap.TryGetValue(uri, out ObjectExplorerSession session))
            {
                // Establish a connection to the specified server/database
                session = await DoCreateSessionAsync(connectionDetails, uri);
            }

            if (session != null && !cancellationToken.IsCancellationRequested)
            {
                // Else we have a session available, response with existing session information
                var response = new SessionCreatedParameters
                {
                    Success = true,
                    RootNode = session.Root.ToNodeInfo(),
                    SessionId = uri,
                    ErrorMessage = session.ErrorMessage
                };
                await _serviceHost.SendEvent(CreateSessionCompleteNotification.Type, response);
            }
        }

        private ExpandResponse ExpandNode(ObjectExplorerSession session, string nodePath, CancellationToken cancellationToken,
            bool forceRefresh = false)
        {
            TreeNode node = session.Root.FindNodeByPath(nodePath);
            var response = new ExpandResponse
            {
                Nodes = Enumerable.Empty<NodeInfo>() as NodeInfo[],
                SessionId = session.Uri,
                NodePath = nodePath
            };

            // This node was likely returned from a different node provider. Ignore expansion and return an empty array
            // since we don't need to add any nodes under this section of the tree.
            if (node == null)
            {
                response.ErrorMessage = string.Empty;
                return response;
            }

            response.ErrorMessage = node.ErrorMessage;

            IList<TreeNode> nodes = forceRefresh 
                ? node.Refresh(cancellationToken) 
                : node.Expand(cancellationToken);

            response.Nodes = nodes.Select(x => x.ToNodeInfo()).ToArray();
            return response;
        }

        /// <summary>
        /// Establishes a new session and stores its information
        /// </summary>
        /// <returns><see cref="ObjectExplorerSession"/> object if successful, null if unsuccessful</returns>
        private async Task<ObjectExplorerSession> DoCreateSessionAsync(ConnectionDetails connectionDetails, string uri)
        {
            try
            {
                connectionDetails.PersistSecurityInfo = true;
                var connectParams = new ConnectParams
                {
                    OwnerUri = uri,
                    Connection = connectionDetails,
                    Type = ConnectionType.ObjectExplorer
                };
                
                ConnectionCompleteParams connectionResult = await Connect(connectParams, uri);
                if (!_connectionService.TryFindConnection(uri, out ConnectionInfo connectionInfo))
                {
                    return null;
                }

                if (connectionResult == null)
                {
                    // Connection failed and notification is already sent
                    return null;
                }

                bool isDefaultOrSystemDatabase = DatabaseUtils.IsSystemDatabaseConnection(connectionDetails.DatabaseName) || string.IsNullOrWhiteSpace(connectionDetails.DatabaseDisplayName);
                connectionInfo.TryGetConnection(ConnectionType.ObjectExplorer, out ReliableDataSourceConnection dataSourceConnection);
                var session = ObjectExplorerSession.CreateSession(connectionResult, _serviceProvider, dataSourceConnection.GetUnderlyingConnection(), isDefaultOrSystemDatabase);
                session.ConnectionInfo = connectionInfo;

                _sessionMap.AddOrUpdate(uri, session, (key, oldSession) => session);
                return session;
            }
            catch(Exception ex)
            {
                await SendSessionFailedNotification(uri, ex.Message);
                return null;
            }
        }        

        private async Task<ConnectionCompleteParams> Connect(ConnectParams connectParams, string uri)
        {
            try
            {
                // open connection based on request details
                ConnectionCompleteParams result = await _connectionService.Connect(connectParams);
                string connectionErrorMessage = result != null ? $"{result.Messages} error code:{result.ErrorNumber}" : string.Empty;
                if (result != null && !string.IsNullOrEmpty(result.ConnectionId))
                {
                    return result;
                }

                await SendSessionFailedNotification(uri, result.ErrorMessage);
                return null;

            }
            catch (Exception ex)
            {
                await SendSessionFailedNotification(uri, ex.ToString());
                return null;
            }
        }

        private async Task SendSessionFailedNotification(string uri, string errorMessage)
        {
            Logger.Write(TraceEventType.Warning, $"Failed To create OE session: {errorMessage}");
            SessionCreatedParameters result = new SessionCreatedParameters()
            {
                Success = false,
                ErrorMessage = errorMessage,
                SessionId = uri
            };
            await _serviceHost.SendEvent(CreateSessionCompleteNotification.Type, result);
        }

        internal async Task SendSessionDisconnectedNotification(string uri, bool success, string errorMessage)
        {
            Logger.Write(TraceEventType.Information, $"OE session disconnected: {errorMessage}");
            SessionDisconnectedParameters result = new SessionDisconnectedParameters()
            {
                Success = success,
                ErrorMessage = errorMessage,
                SessionId = uri
            };
            await _serviceHost.SendEvent(SessionDisconnectedNotification.Type, result);
        }

        private async Task RunExpandTask(ObjectExplorerSession session, ExpandParams expandParams, bool forceRefresh = false)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            await ExpandNodeAsync(session, expandParams,  cancellationTokenSource.Token, forceRefresh);
        }

        private async Task<ObjectExplorerTaskResult> RunTaskWithTimeout(Task task, int timeoutInSec)
        {
            ObjectExplorerTaskResult result = new ObjectExplorerTaskResult();
            TimeSpan timeout = TimeSpan.FromSeconds(timeoutInSec);
            await Task.WhenAny(task, Task.Delay(timeout));
            result.IsCompleted = task.IsCompleted;
            if(task.Exception != null)
            {
                result.Exception = task.Exception;
            }
            else if (!task.IsCompleted)
            {
                result.Exception = new TimeoutException($"Object Explorer task didn't complete within {timeoutInSec} seconds.");
            }
            return result;
        }

        private async Task ExpandNodeAsync(ObjectExplorerSession session, ExpandParams expandParams, CancellationToken cancellationToken, bool forceRefresh = false)
        {
            ExpandResponse response = ExpandNode(session, expandParams.NodePath, cancellationToken, forceRefresh);
            
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.Write(TraceEventType.Verbose, "OE expand canceled");
            }
            else
            {
                await _serviceHost.SendEvent(ExpandCompleteNotification.Type, response);
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
            return ConnectedBindingQueue.GetConnectionContextKey(details);
        }

        /// <summary>
        /// Find all tree nodes matching the given node information
        /// </summary>
        /// <param name="sessionId">The ID of the object explorer session to find nodes for</param>
        /// <param name="typeName">The requested node type</param>
        /// <param name="schema">The schema for the requested object, or null if not applicable</param>
        /// <param name="name">The name of the requested object</param>
        /// <param name="databaseName">The name of the database containing the requested object, or null if not applicable</param>
        /// <param name="parentNames">The name of any other parent objects in the object explorer tree, from highest in the tree to lowest</param>
        /// <returns>A list of nodes matching the given information, or an empty list if no nodes match</returns>
        public List<TreeNode> FindNodes(string sessionId, string typeName, string schema, string name, string databaseName, List<string> parentNames = null)
        {
            var nodes = new List<TreeNode>();
            var oeSession = _sessionMap.GetValueOrDefault(sessionId);
            if (oeSession == null)
            {
                return nodes;
            }

            var outputPaths = NodePathGenerator.FindNodePaths(oeSession, typeName, schema, name, databaseName, parentNames);
            foreach (var outputPath in outputPaths)
            {
                var treeNode = oeSession.Root.FindNodeByPath(outputPath, true);
                if (treeNode != null)
                {
                    nodes.Add(treeNode);
                }
            }
            return nodes;
        }

        internal class ObjectExplorerTaskResult
        {
            public bool IsCompleted { get; set; }
            public Exception Exception { get; set; }
        }

        public void Dispose()
        {
            if (_connectedBindingQueue != null)
            {
                _connectedBindingQueue.OnUnhandledException -= OnUnhandledException;
                _connectedBindingQueue.Dispose();
            }            
        }

        private async void OnUnhandledException(string queueKey, Exception ex)
        {
            string sessionUri = LookupUriFromQueueKey(queueKey);
            if (!string.IsNullOrWhiteSpace(sessionUri))
            {
                await SendSessionDisconnectedNotification(uri: sessionUri, success: false, errorMessage: ex.ToString());
            }
        }

        private string LookupUriFromQueueKey(string queueKey)
        {
            foreach (var session in this._sessionMap.Values)
            {
                var connInfo = session.ConnectionInfo;
                if (connInfo != null)
                {
                    string currentKey = ConnectedBindingQueue.GetConnectionContextKey(connInfo.ConnectionDetails);
                    if (queueKey == currentKey)
                    {
                        return session.Uri;
                    }
                }
            }
            return string.Empty;
        }
    }

}
