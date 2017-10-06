//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Tree node to represent file or folder
    /// </summary>
    public class FileTreeNode
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public FileTreeNode()
        {
            this.Children = new List<FileTreeNode>();
        }

        /// <summary>
        /// List of children nodes
        /// </summary>
        public List<FileTreeNode> Children { get; private set; }

        // Indicates if the node is expanded, applicable to a folder.
        public bool IsExpanded { get; set; }

        // Indicates if the node is file or folder
        public bool IsFile { get; set; }

        /// <summary>
        /// File or folder name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Full path
        /// </summary>
        public string FullPath { get; set; }

        public void AddChildNode(FileTreeNode item)
        {
            this.Children.Add(item);
        }
    }
}