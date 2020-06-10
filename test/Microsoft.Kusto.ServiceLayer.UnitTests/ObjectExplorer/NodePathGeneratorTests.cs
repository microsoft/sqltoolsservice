//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;
using Xunit;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.ObjectExplorer
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

        [Fact]
        public void FindCorrectPathsForTableWithServerRoot()
        {
            var paths = NodePathGenerator.FindNodePaths(serverSession, "Table", "testSchema", "testTable", databaseName);
            var expectedPaths = new List<string>
            {
                "testServer/Databases/testDatabase/Tables/testSchema.testTable",
                "testServer/Databases/System Databases/testDatabase/Tables/testSchema.testTable",
                "testServer/Databases/testDatabase/Tables/System Tables/testSchema.testTable",
                "testServer/Databases/System Databases/testDatabase/Tables/System Tables/testSchema.testTable"
            };

            Assert.Equal(expectedPaths.Count, paths.Count);
            foreach (var expectedPath in expectedPaths)
            {
                Assert.True(paths.Contains(expectedPath));
            }
        }

        [Fact]
        public void FindCorrectPathsForTableWithDatabaseRoot()
        {
            var paths = NodePathGenerator.FindNodePaths(databaseSession, "Table", "testSchema", "testTable", null);
            var expectedPaths = new List<string>
            {
                "testServer/testDatabase/Tables/testSchema.testTable",
                "testServer/testDatabase/Tables/System Tables/testSchema.testTable"
            };

            Assert.Equal(expectedPaths.Count, paths.Count);
            foreach (var expectedPath in expectedPaths)
            {
                Assert.True(paths.Contains(expectedPath));
            }
        }

        [Fact]
        public void FindCorrectPathsForColumnWithServerRoot()
        {
            var paths = NodePathGenerator.FindNodePaths(serverSession, "Column", null, "testColumn", databaseName, new List<string> { "testSchema.testTable" });
            var expectedPaths = new List<string>
            {
                "testServer/Databases/testDatabase/Tables/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/System Databases/testDatabase/Tables/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/testDatabase/Tables/System Tables/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/System Databases/testDatabase/Tables/System Tables/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/testDatabase/Views/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/System Databases/testDatabase/Views/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/testDatabase/Views/System Views/testSchema.testTable/Columns/testColumn",
                "testServer/Databases/System Databases/testDatabase/Views/System Views/testSchema.testTable/Columns/testColumn"
            };

            Assert.Equal(expectedPaths.Count, paths.Count);
            foreach (var expectedPath in expectedPaths)
            {
                Assert.True(paths.Contains(expectedPath));
            }
        }

        [Fact]
        public void FindCorrectPathsForColumnWithDatabaseRoot()
        {
            var paths = NodePathGenerator.FindNodePaths(databaseSession, "Column", null, "testColumn", databaseName, new List<string> { "testSchema.testTable" });
            var expectedPaths = new List<string>
            {
                "testServer/testDatabase/Tables/testSchema.testTable/Columns/testColumn",
                "testServer/testDatabase/Tables/System Tables/testSchema.testTable/Columns/testColumn",
                "testServer/testDatabase/Views/testSchema.testTable/Columns/testColumn",
                "testServer/testDatabase/Views/System Views/testSchema.testTable/Columns/testColumn"
            };

            Assert.Equal(expectedPaths.Count, paths.Count);
            foreach (var expectedPath in expectedPaths)
            {
                Assert.True(paths.Contains(expectedPath));
            }
        }

        [Fact]
        public void FindCorrectPathsForDatabase()
        {
            var paths = NodePathGenerator.FindNodePaths(serverSession, "Database", null, databaseName, null);
            var expectedPaths = new List<string>
            {
                "testServer/Databases/testDatabase",
                "testServer/Databases/System Databases/testDatabase"
            };

            Assert.Equal(expectedPaths.Count, paths.Count);
            foreach (var expectedPath in expectedPaths)
            {
                Assert.True(paths.Contains(expectedPath));
            }
        }

        [Fact]
        public void FindPathForInvalidTypeReturnsEmpty()
        {
            var serverPaths = NodePathGenerator.FindNodePaths(serverSession, "WrongType", "testSchema", "testTable", databaseName);
            Assert.Equal(0, serverPaths.Count);
        }

        [Fact]
        public void FindPathMissingParentReturnsEmpty()
        {
            var serverPaths = NodePathGenerator.FindNodePaths(serverSession, "Column", "testSchema", "testColumn", databaseName);
            Assert.Equal(0, serverPaths.Count);
        }
    }
}