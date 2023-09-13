﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser
{
    /// <summary>
    /// Implementation for file browser operation
    /// </summary>
    internal class FileBrowserOperation : FileBrowserBase, IDisposable
    {
        private FileTree fileTree;
        private string expandPath;
        private string[] fileFilters;
        private bool fileTreeCreated;
        private CancellationTokenSource cancelSource;
        private CancellationToken cancelToken;
        private bool showFoldersOnly;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FileBrowser"/> class.
        /// </summary>
        /// <param name="connection">The connection object.</param>
        /// <param name="expandPath">The initial folder to open in the file dialog.</param>
        /// <param name="fileFilters">The file extension filters. Ignored if <see cref="showFoldersOnly"/> is set to <c>true</c>.</param>
        /// <param name="showFoldersOnly">Whether to only show folders in the file browser.</param>
        public FileBrowserOperation(ServerConnection connection, string expandPath, string[] fileFilters = null, bool? showFoldersOnly = null)
        {
            this.cancelSource = new CancellationTokenSource();
            this.cancelToken = cancelSource.Token;
            this.connection = connection;
            this.showFoldersOnly = showFoldersOnly ?? false;
            this.Initialize(expandPath, fileFilters);
        }

        #endregion

        #region public properties

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

        public bool FileTreeCreated
        {
            get
            {
                return this.fileTreeCreated;
            }
        }

        public bool IsCancellationRequested
        {
            get
            {
                return this.cancelToken.IsCancellationRequested;
            }
        }

        public void Cancel()
        {
            this.cancelSource.Cancel();
        }
        #endregion

        public void Initialize(string expandPath, string[] fileFilters)
        {
            this.fileTree = new FileTree();
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

        public void Dispose()
        {
            if (this.connection != null)
            {
                this.connection.Disconnect();
            }
            this.cancelSource.Dispose();
        }

        public void PopulateFileTree()
        {
            this.fileTreeCreated = false;
            this.PathSeparator = GetPathSeparator(this.Enumerator, this.connection);
            PopulateDrives();
            ExpandSelectedNode(this.expandPath);
            this.fileTreeCreated = true;
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
                    if (cancelToken.IsCancellationRequested)
                    {
                        break;
                    }

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
                            currentNode.Children = this.GetChildren(currentNode.FullPath);
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

                        // If root folders returned from sys.dm_os_enumerate_filesystem don't match the folders present, expandPath might not be found
                        // Bug: https://github.com/microsoft/azuredatastudio/issues/4767
                        // Workaround : scope file browser tree to expandPath explicitly 
                        try
                        {
                            this.ScopeFileTreeToPath(expandPath);
                            return;
                        }
                        catch
                        {
                            throw new FileBrowserException(string.Format(SR.InvalidPathError, this.expandPath));
                        }
                    }
                }

                if (lastNode != null)
                {
                    this.fileTree.SelectedNode = lastNode;
                }
            }
        }

        public void PopulateDrives()
        {
            bool first = true;
            if (!cancelToken.IsCancellationRequested)
            {
                foreach (var fileInfo in EnumerateDrives(Enumerator, connection))
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        break;
                    }

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

                    node.Children = this.GetChildren(node.FullPath);
                }
            }
        }

        public List<FileTreeNode> GetChildren(string filePath)
        {
            List<FileTreeNode> children = new List<FileTreeNode>();
            foreach (var file in EnumerateFilesInFolder(Enumerator, connection, filePath))
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

                // If the node is a directory, or if we are browsing for files and the file name is allowed,
                // add the node to the tree. Files will be skipped instead if the dialog is only showing folders,
                // regardless of any provided file filters. 
                if (!isFile || (this.FilterFile(treeNode.Name, this.fileFilters) && !this.showFoldersOnly))
                {
                    children.Add(treeNode);
                }
            }
            return children;
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
        /// Creates file tree for a scoped path
        /// Removes the top level file tree so only use this if you intend to do that
        /// </summary>
        /// <param name="expandPath">path to consider at top level node for tree</param>
        private void ScopeFileTreeToPath(string expandPath)
        {
            this.fileTree = new FileTree();
            FileTreeNode node = new FileTreeNode()
            {
                Name = expandPath,
                FullPath = expandPath,
                IsExpanded = true
            };

            this.fileTree.RootNode.AddChildNode(node);
            node.Children = this.GetChildren(node.FullPath);
            this.fileTree.SelectedNode = node;
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
