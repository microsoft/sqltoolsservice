﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.SqlCore.ObjectExplorer;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.Nodes;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel;

using Moq;

using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{
    /// <summary>
    /// Tests covering basic operation of Node based classes
    /// </summary>
    public class NodeTests : ObjectExplorerTestBase
    {
        private string defaultOwnerUri = "objectexplorer://myserver";
        private ObjectExplorerServerInfo oeServerInfo = null;
        private ConnectionDetails defaultConnectionDetails;
        private ConnectionCompleteParams defaultConnParams;
        private string fakeConnectionString = "Data Source=server;Initial Catalog=database;Integrated Security=False;User Id=user";
        private ServerConnection serverConnection = null;


        [SetUp]
        public void InitNodeTests()
        {
            var defaultServerInfo = TestObjects.GetTestServerInfo();
            serverConnection = new ServerConnection(new SqlConnection(fakeConnectionString));

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
                ConnectionSummary = defaultConnectionDetails != null ? ((IConnectionSummary)defaultConnectionDetails).Clone() : null,
                OwnerUri = defaultOwnerUri
            };

            oeServerInfo = new ObjectExplorerServerInfo()
            {
                ServerName = defaultConnectionDetails.ServerName,
                DatabaseName = defaultConnectionDetails.DatabaseName,
                UserName = defaultConnectionDetails.UserName,
                ServerVersion = defaultServerInfo.ServerVersion,
                EngineEditionId = defaultServerInfo.EngineEditionId,
                IsCloud = defaultServerInfo.IsCloud
            };

            var smoquery = typeof(SqlCore.ObjectExplorer.SmoModel.SmoQuerier).Assembly;
            // TODO can all tests use the standard service provider?
            ServiceProvider = ExtensionServiceProvider.CreateFromAssembliesInDirectory(Path.GetDirectoryName(smoquery.Location),new string[] { Path.GetFileName(smoquery.Location) });
        }

        [Test]
        public void ServerNodeConstructorValidatesFields()
        {
            Assert.Throws<ArgumentNullException>(() => new ServerNode(null, serverConnection, ServiceProvider));
        }

        [Test]
        public void ServerNodeConstructorShouldSetValuesCorrectly()
        {
            // Given a server node with valid inputs
            ServerNode node = new ServerNode(oeServerInfo, serverConnection, ServiceProvider);
            // Then expect all fields set correctly
            Assert.False(node.IsAlwaysLeaf, "Server node should never be a leaf");
            Assert.AreEqual(defaultConnectionDetails.ServerName, node.NodeValue);

            string expectedLabel = defaultConnectionDetails.ServerName + " (SQL Server " + oeServerInfo.ServerVersion + " - "
                + defaultConnectionDetails.UserName + ")";
            Assert.AreEqual(expectedLabel, node.Label);

            Assert.AreEqual(NodeTypes.Server.ToString(), node.NodeType);
            string[] nodePath = node.GetNodePath().Split(TreeNode.PathPartSeperator);
            Assert.AreEqual(1, nodePath.Length);
            Assert.AreEqual(defaultConnectionDetails.ServerName, nodePath[0]);
        }

        [Test]
        public void ServerNodeLabelShouldIgnoreUserNameIfEmptyOrNull()
        {
            // Given no username set
            oeServerInfo.UserName = null;
            // When querying label
            string label = new ServerNode(oeServerInfo, serverConnection, ServiceProvider).Label;
            // Then only server name and version shown
            string expectedLabel = oeServerInfo.ServerName + " (SQL Server " + oeServerInfo.ServerVersion + ")";
            Assert.AreEqual(expectedLabel, label);
        }

        [Test]
        public void ServerNodeConstructorShouldShowDbNameForCloud()
        {
            oeServerInfo.IsCloud = true;

            // Given a server node for a cloud DB, with master name
            ServerNode node = new ServerNode(oeServerInfo, serverConnection);
            // Then expect label to not include db name
            string expectedLabel = oeServerInfo.ServerName + " (SQL Server " + oeServerInfo.ServerVersion + " - "
                + oeServerInfo.UserName + ")";
            Assert.AreEqual(expectedLabel, node.Label);

            // But given a server node for a cloud DB that's not master
            oeServerInfo.DatabaseName = "NotMaster";
            node = new ServerNode(oeServerInfo, serverConnection, ServiceProvider);

            // Then expect label to include db name 
            expectedLabel = oeServerInfo.ServerName + " (SQL Server " + oeServerInfo.ServerVersion + " - "
                + oeServerInfo.UserName + ", " + oeServerInfo.DatabaseName + ")";
            Assert.AreEqual(expectedLabel, node.Label);
        }

        [Test]
        public void NodeInfoConstructorPopulatesAllFieldsFromTreeNode()
        {
            // Given a server connection
            ServerNode node = new ServerNode(oeServerInfo, serverConnection, ServiceProvider);
            // When converting to NodeInfo
            NodeInfo info = new NodeInfo(node);
            // Then all fields should match
            Assert.AreEqual(node.IsAlwaysLeaf, info.IsLeaf);
            Assert.AreEqual(node.Label, info.Label);
            Assert.AreEqual(node.NodeType, info.NodeType);
            string[] nodePath = node.GetNodePath().Split(TreeNode.PathPartSeperator);
            string[] nodeInfoPathParts = info.NodePath.Split(TreeNode.PathPartSeperator);
            Assert.AreEqual(nodePath.Length, nodeInfoPathParts.Length);
            for (int i = 0; i < nodePath.Length; i++)
            {
                Assert.AreEqual(nodePath[i], nodeInfoPathParts[i]);
            }
        }

        [Test]
        public void AddChildShouldSetParent()
        {
            TreeNode parent = new TreeNode("parent");
            TreeNode child = new TreeNode("child");
            Assert.Null(child.Parent);
            parent.AddChild(child);
            Assert.AreEqual(parent, child.Parent);
        }

        [Test]
        public void GetChildrenShouldReturnReadonlyList()
        {
            TreeNode node = new TreeNode("parent");
            IList<TreeNode> children = node.GetChildren();
            Assert.Throws<NotSupportedException>(() => children.Add(new TreeNode("child")));
        }

        [Test]
        public void GetChildrenShouldReturnAddedNodesInOrder()
        {
            TreeNode parent = new TreeNode("parent");
            TreeNode[] expectedKids = new TreeNode[] { new TreeNode("1"), new TreeNode("2") };
            foreach (TreeNode child in expectedKids)
            {
                parent.AddChild(child);
            }
            IList<TreeNode> children = parent.GetChildren();
            Assert.AreEqual(expectedKids.Length, children.Count);
            for (int i = 0; i < expectedKids.Length; i++)
            {
                Assert.AreEqual(expectedKids[i], children[i]);
            }
        }

        [Test]
        public void MultiLevelTreeShouldFormatPath()
        {
            TreeNode root = new TreeNode("root");
            Assert.AreEqual("root", root.GetNodePath());

            TreeNode level1Child1 = new TreeNode("L1C1 (with extra info)");
            level1Child1.NodePathName = "L1C1";
            TreeNode level1Child2 = new TreeNode("L1C2");
            root.AddChild(level1Child1);
            root.AddChild(level1Child2);
            Assert.AreEqual("root/L1C1", level1Child1.GetNodePath());
            Assert.AreEqual("root/L1C2", level1Child2.GetNodePath());

            TreeNode level2Child1 = new TreeNode("L2C2");
            level1Child1.AddChild(level2Child1);
            Assert.AreEqual("root/L1C1/L2C2", level2Child1.GetNodePath());
        }

        [Test]
        public void ServerNodeContextShouldIncludeServer()
        {
            // given a successful Server creation
            Server smoServer = new Server(new ServerConnection(new SqlConnection(fakeConnectionString)));
            ServerNode node = SetupServerNodeWithServer(smoServer);

            // When I get the context for a ServerNode
            var context = node.GetContextAs<SmoQueryContext>();

            // Then I expect it to contain the server I created 
            Assert.NotNull(context);
            Assert.AreEqual(smoServer, context.Server);
            // And the server should be the parent
            Assert.AreEqual(smoServer, context.Parent);
            Assert.Null(context.Database);
        }

        [Test]
        public void ServerNodeContextShouldSetErrorMessageIfSqlConnectionIsNull()
        {
            // given a connectionInfo with no SqlConnection to use for queries

            Server smoServer = null;
            ServerNode node = SetupServerNodeWithServer(smoServer);

            // When I get the context for a ServerNode
            var context = node.GetContextAs<SmoQueryContext>();

            // Then I expect it to be in an error state 
            Assert.Null(context);
        }

        [Test]
        public void ServerNodeContextShouldSetErrorMessageIfConnFailureExceptionThrown()
        {
            // given a connectionInfo with no SqlConnection to use for queries
            Server smoServer = new Server(new ServerConnection(new SqlConnection(fakeConnectionString)));
            string expectedMsg = "ConnFailed!";
            ServerNode node = SetupServerNodeWithExceptionCreator(new ConnectionFailureException(expectedMsg));

            // When I get the context for a ServerNode
            var context = node.GetContextAs<SmoQueryContext>();

            // Then I expect it to be in an error state 
            Assert.Null(context);
            Assert.AreEqual(
                string.Format(CultureInfo.CurrentCulture, SqlTools.SqlCore.SR.TreeNodeError, expectedMsg),
                node.ErrorStateMessage);
        }

        [Test]
        public void ServerNodeContextShouldSetErrorMessageIfExceptionThrown()
        {
            // given a connectionInfo with no SqlConnection to use for queries
            Server smoServer = new Server(new ServerConnection(new SqlConnection(fakeConnectionString)));
            string expectedMsg = "Failed!";
            ServerNode node = SetupServerNodeWithExceptionCreator(new Exception(expectedMsg));

            // When I get the context for a ServerNode
            var context = node.GetContextAs<SmoQueryContext>();

            // Then I expect it to be in an error state 
            Assert.Null(context);
            Assert.AreEqual(
                string.Format(CultureInfo.CurrentCulture, SqlTools.SqlCore.SR.TreeNodeError, expectedMsg),
                node.ErrorStateMessage);
        }

        [Test]
        public void QueryContextShouldNotCallOpenOnAlreadyOpenConnection()
        {
            // given a server connection that will state its connection is open
            Server smoServer = new Server(new ServerConnection(new SqlConnection(fakeConnectionString)));
            Mock<SmoWrapper> wrapper = SetupSmoWrapperForIsOpenTest(smoServer, isOpen: true);

            SmoQueryContext context = new SmoQueryContext(smoServer, ServiceProvider, wrapper.Object);

            // when I access the Server property
            Server actualServer = context.Server;

            // Then I do not expect to have open called
            Assert.NotNull(actualServer);
            wrapper.Verify(c => c.OpenConnection(It.IsAny<Server>()), Times.Never);
        }

        private Mock<SmoWrapper> SetupSmoWrapperForIsOpenTest(Server smoServer, bool isOpen)
        {
            Mock<SmoWrapper> wrapper = new Mock<SmoWrapper>();
            int count = 0;
            wrapper.Setup(c => c.CreateServer(It.IsAny<ServerConnection>()))
                .Returns(() => smoServer);
            wrapper.Setup(c => c.IsConnectionOpen(It.IsAny<Server>()))
                .Returns(() => isOpen);
            wrapper.Setup(c => c.OpenConnection(It.IsAny<Server>()))
                .Callback(() => count++)
                .Verifiable();
            return wrapper;
        }

        [Test]
        public void QueryContextShouldReopenClosedConnectionWhenGettingServer()
        {
            // given a server connection that will state its connection is closed
            Server smoServer = new Server(new ServerConnection(new SqlConnection(fakeConnectionString)));
            Mock<SmoWrapper> wrapper = SetupSmoWrapperForIsOpenTest(smoServer, isOpen: false);

            SmoQueryContext context = new SmoQueryContext(smoServer, ServiceProvider, wrapper.Object);

            // when I access the Server property
            Server actualServer = context.Server;

            // Then I expect to have open called
            Assert.NotNull(actualServer);
            wrapper.Verify(c => c.OpenConnection(It.IsAny<Server>()), Times.Once);
        }

        [Test]
        public void QueryContextShouldReopenClosedConnectionWhenGettingParent()
        {
            // given a server connection that will state its connection is closed
            Server smoServer = new Server(new ServerConnection(new SqlConnection(fakeConnectionString)));
            Mock<SmoWrapper> wrapper = SetupSmoWrapperForIsOpenTest(smoServer, isOpen: false);

            SmoQueryContext context = new SmoQueryContext(smoServer, ServiceProvider, wrapper.Object);
            context.Parent = smoServer;
            // when I access the Parent property
            SmoObjectBase actualParent = context.Parent;

            // Then I expect to have open called
            Assert.NotNull(actualParent);
            wrapper.Verify(c => c.OpenConnection(It.IsAny<Server>()), Times.Once);
        }

        private ConnectionService SetupAndRegisterTestConnectionService()
        {
            ConnectionService connService = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = new ConnectionInfo(TestObjects.GetTestSqlConnectionFactory(),
                defaultOwnerUri, defaultConnectionDetails);
            connectionInfo.AddConnection("Default", new SqlConnection());

            connService.OwnerToConnectionMap.TryAdd(defaultOwnerUri, connectionInfo);
            ServiceProvider.RegisterSingleService(connService);
            return connService;
        }

        private ServerNode SetupServerNodeWithServer(Server smoServer)
        {
            Mock<SmoWrapper> creator = new Mock<SmoWrapper>();
            creator.Setup(c => c.CreateServer(It.IsAny<ServerConnection>()))
                .Returns(() => smoServer);
            creator.Setup(c => c.IsConnectionOpen(It.IsAny<Server>()))
                .Returns(() => true);
            ServerNode node = SetupServerNodeWithCreator(creator.Object);
            return node;
        }

        private ServerNode SetupServerNodeWithExceptionCreator(Exception ex)
        {
            Mock<SmoWrapper> creator = new Mock<SmoWrapper>();
            creator.Setup(c => c.CreateServer(It.IsAny<ServerConnection>()))
                .Throws(ex);
            creator.Setup(c => c.IsConnectionOpen(It.IsAny<Server>()))
                .Returns(() => false);

            ServerNode node = SetupServerNodeWithCreator(creator.Object);
            return node;
        }

        private ServerNode SetupServerNodeWithCreator(SmoWrapper creator)
        {
            ServerNode node = new ServerNode(oeServerInfo, new ServerConnection(new SqlConnection(fakeConnectionString)), ServiceProvider, () => false);
            node.SmoWrapper = creator;
            return node;
        }

        [Test]
        public void ServerNodeChildrenShouldIncludeFoldersAndDatabases()
        {
            // Given a server with 1 database
            SetupAndRegisterTestConnectionService();
            ServiceProvider.RegisterSingleService(new ObjectExplorerService());

            string dbName = "DB1";
            Mock<NamedSmoObject> smoObjectMock = new Mock<NamedSmoObject>();
            smoObjectMock.SetupGet(s => s.Name).Returns(dbName);

            Mock<SqlDatabaseQuerier> querierMock = new Mock<SqlDatabaseQuerier>();
            querierMock.Setup(q => q.Query(It.IsAny<SmoQueryContext>(), It.IsAny<string>(), false, It.IsAny<IEnumerable<string>>()))
                .Returns(smoObjectMock.Object.SingleItemAsEnumerable());

            ServiceProvider.Register<SmoQuerier>(() => new[] { querierMock.Object });

            Server smoServer = new Server(new ServerConnection(new SqlConnection(fakeConnectionString)));
            ServerNode node = SetupServerNodeWithServer(smoServer);

            // When I populate its children
            IList<TreeNode> children = node.Expand(new CancellationToken());

            // Then I expect it to contain server-level folders 
            Assert.AreEqual(3, children.Count);
            VerifyTreeNode<FolderNode>(children[0], "Folder", SqlTools.SqlCore.SR.SchemaHierarchy_Databases);
            VerifyTreeNode<FolderNode>(children[1], "Folder", SqlTools.SqlCore.SR.SchemaHierarchy_Security);
            VerifyTreeNode<FolderNode>(children[2], "Folder", SqlTools.SqlCore.SR.SchemaHierarchy_ServerObjects);
            // And the database is contained under it
            TreeNode databases = children[0];
            IList<TreeNode> dbChildren = databases.Expand(new CancellationToken());
            Assert.AreEqual(2, dbChildren.Count);
            Assert.AreEqual(SqlTools.SqlCore.SR.SchemaHierarchy_SystemDatabases, dbChildren[0].NodeValue);

            TreeNode dbNode = dbChildren[1];
            Assert.AreEqual(dbName, dbNode.NodeValue);
            Assert.AreEqual(dbName, dbNode.Label);
            Assert.False(dbNode.IsAlwaysLeaf);

            // Note: would like to verify Database in the context, but cannot since it's a Sealed class and isn't easily mockable
        }

        private void VerifyTreeNode<T>(TreeNode node, string nodeType, string folderValue)
            where T : TreeNode
        {
            T nodeAsT = node as T;
            Assert.NotNull(nodeAsT);
            Assert.AreEqual(nodeType, nodeAsT.NodeType);
            Assert.AreEqual(folderValue, nodeAsT.NodeValue);
        }
    }
}
