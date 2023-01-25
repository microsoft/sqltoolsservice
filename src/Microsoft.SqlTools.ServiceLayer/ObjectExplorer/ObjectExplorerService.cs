﻿//
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
using Microsoft.Data.SqlClient;
using Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.TableDesigner;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer
{
    /// <summary>
    /// A Service to support querying server and database information as an Object Explorer tree.
    /// The APIs used for this are modeled closely on the VSCode TreeExplorerNodeProvider API.
    /// </summary>
    [Export(typeof(IHostedService))]
    public class ObjectExplorerService : HostedService<ObjectExplorerService>, IComposableService, IHostedService, IDisposable
    {
        internal const string uriPrefix = "objectexplorer://";

        // Instance of the connection service, used to get the connection info for a given owner URI
        private ConnectionService connectionService;
        private IProtocolEndpoint serviceHost;
        private ConcurrentDictionary<string, ObjectExplorerSession> sessionMap;
        private readonly Lazy<Dictionary<string, HashSet<ChildFactory>>> applicableNodeChildFactories;
        private IMultiServiceProvider serviceProvider;
        private ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue(needsMetadata: false);
        private string connectionName = "ObjectExplorer";

        /// <summary>
        /// This timeout limits the amount of time that object explorer tasks can take to complete
        /// </summary>
        private ObjectExplorerSettings settings;

        /// <summary>
        /// Singleton constructor
        /// </summary>
        public ObjectExplorerService()
        {
            sessionMap = new ConcurrentDictionary<string, ObjectExplorerSession>();
            applicableNodeChildFactories = new Lazy<Dictionary<string, HashSet<ChildFactory>>>(() => PopulateFactories());
            NodePathGenerator.Initialize();
        }

        internal ConnectedBindingQueue ConnectedBindingQueue
        {
            get
            {
                return bindingQueue;
            }
            set
            {
                this.bindingQueue = value;
            }
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
            try
            {
                connectionService.RegisterConnectedQueue(connectionName, bindingQueue);

            }
            catch (Exception ex)
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
            this.serviceHost = serviceHost;

            this.ConnectedBindingQueue.OnUnhandledException += OnUnhandledException;

            // Not enabling parallel processing for OEService as it might cause OE to hang.
            // Register handlers for requests
            serviceHost.SetRequestHandler(CreateSessionRequest.Type, HandleCreateSessionRequest);
            serviceHost.SetRequestHandler(ExpandRequest.Type, HandleExpandRequest);
            serviceHost.SetRequestHandler(RefreshRequest.Type, HandleRefreshRequest);
            serviceHost.SetRequestHandler(CloseSessionRequest.Type, HandleCloseSessionRequest);
            serviceHost.SetRequestHandler(FindNodesRequest.Type, HandleFindNodesRequest);
            WorkspaceService<SqlToolsSettings> workspaceService = WorkspaceService;
            if (workspaceService != null)
            {
                workspaceService.RegisterConfigChangeCallback(HandleDidChangeConfigurationNotification);
            }

        }

        /// <summary>
        /// Gets the workspace service. Note: should handle case where this is null in cases where unit tests do not set this up
        /// </summary>
        private WorkspaceService<SqlToolsSettings> WorkspaceService
        {
            get { return serviceProvider.GetService<WorkspaceService<SqlToolsSettings>>(); }
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
            settings = newSettings?.SqlTools?.ObjectExplorer ?? settings;
            return Task.FromResult(true);
        }


        internal async Task HandleCreateSessionRequest(ConnectionDetails connectionDetails, RequestContext<CreateSessionResponse> context)
        {
            Logger.Write(TraceEventType.Verbose, "HandleCreateSessionRequest");
            Func<Task<CreateSessionResponse>> doCreateSession = async () =>
            {
                Validate.IsNotNull(nameof(connectionDetails), connectionDetails);
                Validate.IsNotNull(nameof(context), context);
                return await Task.Run(() =>
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
            Logger.Write(TraceEventType.Verbose, "HandleExpandRequest");

            Func<Task<bool>> expandNode = async () =>
            {
                Validate.IsNotNull(nameof(expandParams), expandParams);
                Validate.IsNotNull(nameof(context), context);

                string uri = expandParams.SessionId;
                ObjectExplorerSession session = null;
                if (!sessionMap.TryGetValue(uri, out session))
                {
                    Logger.Write(TraceEventType.Verbose, $"Cannot expand object explorer node. Couldn't find session for uri. {uri} ");
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
                    bool refreshNeeded = session.ConnectionInfo.TryUpdateAccessToken(expandParams.SecurityToken);
                    RunExpandTask(session, expandParams, refreshNeeded);
                    return true;
                }
            };
            await HandleRequestAsync(expandNode, context, "HandleExpandRequest");
        }

        internal async Task HandleRefreshRequest(RefreshParams refreshParams, RequestContext<bool> context)
        {
            Logger.Write(TraceEventType.Verbose, "HandleRefreshRequest");
            Validate.IsNotNull(nameof(refreshParams), refreshParams);
            Validate.IsNotNull(nameof(context), context);

            string uri = refreshParams.SessionId;
            ObjectExplorerSession session = null;
            if (string.IsNullOrEmpty(uri) || !sessionMap.TryGetValue(uri, out session))
            {
                Logger.Write(TraceEventType.Verbose, $"Cannot expand object explorer node. Couldn't find session for uri. {uri} ");
                await serviceHost.SendEvent(ExpandCompleteNotification.Type, new ExpandResponse
                {
                    SessionId = refreshParams.SessionId,
                    NodePath = refreshParams.NodePath,
                    ErrorMessage = $"Couldn't find session for session: {uri}"
                });
            }
            else
            {
                session.ConnectionInfo.TryUpdateAccessToken(refreshParams.SecurityToken);
                RunExpandTask(session, refreshParams, true);
            }
            await context.SendResult(true);
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
                    if (!sessionMap.TryGetValue(uri, out session))
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
            foundNodes ??= new List<TreeNode>();

            await context.SendResult(new FindNodesResponse { Nodes = foundNodes.Select(node => node.ToNodeInfo()).ToList() });
        }

        internal void CloseSession(string uri)
        {
            ObjectExplorerSession session;
            if (sessionMap.TryGetValue(uri, out session))
            {
                // Remove the session from active sessions and disconnect
                if (sessionMap.TryRemove(session.Uri, out session))
                {
                    if (session != null && session.ConnectionInfo != null)
                    {
                        bindingQueue.RemoveBindigContext(session.ConnectionInfo);
                    }
                }
                connectionService.Disconnect(new DisconnectParams()
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
                CreateSessionTask = task;
                Task.Run(async () =>
                {
                    ObjectExplorerTaskResult result = await RunTaskWithTimeout(task,
                        settings?.CreateSessionTimeout ?? ObjectExplorerSettings.DefaultCreateSessionTimeout);

                    if (result != null && !result.IsSuccessful)
                    {
                        cancellationTokenSource.Cancel();
                        SessionCreatedParameters response = new SessionCreatedParameters
                        {
                            Success = false,
                            SessionId = uri,
                            ErrorNumber = result.Exception != null && result.Exception is SqlException sqlEx ? sqlEx.ErrorCode : null,
                            ErrorMessage = result.Exception != null ? result.Exception.Message : $"Failed to create session for session id {uri}"

                        };
                        await serviceHost.SendEvent(CreateSessionCompleteNotification.Type, response);
                    }
                    return result;
                }).ContinueWithOnFaulted(null);
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

        private async Task<SessionCreatedParameters> CreateSessionAsync(ConnectionDetails connectionDetails, string uri, CancellationToken cancellationToken)
        {
            ObjectExplorerSession session;
            if (!sessionMap.TryGetValue(uri, out session))
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
                    ErrorNumber = session.ErrorNumber,
                    ErrorMessage = session.ErrorMessage
                };
                await serviceHost.SendEvent(CreateSessionCompleteNotification.Type, response);
                return response;
            }
            return null;

        }

        internal Task<ExpandResponse> ExpandNode(ObjectExplorerSession session, string nodePath, bool forceRefresh = false, SecurityToken? securityToken = null)
        {
            return Task.Run(() => QueueExpandNodeRequest(session, nodePath, forceRefresh, securityToken));
        }

        internal ExpandResponse QueueExpandNodeRequest(ObjectExplorerSession session, string nodePath, bool forceRefresh = false, SecurityToken? securityToken = null)
        {
            NodeInfo[] nodes = null;
            TreeNode? node = session.Root.FindNodeByPath(nodePath);
            ExpandResponse response = null;

            // Performance Optimization for table designer to load the database model earlier based on user configuration.
            if (node?.NodeTypeId == NodeTypes.Database && TableDesignerService.Instance.Settings.PreloadDatabaseModel)
            {
                // The operation below are not blocking, but just in case, wrapping it with a task run to make sure it has no impact on the node expansion time.
                var _ = Task.Run(() =>
                {
                    try
                    {
                        var builder = ConnectionService.CreateConnectionStringBuilder(session.ConnectionInfo.ConnectionDetails);
                        builder.InitialCatalog = node.NodeValue;
                        builder.ApplicationName = TableDesignerService.TableDesignerApplicationName;
                        var azureToken = session.ConnectionInfo.ConnectionDetails.AzureAccountToken;
                        TableDesignerCacheManager.StartDatabaseModelInitialization(builder.ToString(), azureToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.Write(TraceEventType.Warning, $"Failed to start database initialization for table designer: {ex.Message}");
                    }
                });
            }

            // This node was likely returned from a different node provider. Ignore expansion and return an empty array
            // since we don't need to add any nodes under this section of the tree.
            if (node == null)
            {
                Logger.Verbose($"No node returned from FindNodeByPath for {nodePath}");
                response = new ExpandResponse { Nodes = new NodeInfo[] { }, ErrorMessage = string.Empty, SessionId = session.Uri, NodePath = nodePath };
                response.Nodes = new NodeInfo[0];
                return response;
            }
            else
            {
                Logger.Verbose($"Got node from FindNodeByPath for {nodePath}");
                response = new ExpandResponse { Nodes = new NodeInfo[] { }, ErrorMessage = node.ErrorMessage, SessionId = session.Uri, NodePath = nodePath };
            }
            Logger.Verbose($"Before enter BuildingMetadataLock for {nodePath}");
            if (node != null && Monitor.TryEnter(node.BuildingMetadataLock, LanguageService.OnConnectionWaitTimeout))
            {
                Logger.Verbose($"After enter BuildingMetadataLock for {nodePath}");
                try
                {
                    int timeout = (int)TimeSpan.FromSeconds(settings?.ExpandTimeout ?? ObjectExplorerSettings.DefaultExpandTimeout).TotalMilliseconds;
                    QueueItem queueItem = bindingQueue.QueueBindingOperation(
                           key: bindingQueue.AddConnectionContext(session.ConnectionInfo, connectionName),
                           bindingTimeout: timeout,
                           waitForLockTimeout: timeout,
                           bindOperation: (bindingContext, cancelToken) =>
                           {
                               if (!session.ConnectionInfo.IsAzureAuth)
                               {
                                   // explicitly set null here to prevent setting access token for non-Azure auth modes.
                                   securityToken = null;
                               }

                               if (forceRefresh)
                               {
                                   Logger.Verbose($"Forcing refresh for {nodePath}");
                                   nodes = node.Refresh(cancelToken, securityToken?.Token).Select(x => x.ToNodeInfo()).ToArray();
                               }
                               else
                               {
                                   Logger.Verbose($"Expanding {nodePath}");
                                   nodes = node.Expand(cancelToken, securityToken?.Token).Select(x => x.ToNodeInfo()).ToArray();
                               }
                               response.Nodes = nodes;
                               response.ErrorMessage = node.ErrorMessage;
                               try
                               {
                                   // SMO changes the database when getting sql objects. Make sure the database is changed back to the original one
                                   if (bindingContext.ServerConnection.CurrentDatabase != bindingContext.ServerConnection.DatabaseName)
                                   {
                                       bindingContext.ServerConnection.SqlConnectionObject.ChangeDatabase(bindingContext.ServerConnection.DatabaseName);
                                   }
                               }
                               catch (Exception ex)
                               {
                                   Logger.Write(TraceEventType.Warning, $"Failed to change the database in OE connection. error: {ex.Message}");
                                   // We should just try to change the connection. If it fails, there's not much we can do
                               }
                               return response;
                           });
                    Logger.Verbose($"Queuing binding operation for {nodePath}");
                    queueItem.ItemProcessed.WaitOne();
                    Logger.Verbose($"Done with binding operation for {nodePath}");
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
                if (!connectionService.TryFindConnection(uri, out connectionInfo))
                {
                    return null;
                }

                if (connectionResult == null)
                {
                    // Connection failed and notification is already sent
                    return null;
                }

                int timeout = (int)TimeSpan.FromSeconds(settings?.CreateSessionTimeout ?? ObjectExplorerSettings.DefaultCreateSessionTimeout).TotalMilliseconds;
                QueueItem queueItem = bindingQueue.QueueBindingOperation(
                           key: bindingQueue.AddConnectionContext(connectionInfo, connectionName),
                           bindingTimeout: timeout,
                           waitForLockTimeout: timeout,
                           bindOperation: (bindingContext, cancelToken) =>
                           {
                               session = ObjectExplorerSession.CreateSession(connectionResult, serviceProvider, bindingContext.ServerConnection, isDefaultOrSystemDatabase);
                               session.ConnectionInfo = connectionInfo;

                               sessionMap.AddOrUpdate(uri, session, (key, oldSession) => session);
                               return session;
                           });

                queueItem.ItemProcessed.WaitOne();
                if (queueItem.GetResultAsT<ObjectExplorerSession>() != null)
                {
                    session = queueItem.GetResultAsT<ObjectExplorerSession>();
                }
                return session;
            }
            catch (Exception ex)
            {
                int? errorCode = ex is SqlException sqlEx ? sqlEx.ErrorCode : null;
                await SendSessionFailedNotification(uri, ex.Message, errorCode);
                return null;
            }
        }

        private async Task<ConnectionCompleteParams> Connect(ConnectParams connectParams, string uri)
        {
            string connectionErrorMessage = string.Empty;
            try
            {
                // open connection based on request details
                ConnectionCompleteParams result = await connectionService.Connect(connectParams);
                connectionErrorMessage = result != null ? $"{result.Messages} error code:{result.ErrorNumber}" : string.Empty;
                if (result != null && !string.IsNullOrEmpty(result.ConnectionId))
                {
                    return result;
                }
                else
                {
                    await SendSessionFailedNotification(uri, result.ErrorMessage, result.ErrorNumber);
                    return null;
                }

            }
            catch (Exception ex)
            {
                int? errorNum = ex is SqlException sqlEx ? sqlEx.Number : null;
                await SendSessionFailedNotification(uri, ex.ToString(), errorNum);
                return null;
            }
        }

        private async Task SendSessionFailedNotification(string uri, string errorMessage, int? errorCode)
        {
            Logger.Write(TraceEventType.Warning, $"Failed To create OE session: {errorMessage}");
            SessionCreatedParameters result = new SessionCreatedParameters()
            {
                Success = false,
                ErrorMessage = errorMessage,
                ErrorNumber = errorCode,
                SessionId = uri
            };
            await serviceHost.SendEvent(CreateSessionCompleteNotification.Type, result);
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
            await serviceHost.SendEvent(SessionDisconnectedNotification.Type, result);
        }

        private void RunExpandTask(ObjectExplorerSession session, ExpandParams expandParams, bool forceRefresh = false)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Task task = ExpandNodeAsync(session, expandParams, cancellationTokenSource.Token, forceRefresh);
            ExpandTask = task;
            Task.Run(async () =>
            {
                ObjectExplorerTaskResult result = await RunTaskWithTimeout(task,
                    settings?.ExpandTimeout ?? ObjectExplorerSettings.DefaultExpandTimeout);

                if (result != null && !result.IsSuccessful)
                {
                    cancellationTokenSource.Cancel();
                    ExpandResponse response = CreateExpandResponse(session, expandParams);
                    response.ErrorMessage = result.Exception != null ? result.Exception.Message : $"Failed to expand node: {expandParams.NodePath} in session {session.Uri}";
                    await serviceHost.SendEvent(ExpandCompleteNotification.Type, response);
                }
                return result;
            }).ContinueWithOnFaulted(null);
        }

        private async Task<ObjectExplorerTaskResult> RunTaskWithTimeout(Task task, int timeoutInSec)
        {
            ObjectExplorerTaskResult result = new ObjectExplorerTaskResult();
            TimeSpan timeout = TimeSpan.FromSeconds(timeoutInSec);
            await Task.WhenAny(task, Task.Delay(timeout));
            result.IsSuccessful = task.IsCompleted;
            if (task.Exception != null)
            {
                result.IsSuccessful = false;
                result.Exception = task.Exception;
            }
            else if (!task.IsCompleted)
            {
                result.Exception = new TimeoutException($"Object Explorer task didn't complete within {timeoutInSec} seconds.");
            }
            return result;
        }

        /// <summary>
        /// For tests only
        /// </summary>
        internal Task ExpandTask
        {
            get;
            set;
        }

        private async Task ExpandNodeAsync(ObjectExplorerSession session, ExpandParams expandParams, CancellationToken cancellationToken, bool forceRefresh = false)
        {
            ExpandResponse response = null;
            response = await ExpandNode(session, expandParams.NodePath, forceRefresh, expandParams.SecurityToken);
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.Write(TraceEventType.Verbose, "OE expand canceled");
            }
            else
            {
                await serviceHost.SendEvent(ExpandCompleteNotification.Type, response);
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
            var oeSession = sessionMap.GetValueOrDefault(sessionId);
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
            /// <summary>
            /// Whether the task was successfully completed. False if an error of any kind occurred during execution.
            /// </summary>
            public bool IsSuccessful { get; set; }
            /// <summary>
            /// The Exception that occurred during execution, if any. 
            /// </summary>
            public Exception? Exception { get; set; }
        }

        public void Dispose()
        {
            if (bindingQueue != null)
            {
                bindingQueue.OnUnhandledException -= OnUnhandledException;
                bindingQueue.Dispose();
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
            foreach (var session in this.sessionMap.Values)
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

            public ConnectionInfo ConnectionInfo { get; set; }

            public int? ErrorNumber { get; set; }

            public string ErrorMessage { get; set; }

            public static ObjectExplorerSession CreateSession(ConnectionCompleteParams response, IMultiServiceProvider serviceProvider, ServerConnection serverConnection, bool isDefaultOrSystemDatabase)
            {
                ServerNode rootNode = new ServerNode(response, serviceProvider, serverConnection);
                var session = new ObjectExplorerSession(response.OwnerUri, rootNode, serviceProvider, serviceProvider.GetService<ConnectionService>());
                if (!isDefaultOrSystemDatabase)
                {
                    // Assuming the databases are in a folder under server node
                    DatabaseTreeNode databaseNode = new DatabaseTreeNode(rootNode, response.ConnectionSummary.DatabaseName);
                    session.Root = databaseNode;
                }

                if (response?.ErrorMessage != null)
                {
                    session.ErrorMessage = response.ErrorMessage;
                    session.ErrorNumber = response.ErrorNumber;
                }
                return session;
            }

        }
    }

}
