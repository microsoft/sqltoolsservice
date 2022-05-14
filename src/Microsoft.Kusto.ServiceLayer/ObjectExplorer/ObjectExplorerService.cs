﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class ObjectExplorerService : HostedService<ObjectExplorerService>, IComposableService, IHostedService, IDisposable
    {
        private readonly IConnectedBindingQueue _connectedBindingQueue;

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

        private IConnectionManager _connectionManager;

        /// <summary>
        /// Singleton constructor
        /// </summary>
        public ObjectExplorerService(IConnectedBindingQueue connectedBindingQueue)
        {
            _connectedBindingQueue = connectedBindingQueue;
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
        /// As an <see cref="IComposableService"/>, this will be set whenever the service is initialized
        /// via an <see cref="IMultiServiceProvider"/>
        /// </summary>
        /// <param name="provider"></param>
        public override void SetServiceProvider(IMultiServiceProvider provider)
        {
            Validate.IsNotNull(nameof(provider), provider);
            _serviceProvider = provider;
            _connectionService = provider.GetService<ConnectionService>();
            _connectionManager = provider.GetService<IConnectionManager>();
            
            try
            {
                _connectionService.RegisterConnectedQueue(connectionName, _connectedBindingQueue);

            }
            catch(Exception ex)
            {
                Logger.Write(TraceEventType.Error, ex.Message);
            }
        }

        /// <summary>
        /// Initializes the service with the service host and registers request handlers.
        /// </summary>
        /// <param name="serviceHost">The service host instance to register with</param>
        public override void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Write(TraceEventType.Verbose, "ObjectExplorer service initialized");
            _serviceHost = serviceHost;

            _connectedBindingQueue.OnUnhandledException += OnUnhandledException;

            // Register handlers for requests
            serviceHost.SetRequestHandler(CreateSessionRequest.Type, HandleCreateSessionRequest);
            serviceHost.SetRequestHandler(ExpandRequest.Type, HandleExpandRequest);
            serviceHost.SetRequestHandler(RefreshRequest.Type, HandleRefreshRequest);
            serviceHost.SetRequestHandler(CloseSessionRequest.Type, HandleCloseSessionRequest);
            serviceHost.SetRequestHandler(FindNodesRequest.Type, HandleFindNodesRequest);
            
            WorkspaceService<SqlToolsSettings> workspaceService = _serviceProvider.GetService<WorkspaceService<SqlToolsSettings>>();
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


        internal async Task HandleCreateSessionRequest(ConnectionDetails connectionDetails, RequestContext<CreateSessionResponse> context)
        {
            try
            {
                Logger.Write(TraceEventType.Verbose, "HandleCreateSessionRequest");
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
            catch (Exception ex)
            {
                await context.SendError(ex.ToString());
            }

        }

        internal async Task HandleExpandRequest(ExpandParams expandParams, RequestContext<bool> context)
        {
            Logger.Write(TraceEventType.Verbose, "HandleExpandRequest");

            Func<Task<bool>> expandNode = async () =>
            {
                Validate.IsNotNull(nameof(expandParams), expandParams);
                Validate.IsNotNull(nameof(context), context);

                string uri = expandParams.SessionId;
                ObjectExplorerSession session = null;
                if (!_sessionMap.TryGetValue(uri, out session))
                {
                    Logger.Write(TraceEventType.Verbose, $"Cannot expand object explorer node. Couldn't find session for uri. {uri} ");
                    await _serviceHost.SendEvent(ExpandCompleteNotification.Type, new ExpandResponse
                    {
                        SessionId = expandParams.SessionId,
                        NodePath = expandParams.NodePath,
                        ErrorMessage = $"Couldn't find session for session: {uri}"
                    });
                    return false;
                }
                else
                {
                    await RunExpandTask(session, expandParams);
                    return true;
                }
            };
            await HandleRequestAsync(expandNode, context, "HandleExpandRequest");
        }

        internal async Task HandleRefreshRequest(RefreshParams refreshParams, RequestContext<bool> context)
        {
            try
            {
                Logger.Write(TraceEventType.Verbose, "HandleRefreshRequest");
                Validate.IsNotNull(nameof(refreshParams), refreshParams);
                Validate.IsNotNull(nameof(context), context);

                string uri = refreshParams.SessionId;
                ObjectExplorerSession session = null;
                if (!_sessionMap.TryGetValue(uri, out session))
                {
                    Logger.Write(TraceEventType.Verbose, $"Cannot expand object explorer node. Couldn't find session for uri. {uri} ");
                    await _serviceHost.SendEvent(ExpandCompleteNotification.Type, new ExpandResponse
                    {
                        SessionId = refreshParams.SessionId,
                        NodePath = refreshParams.NodePath,
                        ErrorMessage = $"Couldn't find session for session: {uri}"
                    });
                }
                else
                {
                    await RunExpandTask(session, refreshParams, true);
                }
                await context.SendResult(true);
            }
            catch (Exception ex)
            {
                await context.SendError(ex.ToString());
            }
        }

        internal async Task HandleCloseSessionRequest(CloseSessionParams closeSessionParams, RequestContext<CloseSessionResponse> context)
        {

            Logger.Write(TraceEventType.Verbose, "HandleCloseSessionRequest");
            Func<Task<CloseSessionResponse>> closeSession = () =>
            {
                Validate.IsNotNull(nameof(closeSessionParams), closeSessionParams);
                Validate.IsNotNull(nameof(context), context);
                return Task.Factory.StartNew(() =>
                {
                    string uri = closeSessionParams.SessionId;
                    ObjectExplorerSession session = null;
                    bool success = false;
                    if (!_sessionMap.TryGetValue(uri, out session))
                    {
                        Logger.Write(TraceEventType.Verbose, $"Cannot close object explorer session. Couldn't find session for uri. {uri} ");
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

        internal async Task HandleFindNodesRequest(FindNodesParams findNodesParams, RequestContext<FindNodesResponse> context)
        {
            var foundNodes = FindNodes(findNodesParams.SessionId, findNodesParams.Type, findNodesParams.Schema, findNodesParams.Name, findNodesParams.Database, findNodesParams.ParentObjectNames);
            if (foundNodes == null)
            {
                foundNodes = new List<TreeNode>();
            }
            await context.SendResult(new FindNodesResponse { Nodes = foundNodes.Select(node => node.ToNodeInfo()).ToList() });
        }

        internal void CloseSession(string uri)
        {
            ObjectExplorerSession session;
            if (_sessionMap.TryGetValue(uri, out session))
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
            }
        }

        private void RunCreateSessionTask(ConnectionDetails connectionDetails, string uri)
        {
            Logger.Write(TraceEventType.Information, "Creating OE session");
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            if (connectionDetails != null && !string.IsNullOrEmpty(uri))
            {
                Task task = CreateSessionAsync(connectionDetails, uri, cancellationTokenSource.Token);
                Task.Run(async () =>
                {
                    ObjectExplorerTaskResult result = await RunTaskWithTimeout(task,
                        _settings?.CreateSessionTimeout ?? ObjectExplorerSettings.DefaultCreateSessionTimeout);

                    if (result != null && !result.IsCompleted)
                    {
                        cancellationTokenSource.Cancel();
                        SessionCreatedParameters response = new SessionCreatedParameters
                        {
                            Success = false,
                            SessionId = uri,
                            ErrorMessage = result.Exception != null ? result.Exception.Message : $"Failed to create session for session id {uri}"

                        };
                        await _serviceHost.SendEvent(CreateSessionCompleteNotification.Type, response);
                    }
                    return result;
                }).ContinueWithOnFaulted(null);
            }
        }

        private async Task<SessionCreatedParameters> CreateSessionAsync(ConnectionDetails connectionDetails, string uri, CancellationToken cancellationToken)
        {
            ObjectExplorerSession session;
            if (!_sessionMap.TryGetValue(uri, out session))
            {
                // Establish a connection to the specified server/database
                session = await DoCreateSession(connectionDetails, uri);
            }

            SessionCreatedParameters response;
            if (session != null && !cancellationToken.IsCancellationRequested)
            {
                // Else we have a session available, response with existing session information
                response = new SessionCreatedParameters
                {
                    Success = true,
                    RootNode = session.Root.ToNodeInfo(),
                    SessionId = uri,
                    ErrorMessage = session.ErrorMessage
                };
                await _serviceHost.SendEvent(CreateSessionCompleteNotification.Type, response);
                return response;
            }
            return null;

        }

        internal async Task<ExpandResponse> ExpandNode(ObjectExplorerSession session, string nodePath, bool forceRefresh = false)
        {
            return await Task.Factory.StartNew(() =>
            {
                return QueueExpandNodeRequest(session, nodePath, forceRefresh);
            });
        }
        internal ExpandResponse QueueExpandNodeRequest(ObjectExplorerSession session, string nodePath, bool forceRefresh = false)
        {
            NodeInfo[] nodes = null;
            TreeNode node = session.Root.FindNodeByPath(nodePath);
            ExpandResponse response = null;

            // This node was likely returned from a different node provider. Ignore expansion and return an empty array
            // since we don't need to add any nodes under this section of the tree.
            if (node == null)
            {
                response = new ExpandResponse { Nodes = new NodeInfo[] { }, ErrorMessage = string.Empty, SessionId = session.Uri, NodePath = nodePath };
                response.Nodes = new NodeInfo[0];
                return response;
            }
            else
            {
                response = new ExpandResponse { Nodes = new NodeInfo[] { }, ErrorMessage = node.ErrorMessage, SessionId = session.Uri, NodePath = nodePath };
            }

            if (node != null && Monitor.TryEnter(node.BuildingMetadataLock, LanguageService.OnConnectionWaitTimeout))
            {
                try
                {
                    int timeout = (int)TimeSpan.FromSeconds(_settings?.ExpandTimeout ?? ObjectExplorerSettings.DefaultExpandTimeout).TotalMilliseconds;
                    QueueItem queueItem = _connectedBindingQueue.QueueBindingOperation(
                           key: _connectedBindingQueue.AddConnectionContext(session.ConnectionInfo, false, connectionName, false),
                           bindingTimeout: timeout,
                           waitForLockTimeout: timeout,
                           bindOperation: (bindingContext, cancelToken) =>
                           {
                               if (forceRefresh)
                               {
                                   nodes = node.Refresh(cancelToken).Select(x => x.ToNodeInfo()).ToArray();
                               }
                               else
                               {
                                   nodes = node.Expand(cancelToken).Select(x => x.ToNodeInfo()).ToArray();
                               }
                               response.Nodes = nodes;
                               response.ErrorMessage = node.ErrorMessage;

                               return response;
                           });

                    queueItem.ItemProcessed.WaitOne();
                    if (queueItem.GetResultAsT<ExpandResponse>() != null)
                    {
                        response = queueItem.GetResultAsT<ExpandResponse>();
                    }
                }
                catch
                {
                }
                finally
                {
                    Monitor.Exit(node.BuildingMetadataLock);
                }
            }
            return response;
        }

        /// <summary>
        /// Establishes a new session and stores its information
        /// </summary>
        /// <returns><see cref="ObjectExplorerSession"/> object if successful, null if unsuccessful</returns>
        internal async Task<ObjectExplorerSession> DoCreateSession(ConnectionDetails connectionDetails, string uri)
        {
            try
            {
                ObjectExplorerSession session = null;
                connectionDetails.PersistSecurityInfo = true;
                ConnectParams connectParams = new ConnectParams() { OwnerUri = uri, Connection = connectionDetails, Type = Connection.ConnectionType.ObjectExplorer };
                bool isDefaultOrSystemDatabase = DatabaseUtils.IsSystemDatabaseConnection(connectionDetails.DatabaseName) || string.IsNullOrWhiteSpace(connectionDetails.DatabaseDisplayName);

                ConnectionInfo connectionInfo;
                ConnectionCompleteParams connectionResult = await Connect(connectParams, uri);
                if (!_connectionManager.TryGetValue(uri, out connectionInfo))
                {
                    return null;
                }

                if (connectionResult == null)
                {
                    // Connection failed and notification is already sent
                    return null;
                }

                int timeout = (int)TimeSpan.FromSeconds(_settings?.CreateSessionTimeout ?? ObjectExplorerSettings.DefaultCreateSessionTimeout).TotalMilliseconds;
                QueueItem queueItem = _connectedBindingQueue.QueueBindingOperation(
                           key: _connectedBindingQueue.AddConnectionContext(connectionInfo, false, connectionName),
                           bindingTimeout: timeout,
                           waitForLockTimeout: timeout,
                           bindOperation: (bindingContext, cancelToken) =>
                           {
                               session = ObjectExplorerSession.CreateSession(connectionResult, _serviceProvider, bindingContext.DataSource, isDefaultOrSystemDatabase);
                               session.ConnectionInfo = connectionInfo;

                               _sessionMap.AddOrUpdate(uri, session, (key, oldSession) => session);
                               return session;
                           });

                queueItem.ItemProcessed.WaitOne();
                if (queueItem.GetResultAsT<ObjectExplorerSession>() != null)
                {
                    session = queueItem.GetResultAsT<ObjectExplorerSession>();
                }
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
            string connectionErrorMessage = string.Empty;
            try
            {
                // open connection based on request details
                ConnectionCompleteParams result = await _connectionService.Connect(connectParams);
                connectionErrorMessage = result != null ? $"{result.Messages} error code:{result.ErrorNumber}"  : string.Empty;
                if (result != null && !string.IsNullOrEmpty(result.ConnectionId))
                {
                    return result;
                }
                else
                {
                    await SendSessionFailedNotification(uri, result.ErrorMessage);
                    return null;
                }
               
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
            Task task = ExpandNodeAsync(session, expandParams,  cancellationTokenSource.Token, forceRefresh);
            await Task.Run(async () =>
            {
                ObjectExplorerTaskResult result =  await RunTaskWithTimeout(task, 
                    _settings?.ExpandTimeout ?? ObjectExplorerSettings.DefaultExpandTimeout);

                if (result != null && !result.IsCompleted)
                {
                    cancellationTokenSource.Cancel();
                    ExpandResponse response = CreateExpandResponse(session, expandParams);
                    response.ErrorMessage = result.Exception != null ? result.Exception.Message: $"Failed to expand node: {expandParams.NodePath} in session {session.Uri}";
                    await _serviceHost.SendEvent(ExpandCompleteNotification.Type, response);
                }
                return result;
            }).ContinueWithOnFaulted(null);
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
            ExpandResponse response = null;
            response = await ExpandNode(session, expandParams.NodePath, forceRefresh);
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.Write(TraceEventType.Verbose, "OE expand canceled");
            }
            else
            {
                await _serviceHost.SendEvent(ExpandCompleteNotification.Type, response);
            }
        }

        private ExpandResponse CreateExpandResponse(ObjectExplorerSession session, ExpandParams expandParams)
        {
            return new ExpandResponse() { SessionId = session.Uri, NodePath = expandParams.NodePath };
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
            foreach (var session in _sessionMap.Values)
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
