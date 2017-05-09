//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{
    /// <summary>
    /// Tests covering basic operation of Node based classes
    /// </summary>
    public class NodeTests : ObjectExplorerTestBase
    {
        private string defaultOwnerUri = "objectexplorer://myserver";
        private ServerInfo defaultServerInfo;
        private ConnectionDetails defaultConnectionDetails;
        private ConnectionCompleteParams defaultConnParams;
        private string fakeConnectionString = "Data Source=server;Initial Catalog=database;Integrated Security=False;User Id=user";

        public NodeTests()
        {
            defaultServerInfo = TestObjects.GetTestServerInfo();

            defaultConnectionDetails = new ConnectionDetails()
            {
                DatabaseName = "master",
                ServerName = "localhost",
                UserName = "serverAdmin",
                Password = "..."
            };
            defaultConnParams = new ConnectionCompleteParams()
            {
                ServerInfo = defaultServerInfo,
                ConnectionSummary = defaultConnectionDetails,
                OwnerUri = defaultOwnerUri
            };

            // TODO can all tests use the standard service provider?
            ServiceProvider = ExtensionServiceProvider.CreateDefaultServiceProvider();
        }
        
        [Fact]
        public void ServerNodeConstructorValidatesFields()
        {
            Assert.Throws<ArgumentNullException>(() => new ServerNode(null, ServiceProvider));
            Assert.Throws<ArgumentNullException>(() => new ServerNode(defaultConnParams, null));
        }

        [Fact]
        public void ServerNodeConstructorShouldSetValuesCorrectly()
        {
            // Given a server node with valid inputs
            ServerNode node = new ServerNode(defaultConnParams, ServiceProvider);
            // Then expect all fields set correctly
            Assert.False(node.IsAlwaysLeaf, "Server node should never be a leaf");
            Assert.Equal(defaultConnectionDetails.ServerName, node.NodeValue);

            string expectedLabel = defaultConnectionDetails.ServerName + " (SQL Server " + defaultServerInfo.ServerVersion + " - "
                + defaultConnectionDetails.UserName + ")";
            Assert.Equal(expectedLabel, node.Label);

            Assert.Equal(NodeTypes.Server.ToString(), node.NodeType);
            string[] nodePath = node.GetNodePath().Split(TreeNode.PathPartSeperator);
            Assert.Equal(1, nodePath.Length);
            Assert.Equal(defaultConnectionDetails.ServerName, nodePath[0]);
        }

        [Fact]
        public void ServerNodeLabelShouldIgnoreUserNameIfEmptyOrNull()
        {
            // Given no username set
            ConnectionSummary integratedAuthSummary = new ConnectionSummary()
            {
                DatabaseName = defaultConnectionDetails.DatabaseName,
                ServerName = defaultConnectionDetails.ServerName,
                UserName = null
            };
            ConnectionCompleteParams connParams = new ConnectionCompleteParams()
            {
                ConnectionSummary = integratedAuthSummary,
                ServerInfo = defaultServerInfo,
                OwnerUri = defaultOwnerUri
            };
            // When querying label
            string label = new ServerNode(connParams, ServiceProvider).Label;
            // Then only server name and version shown
            string expectedLabel = defaultConnectionDetails.ServerName + " (SQL Server " + defaultServerInfo.ServerVersion + ")";
            Assert.Equal(expectedLabel, label);
        }

        [Fact]
        public void ServerNodeConstructorShouldShowDbNameForCloud()
        {
            defaultServerInfo.IsCloud = true;

            // Given a server node for a cloud DB, with master name
            ServerNode node = new ServerNode(defaultConnParams, ServiceProvider);
            // Then expect label to not include db name
            string expectedLabel = defaultConnectionDetails.ServerName + " (SQL Server " + defaultServerInfo.ServerVersion + " - "
                + defaultConnectionDetails.UserName + ")";
            Assert.Equal(expectedLabel, node.Label);

            // But given a server node for a cloud DB that's not master
            defaultConnectionDetails.DatabaseName = "NotMaster";
            node = new ServerNode(defaultConnParams, ServiceProvider);

            // Then expect label to include db name 
            expectedLabel = defaultConnectionDetails.ServerName + " (SQL Server " + defaultServerInfo.ServerVersion + " - "
                + defaultConnectionDetails.UserName + ", " + defaultConnectionDetails.DatabaseName + ")";
            Assert.Equal(expectedLabel, node.Label);
        }

        [Fact]
        public void ToNodeInfoIncludeAllFields()
        {
            // Given a server connection
            ServerNode node = new ServerNode(defaultConnParams, ServiceProvider);
            // When converting to NodeInfo
            NodeInfo info = node.ToNodeInfo();
            // Then all fields should match
            Assert.Equal(node.IsAlwaysLeaf, info.IsLeaf);
            Assert.Equal(node.Label, info.Label);
            Assert.Equal(node.NodeType, info.NodeType);
            string[] nodePath = node.GetNodePath().Split(TreeNode.PathPartSeperator);
            string[] nodeInfoPathParts = info.NodePath.Split(TreeNode.PathPartSeperator);
            Assert.Equal(nodePath.Length, nodeInfoPathParts.Length);
            for (int i = 0; i < nodePath.Length; i++)
            {
                Assert.Equal(nodePath[i], nodeInfoPathParts[i]);
            }
        }

        [Fact]
        public void AddChildShouldSetParent()
        {
            TreeNode parent = new TreeNode("parent");
            TreeNode child = new TreeNode("child");
            Assert.Null(child.Parent);
            parent.AddChild(child);
            Assert.Equal(parent, child.Parent);
        }

        [Fact]
        public void GetChildrenShouldReturnReadonlyList()
        {
            TreeNode node = new TreeNode("parent");
            IList<TreeNode> children = node.GetChildren();
            Assert.Throws<NotSupportedException>(() => children.Add(new TreeNode("child")));
        }

        [Fact]
        public void GetChildrenShouldReturnAddedNodesInOrder()
        {
            TreeNode parent = new TreeNode("parent");
            TreeNode[] expectedKids = new TreeNode[] { new TreeNode("1"), new TreeNode("2") };
            foreach (TreeNode child in expectedKids)
            {
                parent.AddChild(child);
            }
            IList<TreeNode> children = parent.GetChildren();
            Assert.Equal(expectedKids.Length, children.Count);
            for (int i = 0; i < expectedKids.Length; i++)
            {
                Assert.Equal(expectedKids[i], children[i]);
            }
        }

        public void MultiLevelTreeShouldFormatPath()
        {
            TreeNode root = new TreeNode("root");
            Assert.Equal("/root" , root.GetNodePath());

            TreeNode level1Child1 = new TreeNode("L1C1");
            TreeNode level1Child2 = new TreeNode("L1C2");
            root.AddChild(level1Child1);
            root.AddChild(level1Child2);
            Assert.Equal("/root/L1C1" , level1Child1.GetNodePath());
            Assert.Equal("/root/L1C2", level1Child2.GetNodePath());

            TreeNode level2Child1 = new TreeNode("L2C2");
            level1Child1.AddChild(level2Child1);
            Assert.Equal("/root/L1C1/L2C2", level2Child1.GetNodePath());
        }
        
        [Fact]
        public void ServerNodeContextShouldIncludeServer()
        {
            // given a successful Server creation
            SetupAndRegisterTestConnectionService();
            Server smoServer = new Server(new ServerConnection(new SqlConnection(fakeConnectionString)));
            ServerNode node = SetupServerNodeWithServer(smoServer);

            // When I get the context for a ServerNode
            var context = node.GetContextAs<SmoQueryContext>();

            // Then I expect it to contain the server I created 
            Assert.NotNull(context);
            Assert.Equal(smoServer, context.Server);
            // And the server should be the parent
            Assert.Equal(smoServer, context.Parent);
            Assert.Null(context.Database);
        }

        [Fact]
        public void ServerNodeContextShouldSetErrorMessageIfSqlConnectionIsNull()
        {
            // given a connectionInfo with no SqlConnection to use for queries
            ConnectionService connService = SetupAndRegisterTestConnectionService();
            connService.OwnerToConnectionMap.Remove(defaultOwnerUri);

            Server smoServer = new Server(new ServerConnection(new SqlConnection(fakeConnectionString)));
            ServerNode node = SetupServerNodeWithServer(smoServer);

            // When I get the context for a ServerNode
            var context = node.GetContextAs<SmoQueryContext>();

            // Then I expect it to be in an error state 
            Assert.Null(context);
            Assert.Equal(
                string.Format(CultureInfo.CurrentCulture, SR.ServerNodeConnectionError, defaultConnectionDetails.ServerName), 
                node.ErrorStateMessage);
        }

        [Fact]
        public void ServerNodeContextShouldSetErrorMessageIfConnFailureExceptionThrown()
        {
            // given a connectionInfo with no SqlConnection to use for queries
            SetupAndRegisterTestConnectionService();

            Server smoServer = new Server(new ServerConnection(new SqlConnection(fakeConnectionString)));
            string expectedMsg = "ConnFailed!";
            ServerNode node = SetupServerNodeWithExceptionCreator(new ConnectionFailureException(expectedMsg));

            // When I get the context for a ServerNode
            var context = node.GetContextAs<SmoQueryContext>();

            // Then I expect it to be in an error state 
            Assert.Null(context);
            Assert.Equal(
                string.Format(CultureInfo.CurrentCulture, SR.TreeNodeError, expectedMsg),
                node.ErrorStateMessage);
        }

        [Fact]
        public void ServerNodeContextShouldSetErrorMessageIfExceptionThrown()
        {
            // given a connectionInfo with no SqlConnection to use for queries
            SetupAndRegisterTestConnectionService();

            Server smoServer = new Server(new ServerConnection(new SqlConnection(fakeConnectionString)));
            string expectedMsg = "Failed!";
            ServerNode node = SetupServerNodeWithExceptionCreator(new Exception(expectedMsg));

            // When I get the context for a ServerNode
            var context = node.GetContextAs<SmoQueryContext>();

            // Then I expect it to be in an error state 
            Assert.Null(context);
            Assert.Equal(
                string.Format(CultureInfo.CurrentCulture, SR.TreeNodeError, expectedMsg),
                node.ErrorStateMessage);
        }

        private ConnectionService SetupAndRegisterTestConnectionService()
        {
            ConnectionService connService = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = new ConnectionInfo(TestObjects.GetTestSqlConnectionFactory(),
                defaultOwnerUri, defaultConnectionDetails);
            connectionInfo.AddConnection("Default", new SqlConnection());

            connService.OwnerToConnectionMap.Add(defaultOwnerUri, connectionInfo);
            ServiceProvider.RegisterSingleService(connService);
            return connService;
        }

        private ServerNode SetupServerNodeWithServer(Server smoServer)
        {
            Mock<SmoServerCreator> creator = new Mock<SmoServerCreator>();
            creator.Setup(c => c.Create(It.IsAny<SqlConnection>()))
                .Returns(() => smoServer);
            ServerNode node = SetupServerNodeWithCreator(creator.Object);
            return node;
        }

        private ServerNode SetupServerNodeWithExceptionCreator(Exception ex)
        {
            Mock<SmoServerCreator> creator = new Mock<SmoServerCreator>();
            creator.Setup(c => c.Create(It.IsAny<SqlConnection>()))
                .Throws(ex);

            ServerNode node = SetupServerNodeWithCreator(creator.Object);
            return node;
        }

        private ServerNode SetupServerNodeWithCreator(SmoServerCreator creator)
        {
            ServerNode node = new ServerNode(defaultConnParams, ServiceProvider);
            node.ServerCreator = creator;
            return node;
        }

        [Fact]
        public void ServerNodeChildrenShouldIncludeFoldersAndDatabases()
        {
            // Given a server with 1 database
            SetupAndRegisterTestConnectionService();
            ServiceProvider.RegisterSingleService(new ObjectExplorerService());

            string dbName = "DB1";
            Mock<NamedSmoObject> smoObjectMock = new Mock<NamedSmoObject>();
            smoObjectMock.SetupGet(s => s.Name).Returns(dbName);

            Mock<SqlDatabaseQuerier> querierMock = new Mock<SqlDatabaseQuerier>();
            querierMock.Setup(q => q.Query(It.IsAny<SmoQueryContext>(), "", false))
                .Returns(smoObjectMock.Object.SingleItemAsEnumerable());

            ServiceProvider.Register<SmoQuerier>(() => new[] { querierMock.Object });

            Server smoServer = new Server(new ServerConnection(new SqlConnection(fakeConnectionString)));
            ServerNode node = SetupServerNodeWithServer(smoServer);

            // When I populate its children
            IList<TreeNode> children = node.Expand();

            // Then I expect it to contain server-level folders 
            Assert.Equal(3, children.Count);
            VerifyTreeNode<FolderNode>(children[0], "Folder", SR.SchemaHierarchy_Databases);
            VerifyTreeNode<FolderNode>(children[1], "Folder", SR.SchemaHierarchy_Security);
            VerifyTreeNode<FolderNode>(children[2], "Folder", SR.SchemaHierarchy_ServerObjects);
            // And the database is contained under it
            TreeNode databases = children[0];
            IList<TreeNode> dbChildren = databases.Expand();
            Assert.Equal(2, dbChildren.Count);
            Assert.Equal(SR.SchemaHierarchy_SystemDatabases, dbChildren[0].NodeValue);

            TreeNode dbNode = dbChildren[1];
            Assert.Equal(dbName, dbNode.NodeValue);
            Assert.Equal(dbName, dbNode.Label);
            Assert.False(dbNode.IsAlwaysLeaf);
            
            // Note: would like to verify Database in the context, but cannot since it's a Sealed class and isn't easily mockable
        }

        private void VerifyTreeNode<T>(TreeNode node, string nodeType, string folderValue)
            where T : TreeNode
        {
            T nodeAsT = node as T;
            Assert.NotNull(nodeAsT);
            Assert.Equal(nodeType, nodeAsT.NodeType);
            Assert.Equal(folderValue, nodeAsT.NodeValue);
        }
    }
}
