//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Xunit;
using static Microsoft.SqlTools.ServiceLayer.ObjectExplorer.ObjectExplorerService;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectExplorer
{
    public class ObjectExplorerServiceTests
    {
        private ObjectExplorerService _service = TestServiceProvider.Instance.ObjectExplorerService;

        [Fact]
        public async void CreateSessionAndExpandOnTheServerShouldReturnTheDatabases()
        {
            var query = "";
            string uri = "DatabaseChangesAffectAllConnections";
            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, query))
            {
                var session = await CreateSession(testDb.DatabaseName, uri);
                await CreateSessionAndDatabaseNode(testDb.DatabaseName, session);
            }
        }

        private async Task<ObjectExplorerSession> CreateSession(string databaseName, string uri)
        {
            var result = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
            ConnectionInfo connectionInfo = result.ConnectionInfo;
            ConnectionDetails details = connectionInfo.ConnectionDetails;

            return await _service.DoCreateSession(details, uri);
        }

        private async Task<NodeInfo> CreateSessionAndDatabaseNode(string databaseName, ObjectExplorerSession session)
        {
            Assert.NotNull(session);
            Assert.NotNull(session.Root);
            NodeInfo nodeInfo = session.Root.ToNodeInfo();
            Assert.Equal(nodeInfo.IsLeaf, false);
            Assert.Equal(nodeInfo.NodeType, NodeTypes.ServerInstance.ToString());
            var children = session.Root.Expand();

            //All server children should be folder nodes
            foreach (var item in children)
            {
                Assert.Equal(item.NodeType, "Folder");
            }

            var databasesRoot = children.FirstOrDefault(x => x.NodeTypeId == NodeTypes.Databases);
            var databasesChildren = await _service.ExpandNode(session, databasesRoot.GetNodePath());
            var databases = databasesChildren.Where(x => x.NodeType == NodeTypes.DatabaseInstance.ToString());

            //Verify the test databases is in the list
            Assert.NotNull(databases);
            var databaseNode = databases.FirstOrDefault(d => d.Label == databaseName);
            Assert.NotNull(databaseNode);
            return databaseNode;
        }

        private async Task ExpandTree(NodeInfo node, ObjectExplorerSession session)
        {
            if(node != null && !node.IsLeaf)
            {
                var children = await _service.ExpandNode(session, node.NodePath);
                Assert.NotNull(children);
                foreach (var child in children)
                {
                    Console.WriteLine(child.Label);
                    await ExpandTree(child, session);
                }
            }
        }

        [Fact]
        public async void VerifyAdventureWorksDatabaseObjects()
        {
            var query = Scripts.AdventureWorksScript;
            string uri = "VerifyAdventureWorksDatabaseObjects";
            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, query))
            {
                var session = await CreateSession(testDb.DatabaseName, uri);
                var databaseNodeInfo = await CreateSessionAndDatabaseNode(testDb.DatabaseName, session);
                await ExpandTree(databaseNodeInfo, session);
            }
        }
    }
}
