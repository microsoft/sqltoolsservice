//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer
{
    public class SimpleObjectExplorer
    {
        private ConcurrentDictionary<string, ObjectExplorerSession> sessionMap;

        public SimpleObjectExplorer()
        {
            sessionMap = new ConcurrentDictionary<string, ObjectExplorerSession>();
        }

        /// <summary>
        /// Creates a session for the given connection string and server info
        /// </summary>
        /// <param name="connectionString">Connection string for the server to create OE session</param>
        /// <param name="accessToken">Access token for AAD based connections</param>
        /// <param name="serverInfo">Server info for the OE server. This info is used to create the label for the root node and setting the database context for the OE</param>
        /// <param name="options">Flags to set and change oe options</param>
        /// <param name="sessionIdOverride">Override sessionID. By default a guid is generated to be used as session id</param>
        /// <returns></returns>
        public ObjectExplorerSession CreateSession(string connectionString, SecurityToken? accessToken, SimpleObjectExplorerServerInfo serverInfo, SimpleObjectExplorerOptions options, string? sessionIdOverride = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                ServerConnection connection;
                if (accessToken != null)
                {
                    connection = new ServerConnection(conn, accessToken as IRenewableToken);
                }
                else
                {
                    connection = new ServerConnection(conn);
                }
                ServerNode rootNode = new ServerNode(serverInfo, connection);
                var session = ObjectExplorerSession.Create(connection, rootNode, serverInfo, serverInfo.isDefaultOrSystemDatabase, options);

                if(sessionIdOverride != null)
                {
                    session.SessionID = sessionIdOverride;
                }
                sessionMap.AddOrUpdate(session.SessionID, session, (key, oldSession) => session);
                return session;
            }
        }

        /// <summary>
        /// Returns the session for the given session id
        /// </summary>
        /// <param name="sessionId">session id for the OE session</param>
        /// <returns>OESession</returns>
        public ObjectExplorerSession GetSession(string sessionId)
        {
            Validate.IsNotNull(nameof(sessionId), sessionId);

            if (!sessionMap.TryGetValue(sessionId, out ObjectExplorerSession? session))
            {
                throw new Exception("Session not found");
            }

            return session;
        }

        /// <summary>
        /// Closes the session for the given session id
        /// </summary>
        /// <param name="sessionId">closes the session and disposes off session connection</param>
        public void CloseSession(string sessionId)
        {
            Validate.IsNotNull(nameof(sessionId), sessionId);

            if (!sessionMap.TryRemove(sessionId, out ObjectExplorerSession? session))
            {
                throw new Exception("Session not found");
            }
            if (session.Connection.IsOpen)
            {
                session.Connection.Disconnect();
            }
        }

        /// <summary>
        /// Returns a list of child nodes for the given node path
        /// </summary>
        /// <param name="sessionId">Session id for the OE session</param>
        /// <param name="NodePath">Node path for which child nodes are requested</param>
        /// <param name="accessToken">Access token for AAD based connections</param>
        /// <param name="filters">Filters to be applied on the child nodes</param>
        /// <returns>Array of TreeNodes</returns>
        public TreeNode[] Expand(string sessionId, string NodePath, SecurityToken? accessToken = null, INodeFilter[]? filters = null)
        {

            var treeNode = new TreeNode[0];
            Validate.IsNotNull(nameof(sessionId), sessionId);
            Validate.IsNotNull(nameof(NodePath), NodePath);

            if (!sessionMap.TryGetValue(sessionId, out ObjectExplorerSession? session))
            {
                throw new Exception("Session not found");
            }

            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                TreeNode? node = session.Root.FindNodeByPath(NodePath);
                if (node == null)
                {
                    // Return empty array if node is not found
                    return new TreeNode[0];
                }

                if (Monitor.TryEnter(node.BuildingMetadataLock, session.Options.OperationTimeout))
                {
                    try
                    {
                        var token = accessToken == null ? null : accessToken.Token;
                        var task = Task.Run(() => node.Expand(timeoutCancellationTokenSource.Token, token, filters));

                        if (task.Wait(TimeSpan.FromSeconds(session.Options.OperationTimeout)))
                        {
                            if (timeoutCancellationTokenSource.IsCancellationRequested)
                            {
                                throw new TimeoutException("The operation has timed out.");
                            }


                            return task.Result.ToArray();
                        }
                        else
                        {
                            throw new TimeoutException("The operation has timed out.");
                        }
                    }
                    finally
                    {
                        if (session.ServerInfo.DatabaseName != null && session.Connection.SqlConnectionObject.Database != session.ServerInfo.DatabaseName)
                        {
                            session.Connection.SqlConnectionObject.ChangeDatabase(session.ServerInfo.DatabaseName);
                        }
                        Monitor.Exit(node.BuildingMetadataLock);
                    }
                }
                else
                {
                    throw new TimeoutException("The operation has timed out. Could not acquire the lock to build metadata for the node.");
                }

            }
        }

        /// <summary>
        /// Returns the node for the given node path
        /// </summary>
        /// <param name="sessionId">Session id for the OE session</param>
        /// <param name="NodePath">Node path to be searched</param>
        /// <returns></returns>
        public TreeNode? FindNodes(string sessionId, string NodePath)
        {
            Validate.IsNotNull(nameof(sessionId), sessionId);
            Validate.IsNotNull(nameof(NodePath), NodePath);

            if (!sessionMap.TryGetValue(sessionId, out ObjectExplorerSession? session))
            {
                throw new Exception("Session not found");
            }

            return session.Root.FindNodeByPath(NodePath); ;
        }

        public class ObjectExplorerSession
        {
            public string SessionID { get; set; }
            public ServerConnection Connection { get; set; }
            public TreeNode Root { get; private set; }
            public SimpleObjectExplorerServerInfo ServerInfo { get; set; }
            public SimpleObjectExplorerOptions Options { get; set; }

            public ObjectExplorerSession(ServerConnection connection, TreeNode root, SimpleObjectExplorerServerInfo serverInfo, SimpleObjectExplorerOptions options, SecurityToken? accessToken = null)
            {
                Validate.IsNotNull("root", root);
                Connection = connection;
                Root = root;
                ServerInfo = serverInfo;
                Options = options;
                SessionID = Guid.NewGuid().ToString();
            }

            public static ObjectExplorerSession Create(ServerConnection connection, TreeNode root, SimpleObjectExplorerServerInfo serverInfo, bool isDefaultOrSystemDatabase, SimpleObjectExplorerOptions options)
            {
                ServerNode rootNode = new ServerNode(serverInfo, connection);
                var session = new ObjectExplorerSession(connection, root, serverInfo, options);
                if (!isDefaultOrSystemDatabase)
                {
                    // Assuming the databases are in a folder under server node
                    DatabaseTreeNode databaseNode = new DatabaseTreeNode(rootNode, serverInfo.DatabaseName);
                    session.Root = databaseNode;
                }
                return session;
            }
        }
    }

    public class SimpleObjectExplorerServerInfo
    {
        /// <summary>
        /// Server name for the OE session
        /// </summary>
        public string? ServerName { get; set; }
        /// <summary>
        /// Database name for the OE session
        /// </summary>
        public string? DatabaseName { get; set; }
        /// <summary>
        /// User name for the OE session
        /// </summary>
        public string? UserName { get; set; }
        /// <summary>
        /// SQL Server version for the OE session
        /// </summary>
        public string? ServerVersion { get; set; }
        /// <summary>
        /// SQL Server edition for the OE session
        /// </summary>
        public int EngineEditionId { get; set; }
        /// <summary>
        /// Checks if the OE session is for Azure SQL DB
        /// </summary>
        public bool IsCloud { get; set; }
        /// <summary>
        /// Indicates if the OE session is for default or system database
        /// </summary>
        public bool isDefaultOrSystemDatabase { get; set; }
    }

    public class SimpleObjectExplorerOptions
    {
        /// <summary>
        /// Enables schema level grouping for child nodes. Default is false
        /// </summary>
        public bool EnableGroupBySchema { get; set; } = false;

        /// <summary>
        /// Timeout for OE session operations in seconds. Default is 60 seconds
        /// </summary>
        public int OperationTimeout { get; set; } = 60;
    }
}