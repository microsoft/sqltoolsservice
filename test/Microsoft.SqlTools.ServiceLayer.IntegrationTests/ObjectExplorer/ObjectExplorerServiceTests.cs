//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.Baselined;
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
            string databaseName = null;
            await RunTest(databaseName, query, "EmptyDatabase", async (testDbName, session) =>
            {
                await ExpandServerNodeAndVerifyDatabaseHierachy(testDbName, session);
            });
        }

        [Fact]
        public async void CreateSessionWithTempdbAndExpandOnTheServerShouldReturnServerAsTheRoot()
        {
            var query = "";
            string databaseName = "tempdb";
            await RunTest(databaseName, query, "TepmDb", async (testDbName, session) =>
            {
                await ExpandServerNodeAndVerifyDatabaseHierachy(testDbName, session);
            });
        }

        [Fact]
        public async void VerifyServerLogins()
        {
            var query = @"If Exists (select loginname from master.dbo.syslogins 
                            where name = 'OEServerLogin')
                        Begin
                            Drop Login  [OEServerLogin]
                        End

                        CREATE LOGIN OEServerLogin WITH PASSWORD = 'SuperSecret52&&'
                        GO
                        ALTER LOGIN OEServerLogin DISABLE; ";
            string databaseName = "tempdb";
            await RunTest(databaseName, query, "TepmDb", async (testDbName, session) =>
            {
                var serverChildren = await _service.ExpandNode(session, session.Root.GetNodePath());
                var securityNode = serverChildren.FirstOrDefault(x => x.Label == SR.SchemaHierarchy_Security);
                var securityChildren = await _service.ExpandNode(session, securityNode.NodePath);
                var loginsNode = securityChildren.FirstOrDefault(x => x.Label == SR.SchemaHierarchy_Logins);
                var loginsChildren = await _service.ExpandNode(session, loginsNode.NodePath);
                var login = loginsChildren.FirstOrDefault(x => x.Label == "OEServerLogin");
                Assert.NotNull(login);
               
                Assert.True(login.NodeStatus == "Disabled");
                await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, testDbName, "Drop Login  OEServerLogin");

            });
        }

        [Fact]
        public async void VerifyServerTriggers()
        {
            var query = @"IF EXISTS (SELECT * FROM sys.server_triggers  WHERE name = 'OE_ddl_trig_database')
                           
                        Begin
                            DROP TRIGGER OE_ddl_trig_database  ON ALL SERVER
                        
                        ENd
                        GO

                        CREATE TRIGGER OE_ddl_trig_database   
                        ON ALL SERVER   
                        FOR CREATE_DATABASE   
                        AS   
                            PRINT 'Database Created.'  
                        GO  
                        GO
                        Disable TRIGGER OE_ddl_trig_database ON ALL SERVER ;";
            string databaseName = "tempdb";
            await RunTest(databaseName, query, "TepmDb", async (testDbName, session) =>
            {
                var serverChildren = await _service.ExpandNode(session, session.Root.GetNodePath());
                var serverObjectsNode = serverChildren.FirstOrDefault(x => x.Label == SR.SchemaHierarchy_ServerObjects);
                var serverObjectsChildren = await _service.ExpandNode(session, serverObjectsNode.NodePath);
                var triggersNode = serverObjectsChildren.FirstOrDefault(x => x.Label == SR.SchemaHierarchy_Triggers);
                var triggersChildren = await _service.ExpandNode(session, triggersNode.NodePath);
                var trigger = triggersChildren.FirstOrDefault(x => x.Label == "OE_ddl_trig_database");
                Assert.NotNull(trigger);

                Assert.True(trigger.NodeStatus == "Disabled");
                await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, testDbName, "DROP TRIGGER OE_ddl_trig_database");

            });
        }

        [Fact]
        public async void CreateSessionAndExpandOnTheDatabaseShouldReturnDatabaseAsTheRoot()
        {
            var query = "";
            string databaseName = "#testDb#";
            await RunTest(databaseName, query, "TestDb", async (testDbName, session) =>
            {
                await ExpandAndVerifyDatabaseNode(testDbName, session);
            });
        }

        [Fact]
        public async void RefreshNodeShouldGetTheDataFromDatabase()
        {
            var query = "Create table t1 (c1 int)";
            string databaseName = "#testDb#";
            await RunTest(databaseName, query, "TestDb", async (testDbName, session) =>
            {
                var tablesNode = await FindNodeByLabel(session.Root.ToNodeInfo(), session, SR.SchemaHierarchy_Tables);
                var tableChildren = await _service.ExpandNode(session, tablesNode.NodePath);
                string dropTableScript = "Drop Table t1";
                Assert.True(tableChildren.Any(t => t.Label == "dbo.t1"));
                await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, testDbName, dropTableScript);
                tableChildren = await _service.ExpandNode(session, tablesNode.NodePath);
                Assert.True(tableChildren.Any(t => t.Label == "dbo.t1"));
                tableChildren = await _service.ExpandNode(session, tablesNode.NodePath, true);
                Assert.False(tableChildren.Any(t => t.Label == "dbo.t1"));

            });
        }

        [Fact]
        public async void RefreshShouldCleanTheCache()
        {
            string query = @"Create table t1 (c1 int)
                            GO
                            Create table t2 (c1 int)
                            GO";
            string dropTableScript1 = "Drop Table t1";
            string createTableScript2 = "Create table t3 (c1 int)";

            string databaseName = "#testDb#";
            await RunTest(databaseName, query, "TestDb", async (testDbName, session) =>
            {
                var tablesNode = await FindNodeByLabel(session.Root.ToNodeInfo(), session, SR.SchemaHierarchy_Tables);

                //Expand Tables node
                var tableChildren = await _service.ExpandNode(session, tablesNode.NodePath);

                //Expanding the tables return t1
                Assert.True(tableChildren.Any(t => t.Label == "dbo.t1"));

                //Delete the table from db
                await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, testDbName, dropTableScript1);

                //Expand Tables node
                tableChildren = await _service.ExpandNode(session, tablesNode.NodePath);

                //Tables still includes t1
                Assert.True(tableChildren.Any(t => t.Label == "dbo.t1"));

                //Verify the tables cache has items 

                var rootChildrenCache = session.Root.GetChildren();
                var tablesCache = rootChildrenCache.First(x => x.Label == SR.SchemaHierarchy_Tables).GetChildren();
                Assert.True(tablesCache.Any());

                await VerifyRefresh(session, tablesNode.NodePath, "dbo.t1");
                //Delete the table from db
                await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, testDbName, createTableScript2);
                await VerifyRefresh(session, tablesNode.NodePath, "dbo.t3", false);

            });
        }

        private async Task VerifyRefresh(ObjectExplorerSession session, string tablePath, string tableName, bool deleted = true)
        {
            //Refresh Root
            var rootChildren = await _service.ExpandNode(session, session.Root.ToNodeInfo().NodePath, true);

            //Verify tables cache is empty
            var rootChildrenCache = session.Root.GetChildren();
             var tablesCache = rootChildrenCache.First(x => x.Label == SR.SchemaHierarchy_Tables).GetChildren();
            Assert.False(tablesCache.Any());

            //Expand Tables
            var tableChildren = await _service.ExpandNode(session, tablePath, true);

            //Verify table is not returned
            Assert.Equal(tableChildren.Any(t => t.Label == tableName), !deleted);

            //Verify tables cache has items
            rootChildrenCache = session.Root.GetChildren();
            tablesCache = rootChildrenCache.First(x => x.Label == SR.SchemaHierarchy_Tables).GetChildren();
            Assert.True(tablesCache.Any());
        }

        [Fact]
        public async void VerifyAllSqlObjects()
        {
            var queryFileName = "AllSqlObjects.sql";
            string baselineFileName = "AllSqlObjects.txt";
            string databaseName = "#testDb#";
            await TestServiceProvider.CalculateRunTime(() => VerifyObjectExplorerTest(databaseName, "AllSqlObjects", queryFileName, baselineFileName), true);
        }

        //[Fact]
        //This takes take long to run so not a good test for CI builds
        public async void VerifySystemObjects()
        {
            string queryFileName = null;
            string baselineFileName = null;
            string databaseName = "#testDb#";
            await TestServiceProvider.CalculateRunTime(() => VerifyObjectExplorerTest(databaseName, queryFileName, "SystemOBjects", baselineFileName, true), true);
        }

        private async Task RunTest(string databaseName, string query, string testDbPrefix, Func<string, ObjectExplorerSession, Task> test)
        {
            SqlTestDb testDb = null;
            string uri = string.Empty;
            try
            {
                testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, query, testDbPrefix);
                if (databaseName == "#testDb#")
                {
                    databaseName = testDb.DatabaseName;
                }
                var session = await CreateSession(databaseName);
                uri = session.Uri;
                await test(testDb.DatabaseName, session);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to run OE test. uri:{uri} error:{ex.Message} {ex.StackTrace}");
                Assert.False(true, ex.Message);
            }
            finally
            {
                if (!string.IsNullOrEmpty(uri))
                {
                    CloseSession(uri);
                }
                if (testDb != null)
                {
                    await testDb.CleanupAsync();
                }
            }
        }

        private async Task<ObjectExplorerSession> CreateSession(string databaseName)
        {
            ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem, databaseName);
            //connectParams.Connection.Pooling = false;
            ConnectionDetails details = connectParams.Connection;
            string uri = ObjectExplorerService.GenerateUri(details);

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

        private async Task ExpandAndVerifyDatabaseNode(string databaseName, ObjectExplorerSession session)
        {
            Assert.NotNull(session);
            Assert.NotNull(session.Root);
            NodeInfo nodeInfo = session.Root.ToNodeInfo();
            Assert.Equal(nodeInfo.IsLeaf, false);
            Assert.Equal(nodeInfo.NodeType, NodeTypes.Database.ToString());
            Assert.True(nodeInfo.Label.Contains(databaseName));
            var children = await _service.ExpandNode(session, session.Root.GetNodePath());

            //All server children should be folder nodes
            foreach (var item in children)
            {
                Assert.Equal(item.NodeType, "Folder");
            }

            var tablesRoot = children.FirstOrDefault(x => x.Label == SR.SchemaHierarchy_Tables);
            Assert.NotNull(tablesRoot);
        }

        private void CloseSession(string uri)
        {
            _service.CloseSession(uri);
            Console.WriteLine($"Session closed uri:{uri}");
        }

        private async Task ExpandTree(NodeInfo node, ObjectExplorerSession session, StringBuilder stringBuilder = null, bool verifySystemObjects = false)
        {
            if (node != null && !node.IsLeaf)
            {
                var children = await _service.ExpandNode(session, node.NodePath);
                foreach (var child in children)
                {
                    VerifyMetadata(child);
                    if (stringBuilder != null && child.NodeType != "Folder" && child.NodeType != "FileGroupFile")
                    {
                        stringBuilder.AppendLine($"NodeType: {child.NodeType} Label: {child.Label} SubType:{child.NodeSubType} Status:{child.NodeStatus}");
                    }
                    if (!verifySystemObjects && (child.Label == SR.SchemaHierarchy_SystemStoredProcedures ||
                        child.Label == SR.SchemaHierarchy_SystemViews ||
                        child.Label == SR.SchemaHierarchy_SystemFunctions ||
                        child.Label == SR.SchemaHierarchy_SystemDataTypes))
                    {
                        // don't expand the system folders because then the test will take for ever
                    }
                    else
                    {
                        await ExpandTree(child, session, stringBuilder, verifySystemObjects);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the node with the given label
        /// </summary>
        private async Task<NodeInfo> FindNodeByLabel(NodeInfo node, ObjectExplorerSession session, string label)
        {
            if(node != null && node.Label == label)
            {
                return node;
            }
            else if (node != null && !node.IsLeaf)
            {
                var children = await _service.ExpandNode(session, node.NodePath);
                Assert.NotNull(children);
                foreach (var child in children)
                {
                    VerifyMetadata(child);
                    if (child.Label == label)
                    {
                        return child;
                    }
                    var result = await FindNodeByLabel(child, session, label);
                    if (result != null)
                    {
                        return result;
                    }
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
                        Assert.True(node.Label.Contains($"{node.Metadata.Schema}.{node.Metadata.Name}"));
                    }
                    else
                    {
                        Assert.NotNull(node.Label);
                    }
                }
            }
        }

        private async Task<bool> VerifyObjectExplorerTest(string databaseName, string testDbPrefix, string queryFileName, string baselineFileName, bool verifySystemObjects = false)
        {
            var query = string.IsNullOrEmpty(queryFileName) ? string.Empty : LoadScript(queryFileName);
            StringBuilder stringBuilder = new StringBuilder();
            await RunTest(databaseName, query, testDbPrefix, async (testDbName, session) =>
            {
                await ExpandServerNodeAndVerifyDatabaseHierachy(testDbName, session, false);
                await ExpandTree(session.Root.ToNodeInfo(), session, stringBuilder, verifySystemObjects);
                string baseline = string.IsNullOrEmpty(baselineFileName) ? string.Empty : LoadBaseLine(baselineFileName);
                if (!string.IsNullOrEmpty(baseline))
                {
                    string actual = stringBuilder.ToString();
                    BaselinedTest.CompareActualWithBaseline(actual, baseline);
                }
            });
           
            return true;
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

        public DirectoryInfo BaselineFileDirectory
        {
            get
            {
                string d = Path.Combine(TestLocationDirectory, "Baselines");
                return new DirectoryInfo(d);
            }
        }

        public FileInfo GetInputFile(string fileName)
        {
            return new FileInfo(Path.Combine(InputFileDirectory.FullName, fileName));
        }

        public FileInfo GetBaseLineFile(string fileName)
        {
            return new FileInfo(Path.Combine(BaselineFileDirectory.FullName, fileName));
        }

        private string LoadScript(string fileName)
        {
            FileInfo inputFile = GetInputFile(fileName);
            return TestUtilities.ReadTextAndNormalizeLineEndings(inputFile.FullName);
        }

        private string LoadBaseLine(string fileName)
        {
            FileInfo inputFile = GetBaseLineFile(fileName);
            return TestUtilities.ReadTextAndNormalizeLineEndings(inputFile.FullName);
        }
    }
}
