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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
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
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;

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
        private Dictionary<string, HashSet<TreeFolder>> typeMap = new Dictionary<string, HashSet<TreeFolder>>();


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
            try
            {
                connectionService.RegisterConnectedQueue(connectionName, bindingQueue);

            }
            catch(Exception ex)
            {
                Logger.Write(LogLevel.Error, ex.Message);
            }
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
            try
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
            catch (Exception ex)
            {
                await context.SendError(ex.ToString());
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
            try
            {
                Logger.Write(LogLevel.Verbose, "HandleRefreshRequest");
                Validate.IsNotNull(nameof(refreshParams), refreshParams);
                Validate.IsNotNull(nameof(context), context);

                string uri = refreshParams.SessionId;
                ObjectExplorerSession session = null;
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
            catch (Exception ex)
            {
                await context.SendError(ex.ToString());
            }
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
            if (sessionMap.TryGetValue(uri, out session))
            {
                // Remove the session from active sessions and disconnect
                if(sessionMap.TryRemove(session.Uri, out session))
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
            Logger.Write(LogLevel.Normal, "Creating OE session");
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            if (connectionDetails != null && !string.IsNullOrEmpty(uri))
            {
                Task task = CreateSessionAsync(connectionDetails, uri, cancellationTokenSource.Token);
                CreateSessionTask = task;
                Task.Run(async () =>
                {
                    ObjectExplorerTaskResult result = await RunTaskWithTimeout(task,
                        settings?.CreateSessionTimeout ?? ObjectExplorerSettings.DefaultCreateSessionTimeout);

                    if (result != null && !result.IsCompleted)
                    {
                        cancellationTokenSource.Cancel();
                        SessionCreatedParameters response = new SessionCreatedParameters
                        {
                            Success = false,
                            SessionId = uri,
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
                    ErrorMessage = session.ErrorMessage

                };
                await serviceHost.SendEvent(CreateSessionCompleteNotification.Type, response);
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
            ExpandResponse response = new ExpandResponse { Nodes = new NodeInfo[] { }, ErrorMessage = node.ErrorMessage, SessionId = session.Uri, NodePath = nodePath };

            if (node != null && Monitor.TryEnter(node.BuildingMetadataLock, LanguageService.OnConnectionWaitTimeout))
            {
                try
                {
                    int timeout = (int)TimeSpan.FromSeconds(settings?.ExpandTimeout ?? ObjectExplorerSettings.DefaultExpandTimeout).TotalMilliseconds;
                    QueueItem queueItem = bindingQueue.QueueBindingOperation(
                           key: bindingQueue.AddConnectionContext(session.ConnectionInfo, connectionName),
                           bindingTimeout: timeout,
                           waitForLockTimeout: timeout,
                           bindOperation: (bindingContext, cancelToken) =>
                           {
                               if (forceRefresh)
                               {
                                   nodes = node.Refresh().Select(x => x.ToNodeInfo()).ToArray();
                               }
                               else
                               {
                                   nodes = node.Expand().Select(x => x.ToNodeInfo()).ToArray();
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
                               catch(Exception ex)
                               {
                                   Logger.Write(LogLevel.Warning, $"Failed to change the database in OE connection. error: {ex.Message}");
                                   // We should just try to change the connection. If it fails, there's not much we can do
                               }
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
                ConnectionCompleteParams result = await connectionService.Connect(connectParams);
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
            Logger.Write(LogLevel.Warning, $"Failed To create OE session: {errorMessage}");
            SessionCreatedParameters result = new SessionCreatedParameters()
            {
                Success = false,
                ErrorMessage = errorMessage,
                SessionId = uri
            };
            await serviceHost.SendEvent(CreateSessionCompleteNotification.Type, result);
        }

        private void RunExpandTask(ObjectExplorerSession session, ExpandParams expandParams, bool forceRefresh = false)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Task task = ExpandNodeAsync(session, expandParams,  cancellationTokenSource.Token, forceRefresh);
            ExpandTask = task;
            Task.Run(async () =>
            {
                ObjectExplorerTaskResult result =  await RunTaskWithTimeout(task, 
                    settings?.ExpandTimeout ?? ObjectExplorerSettings.DefaultExpandTimeout);

                if (result != null && !result.IsCompleted)
                {
                    cancellationTokenSource.Cancel();
                    ExpandResponse response = CreateExpandResponse(session, expandParams);
                    response.ErrorMessage = result.Exception != null ? result.Exception.Message: $"Failed to expand node: {expandParams.NodePath} in session {session.Uri}";
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
            response = await ExpandNode(session, expandParams.NodePath, forceRefresh);
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.Write(LogLevel.Verbose, "OE expand canceled");
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
            Validate.IsNotNull("details", details);
            string uri = string.Format(CultureInfo.InvariantCulture, "{0}{1}", uriPrefix, Uri.EscapeUriString(details.ServerName));
            uri = AppendIfExists(uri, "databaseName", details.DatabaseName);
            uri = AppendIfExists(uri, "user", details.UserName);
            uri = AppendIfExists(uri, "groupId", details.GroupId);
            uri = AppendIfExists(uri, "displayName", details.DatabaseDisplayName);
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

        public List<TreeNode> FindNodes(string sessionId, string typeName, string schema, string name, string databaseName, List<string> parentNames = null)
        {
            if (this.typeMap.Count == 0)
            {
                this.LoadTreeStructure();
            }
            
            var oeSession = sessionMap.GetValueOrDefault(sessionId);
            var matchingTreeFolders = this.typeMap.GetValueOrDefault(typeName);
            if (oeSession == null || matchingTreeFolders == null)
            {
                return null;
            }

            var outputPaths = new List<string>();
            foreach (var treeFolder in matchingTreeFolders)
            {
                List<string> clonedParentNames = null;
                if (parentNames != null)
                {
                    clonedParentNames = parentNames.ToList();
                }
                TreeFolder lastFolder = null;
                var currentTreeFolder = treeFolder;
                var path = new StringBuilder(name);
                if (schema != null)
                {
                    path.Insert(0, schema + ".");
                }

                while (currentTreeFolder != null)
                {
                    if (currentTreeFolder.Name == "Server" || (currentTreeFolder.Name == "Databases" && oeSession.Root.NodeType == "Database"))
                    {
                        var serverRoot = oeSession.Root;
                        if (oeSession.Root.NodeType == "Database")
                        {
                            serverRoot = oeSession.Root.Parent;
                            path.Insert(0, oeSession.Root.NodeValue + (path.Length > 0 ? "/" : ""));
                        }

                        path.Insert(0, serverRoot.NodeValue + (path.Length > 0 ? "/" : ""));
                        break;
                    }

                    if (lastFolder == null || currentTreeFolder.ChildFolders.Contains(lastFolder))
                    {
                        path.Insert(0, currentTreeFolder.LocLabel + "/");
                        lastFolder = currentTreeFolder;
                        currentTreeFolder = currentTreeFolder.ParentFolder;
                    }
                    else
                    {
                        if (currentTreeFolder.ContainedType == "Database")
                        {
                            path.Insert(0, databaseName + "/");
                        }
                        else if (clonedParentNames.Count > 0)
                        {
                            var parentName = clonedParentNames.Last();
                            clonedParentNames.RemoveAt(clonedParentNames.Count - 1);
                            path.Insert(0, parentName + "/");
                        }
                        else
                        {
                            path = null;
                            break;
                        }

                        lastFolder = null;
                    }
                }

                if (path != null)
                {
                    outputPaths.Add(path.ToString());
                }
            }

            var nodes = new List<TreeNode>();
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

        private void LoadTreeStructure()
        {
            var assembly = typeof(ObjectExplorerService).Assembly;
            var resource = assembly.GetManifestResourceStream("Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel.TreeNodeDefinition.xml");
            XmlDocument doc = new XmlDocument();
            using (StreamReader reader = new StreamReader(resource))
            {
                doc.LoadXml(reader.ReadToEnd());
            }
            XmlNodeList nodeList = doc.SelectNodes("/ServerExplorerTree/Node[@Name = 'Server']");
            XmlElement itemAsElement = nodeList[0] as XmlElement;
            GenerateTreeFolders(null, itemAsElement, doc, false);
        }

        private void GenerateTreeFolders(TreeFolder parent, XmlElement parentElement, XmlDocument doc, bool isObjectChild)
        {
            List<TreeFolder> objectFolders = null;
            var name = parentElement.GetAttribute("Name");
            var containedType = parentElement.GetAttribute("TreeNode");
            if (containedType != String.Empty)
            {
                containedType = containedType.Replace("TreeNode", "");
                objectFolders = new List<TreeFolder>();
            }
            else
            {
                containedType = parentElement.GetAttribute("NodeType");
                if (containedType == String.Empty && name == "Server")
                {
                    containedType = "Server";
                }
            }

            var folder = new TreeFolder
            {
                Name = name,
                ContainedType = containedType,
                ChildFolders = new List<TreeFolder>(),
                ParentFolder = parent,
                ObjectFolders = objectFolders,
                LocLabel = SR.Keys.GetString(parentElement.GetAttribute("LocLabel").Remove(0, 3))
            };
            if (parent != null)
            {
                if (isObjectChild)
                {
                    parent.ObjectFolders.Add(folder);
                }
                else
                {
                    parent.ChildFolders.Add(folder);
                }
            }

            if (containedType != String.Empty)
            {
                var currentFolders = typeMap.GetValueOrDefault(containedType);
                if (currentFolders == null)
                {
                    currentFolders = new HashSet<TreeFolder>();
                    typeMap.Add(containedType, currentFolders);
                }
                currentFolders.Add(folder);
            }

            XmlNodeList childNodes = parentElement.GetElementsByTagName("Child");
            foreach (var item in childNodes)
            {
                XmlElement itemAsElement = item as XmlElement;
                var typeName = itemAsElement.GetAttribute("Name");
                XmlNodeList childEntryList = doc.SelectNodes("/ServerExplorerTree/Node[@Name = '" + typeName + "']");
                if (childEntryList.Count > 0)
                {
                    GenerateTreeFolders(folder, childEntryList[0] as XmlElement, doc, false);
                }
            }
            
            if (objectFolders != null)
            {
                XmlNodeList objectNodes = doc.SelectNodes("/ServerExplorerTree/Node[@Name = '" + containedType + "']");
                if (objectNodes.Count > 0)
                {
                    XmlNodeList objectChildNodes = (objectNodes[0] as XmlElement).GetElementsByTagName("Child");
                    foreach (var item in objectChildNodes)
                    {
                        XmlElement itemAsElement = item as XmlElement;
                        var typeName = itemAsElement.GetAttribute("Name");
                        XmlNodeList childEntryList = doc.SelectNodes("/ServerExplorerTree/Node[@Name = '" + typeName + "']");
                        if (childEntryList.Count > 0)
                        {
                            GenerateTreeFolders(folder, childEntryList[0] as XmlElement, doc, true);
                        }
                    }
                }
            }
        }

        internal class TreeFolder
        {
            public string Name;
            public string ContainedType;
            public List<TreeFolder> ChildFolders;
            public TreeFolder ParentFolder;
            public List<TreeFolder> ObjectFolders;
            public string LocLabel;
        }

        internal class ObjectExplorerTaskResult
        {
            public bool IsCompleted { get; set; }
            public Exception Exception { get; set; }
        }

        public void Dispose()
        {
            if (bindingQueue != null)
            {
                bindingQueue.Dispose();
            }
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

                return session;
            }
            
        }
    }

}
