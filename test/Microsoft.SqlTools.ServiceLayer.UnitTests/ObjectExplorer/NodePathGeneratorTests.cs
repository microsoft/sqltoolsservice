//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{

    public class NodePathGeneratorTests
    {
        private ObjectExplorerService.ObjectExplorerSession serverSession;
        private ObjectExplorerService.ObjectExplorerSession databaseSession;
        private const string serverName = "testServer";
        private const string databaseName = "testDatabase";

        public NodePathGeneratorTests()
        {
            var serverRoot = new TreeNode
            {
                NodeType = "Server",
                NodeValue = serverName
            };

            serverSession = new ObjectExplorerService.ObjectExplorerSession("serverUri", serverRoot, null, null);

            var databaseRoot = new TreeNode
            {
                NodeType = "Database",
                NodeValue = databaseName,
                Parent = serverRoot
            };

            databaseSession = new ObjectExplorerService.ObjectExplorerSession("databaseUri", databaseRoot, null, null);
        }

        [Test]
        public void FindCorrectPathsForTableWithServerRoot()
        {
            var paths = NodePathGenerator.FindNodePaths(serverSession, "Table", "testSchema", "testTable", databaseName);
            var expectedPaths = new List<string>
            {
                "testServer/Databases/testDatabase/Tables/testSchema.testTable",
                "testServer/Databases/System Databases/testDatabase/Tables/testSchema.testTable",
                "testServer/Databases/testDatabase/Tables/System Tables/testSchema.testTable",
                "testServer/Databases/System Databases/testDatabase/Tables/System Tables/testSchema.testTable",
                "testServer/Databases/testDatabase/Tables/Dropped Ledger Tables/testSchema.testTable",
                "testServer/Databases/System Databases/testDatabase/Tables/Dropped Ledger Tables/testSchema.testTable"
            };

            Assert.AreEqual(expectedPaths.Count, paths.Count);
            foreach (var expectedPath in expectedPaths)
            {
                Assert.True(paths.Contains(expectedPath));
            }
        }

        [Test]
        public void FindCorrectPathsForTableWithDatabaseRoot()
        {
            var paths = NodePathGenerator.FindNodePaths(databaseSession, "Table", "testSchema", "testTable", string.Empty);
            var expectedPaths = new List<string>
            {
                "testServer/testDatabase/Tables/testSchema.testTable",
                "testServer/testDatabase/Tables/System Tables/testSchema.testTable",
                "testServer/testDatabase/Tables/Dropped Ledger Tables/testSchema.testTable"
            };

            Assert.AreEqual(expectedPaths.Count, paths.Count);
            foreach (var expectedPath in expectedPaths)
            {
                Assert.True(paths.Contains(expectedPath));
            }
        }

        [Test]
        public void FindCorrectPathsForColumnWithServerRoot()
        {
            var paths = NodePathGenerator.FindNodePaths(serverSession, "Column", null, "testColumn", databaseName, new List<string> { "testSchema.testTable" });
            var expectedPaths = new List<string>
            {
                "testServer/Databases/testDatabase/Tables/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/System Databases/testDatabase/Tables/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/testDatabase/Tables/System Tables/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/System Databases/testDatabase/Tables/System Tables/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/testDatabase/Tables/Dropped Ledger Tables/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/System Databases/testDatabase/Tables/Dropped Ledger Tables/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/testDatabase/Views/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/System Databases/testDatabase/Views/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/testDatabase/Views/System Views/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/System Databases/testDatabase/Views/System Views/testSchema.testTable/Columns/testColumn"
            };

            Assert.AreEqual(expectedPaths.Count, paths.Count);
            foreach (var expectedPath in expectedPaths)
            {
                Assert.True(paths.Contains(expectedPath));
            }
        }

        [Test]
        public void FindCorrectPathsForColumnWithDatabaseRoot()
        {
            var paths = NodePathGenerator.FindNodePaths(databaseSession, "Column", null, "testColumn", databaseName, new List<string> { "testSchema.testTable" });
            var expectedPaths = new List<string>
            {
                "testServer/testDatabase/Tables/testSchema.testTable/Columns/testColumn",
                "testServer/testDatabase/Tables/System Tables/testSchema.testTable/Columns/testColumn",
                "testServer/testDatabase/Tables/Dropped Ledger Tables/testSchema.testTable/Columns/testColumn",
                "testServer/testDatabase/Views/testSchema.testTable/Columns/testColumn",
                "testServer/testDatabase/Views/System Views/testSchema.testTable/Columns/testColumn"
            };

            Assert.AreEqual(expectedPaths.Count, paths.Count);
            foreach (var expectedPath in expectedPaths)
            {
                Assert.True(paths.Contains(expectedPath));
            }
        }

        [Test]
        public void FindCorrectPathsForDatabase()
        {
            var paths = NodePathGenerator.FindNodePaths(serverSession, "Database", null, databaseName, string.Empty);
            var expectedPaths = new List<string>
            {
                "testServer/Databases/testDatabase",
                "testServer/Databases/System Databases/testDatabase"
            };

            Assert.AreEqual(expectedPaths.Count, paths.Count);
            foreach (var expectedPath in expectedPaths)
            {
                Assert.True(paths.Contains(expectedPath));
            }
        }

        [Test]
        public void FindPathForInvalidTypeReturnsEmpty()
        {
            var serverPaths = NodePathGenerator.FindNodePaths(serverSession, "WrongType", "testSchema", "testTable", databaseName);
            Assert.AreEqual(0, serverPaths.Count);
        }

        [Test]
        public void FindPathMissingParentReturnsEmpty()
        {
            var serverPaths = NodePathGenerator.FindNodePaths(serverSession, "Column", "testSchema", "testColumn", databaseName);
            Assert.AreEqual(0, serverPaths.Count);
        }
    }
}