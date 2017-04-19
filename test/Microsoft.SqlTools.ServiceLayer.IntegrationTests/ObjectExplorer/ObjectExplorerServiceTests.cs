//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
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
            string uri = "CreateSessionAndExpandServer";
            string databaseName = null;
            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName, query, uri))
            {
                var session = await CreateSession(null, uri);
                await CreateSessionAndServerNode(testDb.DatabaseName, session);
                CancelConnection(uri);
            }
        }

        [Fact]
        public async void CreateSessionAndExpandOnTheDatabaseShouldReturnDatabaseObjects()
        {
            var query = "";
            string uri = "CreateSessionAndExpandDatabase";
            string databaseName = null;
            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName, query, uri))
            {
                var session = await CreateSession(testDb.DatabaseName, uri);
                CreateSessionAndDatabaseNode(testDb.DatabaseName, session);
                CancelConnection(uri);
            }
        }

        private async Task<ObjectExplorerSession> CreateSession(string databaseName, string uri)
        {
            ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem, databaseName);
            ConnectionDetails details = connectParams.Connection;

            return await _service.DoCreateSession(details, uri);
        }

        private async Task<NodeInfo> CreateSessionAndServerNode(string databaseName, ObjectExplorerSession session)
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

        private void CreateSessionAndDatabaseNode(string databaseName, ObjectExplorerSession session)
        {
            Assert.NotNull(session);
            Assert.NotNull(session.Root);
            NodeInfo nodeInfo = session.Root.ToNodeInfo();
            Assert.Equal(nodeInfo.IsLeaf, false);
            Assert.Equal(nodeInfo.NodeType, NodeTypes.DatabaseInstance.ToString());
            Assert.True(nodeInfo.Label.Contains(databaseName));
            var children = session.Root.Expand();

            //All server children should be folder nodes
            foreach (var item in children)
            {
                Assert.Equal(item.NodeType, "Folder");
            }

            var tablesRoot = children.FirstOrDefault(x => x.NodeTypeId == NodeTypes.Tables);
            Assert.NotNull(tablesRoot);
        }

        private void CancelConnection(string uri)
        {
            //ConnectionService.Instance.CancelConnect(new CancelConnectParams
            //{
            //    OwnerUri = uri,
            //    Type = ConnectionType.Default
            //});
        }

        private async Task ExpandTree(NodeInfo node, ObjectExplorerSession session)
        {
            if(node != null && !node.IsLeaf)
            {
                var children = await _service.ExpandNode(session, node.NodePath);
                Assert.NotNull(children);
                if(children.Count() == 0 && !node.NodePath.Contains("System") &&
                    !node.NodePath.Contains("FileTables") && !node.NodePath.Contains("External Tables"))
                {
                    var labaleToUpper = node.Label.ToUpper();
                    if (labaleToUpper.Contains("TABLE") || labaleToUpper.Contains("StoredProcedure") 
                        || labaleToUpper.Contains("VIEW"))
                    {
                        //TOOD: Add a better validation. For now at least check tables not to be empty 
                        //Assert.True(false, "The list of tables, procedure and views cannot be empty");
                    }
                }
                foreach (var child in children)
                {
                    //Console.WriteLine(child.Label);
                    await ExpandTree(child, session);
                }
            }
        }

        [Fact]
        public async void VerifyAdventureWorksDatabaseObjects()
        {
            var query = Scripts.AdventureWorksScript;
            string uri = "VerifyAdventureWorksDatabaseObjects";
            string databaseName = null;

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName, query, uri))
            {
                var session = await CreateSession(testDb.DatabaseName, uri);
                var databaseNodeInfo = await CreateSessionAndServerNode(testDb.DatabaseName, session);
                await ExpandTree(databaseNodeInfo, session);
                CancelConnection(uri);
            }
        }

       // [Fact]
        public async void VerifySql2016Objects()
        {
            var query = LoadScript("Sql_2016_Additions.sql");
            string uri = "VerifySql2016Objects";
            string databaseName = null;

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName, query, uri))
            {
                var session = await CreateSession(testDb.DatabaseName, uri);
                var databaseNodeInfo = await CreateSessionAndServerNode(testDb.DatabaseName, session);
                await ExpandTree(databaseNodeInfo, session);
                CancelConnection(uri);
            }
        }

       // [Fact]
        public async void VerifySqlObjects()
        {
            var query = LoadScript("Sql_Additions.sql");
            string uri = "VerifySqlObjects";
            string databaseName = null;

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName, query, uri))
            {
                var session = await CreateSession(testDb.DatabaseName, uri);
                var databaseNodeInfo = await CreateSessionAndServerNode(testDb.DatabaseName, session);
                await ExpandTree(databaseNodeInfo, session);
                CancelConnection(uri);
            }
        }

       // [Fact]
        public async void VerifyFileTableTest()
        {
            var query = LoadScript("FileTableTest.sql");
            string uri = "VerifyFileTableTest";
            string databaseName = null;

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName, query, uri))
            {
                var session = await CreateSession(testDb.DatabaseName, uri);
                var databaseNodeInfo = await CreateSessionAndServerNode(testDb.DatabaseName, session);
                await ExpandTree(databaseNodeInfo, session);
                CancelConnection(uri);
            }
        }

        //[Fact]
        public async void VerifyColumnstoreindexSql16()
        {
            var query = LoadScript("ColumnstoreindexSql16.sql");
            string uri = "VerifyColumnstoreindexSql16";
            string databaseName = null;

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName, query, uri))
            {
                var session = await CreateSession(testDb.DatabaseName, uri);
                var databaseNodeInfo = await CreateSessionAndServerNode(testDb.DatabaseName, session);
                await ExpandTree(databaseNodeInfo, session);
                CancelConnection(uri);
            }
        }

        private static string TestLocationDirectory
        {
            get
            {
                return Path.Combine(RunEnvironmentInfo.GetTestDataLocation(), "ObjectExplorer");
            }
        }

        public DirectoryInfo InputFileDirectory
        {
            get
            {
                string d = Path.Combine(TestLocationDirectory, "TestScripts");
                return new DirectoryInfo(d);
            }
        }

        public FileInfo GetInputFile(string fileName)
        {
            return new FileInfo(Path.Combine(InputFileDirectory.FullName, fileName));
        }

        private string LoadScript(string fileName)
        {
            FileInfo inputFile = GetInputFile(fileName);
            return TestUtilities.ReadTextAndNormalizeLineEndings(inputFile.FullName);
        }
    }
}
