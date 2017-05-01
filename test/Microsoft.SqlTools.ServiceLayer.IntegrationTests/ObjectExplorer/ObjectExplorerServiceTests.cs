//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
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
                Console.WriteLine($"Verifying the test uri:{uri}");
                await test(testDb.DatabaseName, session);
                Console.WriteLine($"Done verifying test uri:{uri}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to run OE test. uri:{uri} error:{ex.Message}");
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
                    if (stringBuilder != null && child.NodeType != "Folder" && child.NodeType != "FileGroupFile")
                    {
                        stringBuilder.AppendLine($"NodeType: {child.NodeType} Label: {child.Label}");
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
        /// Returns the children of a node with the given label
        /// </summary>
        private async Task<IList<NodeInfo>> FindNodeByLabel(NodeInfo node, ObjectExplorerSession session, string nodeType, bool nodeFound = false)
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
