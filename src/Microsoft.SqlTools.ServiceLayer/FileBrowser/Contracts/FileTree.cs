//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Tree to represent file/folder structure
    /// </summary>
    public class FileTree
    {
        public FileTreeNode RootNode { get; private set; }
        public FileTreeNode SelectedNode { get; set; }

        public FileTree()
        {
            this.RootNode = new FileTreeNode();
        }
    }
}