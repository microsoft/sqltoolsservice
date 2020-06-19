//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.Kusto.ServiceLayer.FileBrowser;
using Microsoft.Kusto.ServiceLayer.FileBrowser.Contracts;
using Xunit;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.FileBrowser
{
    /// <summary>
    /// File browser unit tests
    /// </summary>
    public class FileBrowserTests
    {
        [Fact]
        public void CreateFileBrowserOperationTest()
        {
            var operation = new FileBrowserOperation(null, "", null);
            Assert.True(operation.FileTree != null);

            // It should set "*" filter as default
            Assert.True(operation.FileFilters.Length == 1);
        }

        [Fact]
        public void FilterFilesTest()
        {
            var operation = new FileBrowserOperation(null, "", null);
            string[] supportedFilePaths = {"te\\s/t1.txt", "te!s.t2.bak"};
            string[] unsupportedFilePaths = {"te.s*/t3.jpg", "t_est4.trn"};
            string[] filters = {"*.txt", "*.bak"};

            foreach (string path in supportedFilePaths)
            {
                Assert.True(operation.FilterFile(path, filters));
            }

            foreach (string path in unsupportedFilePaths)
            {
                Assert.False(operation.FilterFile(path, filters));
            }
        }

        [Fact]
        public void ExpandNodeShouldThrowExceptionForInvalidPath()
        {
            var operation = new FileBrowserOperation(null, "", null);
            Exception exception = null;

            try
            {
                operation.PathSeparator = '/';
                operation.ExpandSelectedNode("testdrive/filebrowser/test");
            }
            catch (FileBrowserException ex)
            {
                exception = ex;
            }

            Assert.NotNull(exception);
            Assert.Null(operation.FileTree.SelectedNode);
        }

        [Fact]
        public void CreateFileTreeTest()
        {
            var tree = new FileTree();
            Assert.NotNull(tree.RootNode);
            Assert.Null(tree.SelectedNode);
        }

        [Fact]
        public void AddFileTreeNodeChildTest()
        {
            var node1 = new FileTreeNode();
            var node2 = new FileTreeNode();
            node1.AddChildNode(node2);
            Assert.NotNull(node1.Children);
        }
    }
}