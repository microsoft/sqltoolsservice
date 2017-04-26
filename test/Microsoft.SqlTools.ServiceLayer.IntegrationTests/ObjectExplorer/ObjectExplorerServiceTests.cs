//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
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
            }
            CancelConnection(uri);
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
            }
            CancelConnection(uri);

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
            }
            CancelConnection(uri);

        }

        private async Task<ObjectExplorerSession> CreateSession(string databaseName, string uri)
        {
            ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem, databaseName);
           // connectParams.Connection.Pooling = false;
            ConnectionDetails details = connectParams.Connection;

            var session =  await _service.DoCreateSession(details, uri);
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "OE session created for database: {0}", databaseName));
            return session;
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
            ConnectionService.Instance.CancelConnect(new CancelConnectParams
            {
                OwnerUri = uri,
                Type = ConnectionType.Default
            });
        }

        /// <summary>
        /// Returns the children of a node with the given label
        /// </summary>
        private async Task<NodeInfo[]> FindNodeByLabel(NodeInfo node, ObjectExplorerSession session, string nodeType, bool nodeFound = false)
        {
            if (node != null && !node.IsLeaf)
            {
                var children = await _service.ExpandNode(session, node.NodePath);
                Assert.NotNull(children);
                if (!nodeFound)
                {
                    foreach (var child in children)
                    {
                        VerifyMetadata(child);
                        if (child.Label == nodeType)
                        {
                            return await FindNodeByLabel(child, session, nodeType, true);
                        }
                        var result = await FindNodeByLabel(child, session, nodeType);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
                else
                {
                    return children;
                }
            }

            return null;
        }


        private void VerifyMetadata(NodeInfo node)
        {
            if (node.NodeType != "Folder")
            {
                Assert.NotNull(node.NodeType);
                if (node.Metadata != null && !string.IsNullOrEmpty(node.Metadata.MetadataTypeName))
                {
                    if (!string.IsNullOrEmpty(node.Metadata.Schema))
                    {
                        Assert.Equal($"{node.Metadata.Schema}.{node.Metadata.Name}", node.Label);
                    }
                    else
                    {
                        Assert.Equal(node.Metadata.Name, node.Label);
                    }
                }
            }
        }


        [Fact]
        public async void VerifyAdventureWorksDatabaseObjects()
        {
            var query = Scripts.AdventureWorksScript;
            string uri = "VerifyAdventureWorksDatabaseObjects";
            string databaseName = null;

                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName, query, uri))
            {
                var session = await CreateSession(testDb.DatabaseName, queryTempFile.FilePath);
                var databaseNodeInfo = await ExpandServerNodeAndVerifyDatabaseHierachy(testDb.DatabaseName, session, false);
                var tablesChildren = await FindNodeByLabel(databaseNodeInfo, session, SR.SchemaHierarchy_Tables);

                var systemTables = await FindNodeByLabel(databaseNodeInfo, session, SR.SchemaHierarchy_SystemTables);
                Assert.True(!systemTables.Any());

                var externalTables = await FindNodeByLabel(databaseNodeInfo, session, SR.SchemaHierarchy_ExternalTables);
                Assert.True(!externalTables.Any());

                var fileTables = await FindNodeByLabel(databaseNodeInfo, session, SR.SchemaHierarchy_FileTables);
                Assert.True(!fileTables.Any());

                var allTables = tablesChildren.Where(x => x.NodeType != NodeTypes.Folder.ToString());
                Assert.True(allTables.Any());

                var storedProcedures = await FindNodeByLabel(databaseNodeInfo, session, SR.SchemaHierarchy_StoredProcedures);
                Assert.True(storedProcedures.Any());

                var views = await FindNodeByLabel(databaseNodeInfo, session, SR.SchemaHierarchy_Views);
                Assert.True(views.Any());

                var userDefinedDataTypes = await FindNodeByLabel(databaseNodeInfo, session, SR.SchemaHierarchy_UserDefinedDataTypes);
                Assert.True(userDefinedDataTypes.Any());

                var scalarValuedFunctions = await FindNodeByLabel(databaseNodeInfo, session, SR.SchemaHierarchy_ScalarValuedFunctions);
                Assert.True(scalarValuedFunctions.Any());

            }
           CancelConnection(uri);

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
                await FindNodeByLabel(databaseNodeInfo, session, SR.SchemaHierarchy_Tables);
            }
            CancelConnection(uri);

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
                await FindNodeByLabel(databaseNodeInfo, session, SR.SchemaHierarchy_Tables);
            }
            CancelConnection(uri);

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
                await FindNodeByLabel(databaseNodeInfo, session, SR.SchemaHierarchy_Tables);
            }
            CancelConnection(uri);

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
                await FindNodeByLabel(databaseNodeInfo, session, SR.SchemaHierarchy_Tables);
            }
            CancelConnection(uri);

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
