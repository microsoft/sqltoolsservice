//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Tree to represent file/folder structure
    /// </summary>
    public class FileTree
    {
        /// <summary>
        /// Root node of the tree
        /// </summary>
        public FileTreeNode RootNode { get; private set; }

        /// <summary>
        /// Selected node of the tree
        /// </summary>
        public FileTreeNode SelectedNode { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public FileTree()
        {
            this.RootNode = new FileTreeNode();
        }
    }
}