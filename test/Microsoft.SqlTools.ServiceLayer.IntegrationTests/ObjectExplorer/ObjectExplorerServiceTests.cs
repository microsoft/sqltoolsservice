//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
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
        [Fact]
        public async void CreateSessionAndExpandOnTheServerShouldReturnTheDatabases()
        {
            var query = "";
            string uri = "DatabaseChangesAffectAllConnections";
            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, query))
            {
                // If we make a connection to a live database 
                ObjectExplorerService service = TestServiceProvider.Instance.ObjectExplorerService;
                var result = LiveConnectionHelper.InitLiveConnectionInfo(testDb.DatabaseName);
                ConnectionInfo connectionInfo = result.ConnectionInfo;
                ConnectionDetails details = connectionInfo.ConnectionDetails;

                ObjectExplorerSession session = await service.DoCreateSession(details, uri);
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
                var databasesChildren = await service.ExpandNode(session, databasesRoot.GetNodePath());
                var databases = databasesChildren.Where(x => x.NodeType == NodeTypes.DatabaseInstance.ToString());
                
                //Verify the test databases is in the list
                Assert.NotNull(databases);
                var databaseNode = databases.FirstOrDefault(d => d.Label == testDb.DatabaseName);
                Assert.NotNull(databaseNode);
            }
        }
    }
}
