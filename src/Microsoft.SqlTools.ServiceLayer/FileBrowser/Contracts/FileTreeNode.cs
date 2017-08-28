//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Simple tree node to represent file/folder structure
    /// </summary>
    public class FileTreeNode
    {
        public FileTreeNode Parent { get; private set; }
        public List<FileTreeNode> Children { get; private set; }

        // Indicates if the node is expanded. Applicable to a folder
        public bool IsExpanded { get; set; }

        // Indicates if the node is file or folder
        public bool IsFile { get; set; }

        public string Name { get; set; }

        public string FullPath { get; set; }

        public FileTreeNode()
        {
            this.Children = new List<FileTreeNode>();
        }

        public void AddChildNode(FileTreeNode item)
        {
            item.Parent = this;
            this.Children.Add(item);
        }
    }
}