//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser
{
    /// <summary>
    /// Implementation for file browser operation
    /// </summary>
    internal class FileBrowserOperation : FileBrowserBase
    {
        private FileTree fileTree;
        private string expandPath;
        private string[] fileFilters;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FileBrowser"/> class.
        /// </summary>
        public FileBrowserOperation()
        {
            this.fileTree = new FileTree();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileBrowser"/> class.
        /// </summary>
        /// <param name="connectionInfo">The connection info</param>
        /// <param name="fileFilters">The file extension filters</param>
        public FileBrowserOperation(SqlConnection connectionInfo, string expandPath, string[] fileFilters = null): this()
        {
            this.sqlConnection = connectionInfo;
            this.expandPath = expandPath;
            if (fileFilters == null)
            {
                this.fileFilters = new string[1] { "*" };
            }
            else
            {
                this.fileFilters = fileFilters;
            }
        }

        #endregion

        #region public properties and methods

        public FileTree FileTree
        {
            get
            {
                return this.fileTree;
            }
        }

        internal string[] FileFilters
        {
            get
            {
                return this.fileFilters;
            }
        }

        public void PopulateFileTree()
        {
            this.PathSeparator = GetPathSeparator(this.Enumerator, this.sqlConnection);
            PopulateDrives();
            ExpandSelectedNode(this.expandPath);
        }

        /// <summary>
        /// Expand nodes for the selected path.
        /// </summary>
        public void ExpandSelectedNode(string expandPath)
        {
            this.expandPath = expandPath;
            if (!string.IsNullOrEmpty(this.expandPath))
            {
                var dirs = this.expandPath.TrimEnd(this.PathSeparator).Split(this.PathSeparator);
                List<FileTreeNode> currentChildren = this.fileTree.RootNode.Children;
                FileTreeNode lastNode = null;
                string pathSeparatorString = Convert.ToString(this.PathSeparator);

                foreach (string dir in dirs)
                {
                    FileTreeNode currentNode = null;
                    foreach (FileTreeNode node in currentChildren)
                    {
                        if (node.Name == pathSeparatorString || string.Equals(node.Name, dir, StringComparison.OrdinalIgnoreCase))
                        {
                            currentNode = node;
                            break;
                        }
                    }

                    if (currentNode != null)
                    {
                        currentNode.IsExpanded = true;
                        if (!currentNode.IsFile)
                        {
                            PopulateFileNode(currentNode);
                        }
                        currentChildren = currentNode.Children;
                        lastNode = currentNode;
                    }
                    else
                    {
                        if (lastNode != null)
                        {
                            this.fileTree.SelectedNode = lastNode;
                        }
                        throw new FileBrowserException(string.Format(SR.InvalidPathError, this.expandPath));
                    }
                }

                if (lastNode != null)
                {
                    this.fileTree.SelectedNode = lastNode;
                }
            }
        }

        #endregion

        private void PopulateDrives()
        {
            bool first = true;
            foreach (var fileInfo in EnumerateDrives(Enumerator, sqlConnection))
            {
                // Windows drive letter paths have a '\' at the end. Linux drive paths won't have a '\'.
                var node = new FileTreeNode
                {
                    Name = Convert.ToString(fileInfo.path, CultureInfo.InvariantCulture).TrimEnd('\\'),
                    FullPath = fileInfo.path
                };

                this.fileTree.RootNode.AddChildNode(node);

                if (first)
                {
                    this.fileTree.SelectedNode = node;
                    first = false;
                }

                PopulateFileNode(node);
            }
        }

        private void PopulateFileNode(FileTreeNode parentNode)
        {
            string parentPath = parentNode.FullPath;
            parentNode.Children.Clear();

            foreach (var file in EnumerateFilesInFolder(Enumerator, sqlConnection, parentPath))
            {
                bool isFile = !string.IsNullOrEmpty(file.fileName);
                FileTreeNode treeNode = new FileTreeNode();
                if (isFile)
                {
                    treeNode.Name = file.fileName;
                    treeNode.FullPath = file.fullPath;
                }
                else
                {
                    treeNode.Name = file.folderName;
                    treeNode.FullPath = file.path;
                }
                treeNode.IsFile = isFile;

                // if the node is a directory, or if we are browsing for files and the file name is allowed,
                // add the node to the tree
                if (!isFile || (this.FilterFile(treeNode.Name, this.fileFilters)))
                {
                    parentNode.AddChildNode(treeNode);
                }
            }
        }

        /// <summary>
        /// Filter a filename based on the full mask provide.  The full mask may be a collection a masks seperated by semi-colons.
        /// For example: *; *.txt
        /// </summary>
        internal bool FilterFile(string fileName, string[] masks)
        {
            for (int index = 0; index < masks.Length; index++)
            {
                if (MatchFileToSubMask(fileName, masks[index]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Compares a file name to the user specified mask using a regular expression
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        private bool MatchFileToSubMask(string fileName, string mask)
        {
            Regex regex;

            // If this mask is for all files (*) then just return true.
            if (mask == "*")
            {
                return true;
            }

            mask = mask.Replace(".", "\\.");
            mask = mask.Replace("*", ".*");
            mask = mask.Replace("?", ".");

            // Perform case insensitive RegEx
            {
                regex = new Regex(mask, RegexOptions.IgnoreCase);
            }

            if (!regex.IsMatch(fileName))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
