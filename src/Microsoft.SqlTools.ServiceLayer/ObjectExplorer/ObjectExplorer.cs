//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer
{
    public class ObjectExplorer
    {
        private class ObjectExplorerSession
        {
            public ServerConnection Connection { get; set; }
            public TreeNode Root { get; private set; }
            public ObjectExplorerServerInfo ServerInfo { get; set; }

            public ObjectExplorerSession(ServerConnection connection, TreeNode root, ObjectExplorerServerInfo serverInfo)
            {
                Validate.IsNotNull("root", root);
                Connection = connection;
                Root = root;
                ServerInfo = serverInfo;
            }

            public static ObjectExplorerSession Create(ServerConnection connection, TreeNode root, ObjectExplorerServerInfo serverInfo, bool isDefaultOrSystemDatabase)
            {
                ServerNode rootNode = new ServerNode(serverInfo, connection);
                var session = new ObjectExplorerSession(connection, root, serverInfo);
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
    }

    public class ObjectExplorerOptions
    {
        public bool EnableGroupBySchema { get; set; }
    }
}