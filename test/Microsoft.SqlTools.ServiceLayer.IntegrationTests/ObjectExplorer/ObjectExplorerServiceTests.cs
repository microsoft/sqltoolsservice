//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
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
        public async void CreateSessionAndExpandOnTheServerShouldReturnServerAsTheRoot()
        {
            var query = "";
            string uri = "CreateSessionAndExpandServer";
            string databaseName = null;
            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName, query, uri))
            {
                var session = await CreateSession(null, uri);
                await ExpandServerNodeAndVerifyDatabaseHierachy(testDb.DatabaseName, session);
                CancelConnection(uri);
            }
        }

        [Fact]
        public async void CreateSessionWithTempdbAndExpandOnTheServerShouldReturnServerAsTheRoot()
        {
            var query = "";
            string uri = "CreateSessionAndExpandServer";
            string databaseName = null;
            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName, query, uri))
            {
                var session = await CreateSession("tempdb", uri);
                await ExpandServerNodeAndVerifyDatabaseHierachy(testDb.DatabaseName, session);
                CancelConnection(uri);
            }
        }

        [Fact]
        public async void CreateSessionAndExpandOnTheDatabaseShouldReturnDatabaseAsTheRoot()
        {
            var query = "";
            string uri = "CreateSessionAndExpandDatabase";
            string databaseName = null;
            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName, query, uri))
            {
                var session = await CreateSession(testDb.DatabaseName, uri);
                ExpandAndVerifyDatabaseNode(testDb.DatabaseName, session);
                CancelConnection(uri);
            }
        }

        private async Task<ObjectExplorerSession> CreateSession(string databaseName, string uri)
        {
            ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem, databaseName);
            ConnectionDetails details = connectParams.Connection;

            return await _service.DoCreateSession(details, uri);
        }

        private async Task<NodeInfo> ExpandServerNodeAndVerifyDatabaseHierachy(string databaseName, ObjectExplorerSession session, bool serverNode = true)
        {
            Assert.NotNull(session);
            Assert.NotNull(session.Root);
            NodeInfo nodeInfo = session.Root.ToNodeInfo();
            Assert.Equal(nodeInfo.IsLeaf, false);
           
            NodeInfo databaseNode = null;

            if (serverNode)
            {
                Assert.Equal(nodeInfo.NodeType, NodeTypes.Server.ToString());
                var children = session.Root.Expand();

                //All server children should be folder nodes
                foreach (var item in children)
                {
                    Assert.Equal(item.NodeType, "Folder");
                }

                var databasesRoot = children.FirstOrDefault(x => x.NodeTypeId == NodeTypes.Databases);
                var databasesChildren = await _service.ExpandNode(session, databasesRoot.GetNodePath());
                var databases = databasesChildren.Where(x => x.NodeType == NodeTypes.Database.ToString());

                //Verify the test databases is in the list
                Assert.NotNull(databases);
                databaseNode = databases.FirstOrDefault(d => d.Label == databaseName);
            }
            else
            {
                Assert.Equal(nodeInfo.NodeType, NodeTypes.Database.ToString());
                databaseNode = session.Root.ToNodeInfo();
                Assert.True(databaseNode.Label.Contains(databaseName));
            }
            Assert.NotNull(databaseNode);
            return databaseNode;
        }

        private void ExpandAndVerifyDatabaseNode(string databaseName, ObjectExplorerSession session)
        {
            Assert.NotNull(session);
            Assert.NotNull(session.Root);
            NodeInfo nodeInfo = session.Root.ToNodeInfo();
            Assert.Equal(nodeInfo.IsLeaf, false);
            Assert.Equal(nodeInfo.NodeType, NodeTypes.Database.ToString());
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
                if(!node.NodePath.Contains("System") &&
                    !node.NodePath.Contains("FileTables") && !node.NodePath.Contains("External Tables"))
                {
                    var labaleToUpper = node.Label.ToUpper();
                    foreach (var child in children)
                    {
                        if (child.NodeType != "Folder")
                        {
                            Assert.NotNull(child.NodeType);
                            if (child.Metadata != null && !string.IsNullOrEmpty(child.Metadata.MetadataTypeName))
                            {
                                if (!string.IsNullOrEmpty(child.Metadata.Schema))
                                {
                                    Assert.Equal($"{child.Metadata.Schema}.{child.Metadata.Name}", child.Label);
                                }
                                else
                                {
                                    Assert.Equal(child.Metadata.Name, child.Label);
                                }
                            }
                            else
                            {

                            }
                        }
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
                var databaseNodeInfo = await ExpandServerNodeAndVerifyDatabaseHierachy(testDb.DatabaseName, session, false);
                await ExpandTree(databaseNodeInfo, session);

                var tables = session.Root.GetChildren().FirstOrDefault(x => x.Label == SR.SchemaHierarchy_Tables);

                //No tables should be under System, External and Files tables folder
                var systemTables = tables.GetChildren().FirstOrDefault(x => x.Label == SR.SchemaHierarchy_SystemTables).GetChildren();
                
                Assert.True(systemTables.Count() == 0);
                var externalTables = tables.GetChildren().FirstOrDefault(x => x.Label == SR.SchemaHierarchy_ExternalTables).GetChildren();
                Assert.True(externalTables.Count() == 0);
                var fileTables = tables.GetChildren().FirstOrDefault(x => x.Label == SR.SchemaHierarchy_FileTables).GetChildren();
                Assert.True(fileTables.Count() == 0);

                var allTables = tables.GetChildren().Where(x => x.NodeType != NodeTypes.Folder.ToString());
                Assert.True(allTables.Count() > 0);
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
                var databaseNodeInfo = await ExpandServerNodeAndVerifyDatabaseHierachy(testDb.DatabaseName, session);
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
                var databaseNodeInfo = await ExpandServerNodeAndVerifyDatabaseHierachy(testDb.DatabaseName, session);
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
                var databaseNodeInfo = await ExpandServerNodeAndVerifyDatabaseHierachy(testDb.DatabaseName, session);
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
                var databaseNodeInfo = await ExpandServerNodeAndVerifyDatabaseHierachy(testDb.DatabaseName, session);
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
