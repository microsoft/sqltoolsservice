//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.Extensibility;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel;
using Microsoft.Kusto.ServiceLayer.UnitTests.Utility;
using Moq;
using Xunit;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.ObjectExplorer
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
        private ServerConnection serverConnection = null;

        public NodeTests()
        {
            defaultServerInfo = TestObjects.GetTestServerInfo();
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
                ConnectionSummary = defaultConnectionDetails != null ? ((IConnectionSummary)defaultConnectionDetails).Clone(): null,
                OwnerUri = defaultOwnerUri
            };

            // TODO can all tests use the standard service provider?
            ServiceProvider = ExtensionServiceProvider.CreateDefaultServiceProvider();
        }
        
        [Fact]
        public void ServerNodeConstructorValidatesFields()
        {
            var mockDataSource = new Mock<IDataSource>();
            Assert.Throws<ArgumentNullException>(() => new ServerNode(null, ServiceProvider, serverConnection, mockDataSource.Object, new DatabaseMetadata()));
            Assert.Throws<ArgumentNullException>(() => new ServerNode(defaultConnParams, null, serverConnection, mockDataSource.Object, new DatabaseMetadata()));
        }

        [Fact]
        public void ServerNodeConstructorShouldSetValuesCorrectly()
        {
            var mockDataSource = new Mock<IDataSource>();
            // Given a server node with valid inputs
            ServerNode node = new ServerNode(defaultConnParams, ServiceProvider, serverConnection, mockDataSource.Object, new DatabaseMetadata());
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
            var mockDataSource = new Mock<IDataSource>();
            
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
            string label = new ServerNode(connParams, ServiceProvider, serverConnection, mockDataSource.Object, new DatabaseMetadata()).Label;
            // Then only server name and version shown
            string expectedLabel = defaultConnectionDetails.ServerName + " (SQL Server " + defaultServerInfo.ServerVersion + ")";
            Assert.Equal(expectedLabel, label);
        }

        [Fact]
        public void ServerNodeConstructorShouldShowDbNameForCloud()
        {
            var mockDataSource = new Mock<IDataSource>();
            defaultServerInfo.IsCloud = true;

            // Given a server node for a cloud DB, with master name
            ServerNode node = new ServerNode(defaultConnParams, ServiceProvider, serverConnection, mockDataSource.Object, new DatabaseMetadata());
            // Then expect label to not include db name
            string expectedLabel = defaultConnectionDetails.ServerName + " (SQL Server " + defaultServerInfo.ServerVersion + " - "
                + defaultConnectionDetails.UserName + ")";
            Assert.Equal(expectedLabel, node.Label);

            // But given a server node for a cloud DB that's not master
            defaultConnectionDetails.DatabaseName = "NotMaster";
            defaultConnParams.ConnectionSummary.DatabaseName = defaultConnectionDetails.DatabaseName;
            node = new ServerNode(defaultConnParams, ServiceProvider, serverConnection, mockDataSource.Object, new DatabaseMetadata());

            // Then expect label to include db name 
            expectedLabel = defaultConnectionDetails.ServerName + " (SQL Server " + defaultServerInfo.ServerVersion + " - "
                + defaultConnectionDetails.UserName + ", " + defaultConnectionDetails.DatabaseName + ")";
            Assert.Equal(expectedLabel, node.Label);
        }

        [Fact]
        public void ToNodeInfoIncludeAllFields()
        {
            var mockDataSource = new Mock<IDataSource>();
            
            // Given a server connection
            ServerNode node = new ServerNode(defaultConnParams, ServiceProvider, serverConnection, mockDataSource.Object, new DatabaseMetadata());
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
            TreeNode parent = new TreeNode {NodeValue = "parent"};
            TreeNode child = new TreeNode {NodeValue = "child"};
            Assert.Null(child.Parent);
            parent.AddChild(child);
            Assert.Equal(parent, child.Parent);
        }

        [Fact]
        public void GetChildrenShouldReturnReadonlyList()
        {
            TreeNode node = new TreeNode {NodeValue = "parent"};
            IList<TreeNode> children = node.GetChildren();
            Assert.Throws<NotSupportedException>(() => children.Add(new TreeNode{NodeValue = "child"}));
        }

        [Fact]
        public void GetChildrenShouldReturnAddedNodesInOrder()
        {
            TreeNode parent = new TreeNode{NodeValue = "parent"};
            TreeNode[] expectedKids = {new TreeNode {NodeValue = "1"}, new TreeNode {NodeValue = "2"}};
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

        [Fact]
        public void MultiLevelTreeShouldFormatPath()
        {
            TreeNode root = new TreeNode{NodeValue = "root"};
            Assert.Equal("root" , root.GetNodePath());

            TreeNode level1Child1 = new TreeNode{NodeValue = "L1C1 (with extra info)"};
            level1Child1.NodePathName = "L1C1";
            TreeNode level1Child2 = new TreeNode {NodeValue = "L1C2"};
            root.AddChild(level1Child1);
            root.AddChild(level1Child2);
            Assert.Equal("root/L1C1" , level1Child1.GetNodePath());
            Assert.Equal("root/L1C2", level1Child2.GetNodePath());

            TreeNode level2Child1 = new TreeNode {NodeValue = "L2C2"};
            level1Child1.AddChild(level2Child1);
            Assert.Equal("root/L1C1/L2C2", level2Child1.GetNodePath());
        }
    }
}
