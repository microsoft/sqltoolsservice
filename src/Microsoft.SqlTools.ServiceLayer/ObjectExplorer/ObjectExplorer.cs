//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer
{
    public class ObjectExplorer
    {
        private ConcurrentDictionary<string, ObjectExplorerSession> sessionMap;

        public ObjectExplorer()
        {
            sessionMap = new ConcurrentDictionary<string, ObjectExplorerSession>();
        }

        public ObjectExplorerSession CreateSession(ServerConnection connection, ObjectExplorerServerInfo serverInfo, ObjectExplorerSessionOptions options, string? SessionId = null)
        {
            ServerNode rootNode = new ServerNode(serverInfo, connection);
            var session = ObjectExplorerSession.Create(connection, rootNode, serverInfo, serverInfo.isDefaultOrSystemDatabase, options);

            var sessionID = SessionId == null ?  Guid.NewGuid().ToString() : SessionId;
            sessionMap.AddOrUpdate(sessionID, session, (key, oldSession) => session);
            session.SessionID = sessionID;
            return session;
        }


        public class ObjectExplorerSession
        {
            public string SessionID { get; set; }
            public ServerConnection Connection { get; set; }
            public TreeNode Root { get; private set; }
            public ObjectExplorerServerInfo ServerInfo { get; set; }
            public ObjectExplorerSessionOptions Options { get; set; }

            public ObjectExplorerSession(ServerConnection connection, TreeNode root, ObjectExplorerServerInfo serverInfo, ObjectExplorerSessionOptions options)
            {
                Validate.IsNotNull("root", root);
                Connection = connection;
                Root = root;
                ServerInfo = serverInfo;
                Options = options;
            }

            public static ObjectExplorerSession Create(ServerConnection connection, TreeNode root, ObjectExplorerServerInfo serverInfo, bool isDefaultOrSystemDatabase, ObjectExplorerSessionOptions options)
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

    public class ObjectExplorerServerInfo
    {
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string ServerVersion { get; set; }
        public int EngineEditionId { get; set; }
        public bool IsCloud { get; set; }
        public bool isDefaultOrSystemDatabase { get; set;}
    }

    public class ObjectExplorerSessionOptions
    {
        public bool EnableGroupBySchema { get; set; } = false;

        public int SessionOperationTimeout { get; set; } = 60;
    }
}