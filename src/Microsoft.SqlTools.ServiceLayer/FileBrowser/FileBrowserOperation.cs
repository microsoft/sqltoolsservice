//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using Microsoft.SqlServer.Management.Smo;
using System.Text.RegularExpressions;
using System.Collections.Generic;
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
        /// <param name="connectionInfo">The connection info.</param>
        /// <param name="filter">The filters. </param>
        public FileBrowserOperation(object connectionInfo, string expandPath, string[] filters = null)
            : this()
        {
            this.sqlConnection = connectionInfo;
            this.expandPath = expandPath;
            if (filters == null)
            {
                this.fileFilters = new string[1] { "*" };
            }
            else
            {
                this.fileFilters = filters;
            }
        }

        #endregion

        #region public methods and delegates

        protected override void Initialize()
        {
            this.PathSeparator = GetPathSeparator(SfcEnumerator, sqlConnection);
            FillDrives();
            ExpandNodes();
        }

        #endregion

        private void FillDrives()
        {
            bool first = true;
            foreach (var fileInfo in EnumerateDrives(SfcEnumerator, sqlConnection))
            {
                // Windows drive letter paths have a '\' at the end. Linux drive paths won't have a '\'.
                var node = new FileTreeNode
                {
                    Name = Convert.ToString(fileInfo.path, CultureInfo.InvariantCulture).TrimEnd('\\'),
                    FullPath = fileInfo.path //TODO: check this value
                };

                this.fileTree.RootNode.AddChildNode(node);

                if (first)
                {
                    this.fileTree.SelectedNode = node;
                    first = false;
                }

                FillTree(node);
            }
        }

        private void FillTree(FileTreeNode parentNode)
        {
            try
            {
                string parentPath = parentNode.FullPath; //TODO: make sure this is correct value
                parentNode.Children.Clear();

                foreach (var file in EnumerateFilesInFolder(SfcEnumerator, sqlConnection, parentPath))
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
            catch
            {
            }

        }

        /// <summary>
        /// Expand nodes for the selected path.
        /// </summary>
        private void ExpandNodes()
        {
            if (!String.IsNullOrEmpty(this.expandPath))
            {
                var dirs = this.expandPath.TrimEnd(PathSeparator[0]).Split(PathSeparator[0]); //TODO: index for separator?
                List<FileTreeNode> currentChildren = this.fileTree.RootNode.Children;
                FileTreeNode lastNode = null;

                foreach (string dir in dirs)
                {
                    FileTreeNode currentNode = null;
                    foreach (FileTreeNode node in currentChildren)
                    {
                        if (node.Name == PathSeparator || string.Equals(node.Name, dir, StringComparison.OrdinalIgnoreCase))
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
                            FillTree(currentNode);
                        }
                        currentChildren = currentNode.Children;
                        lastNode = currentNode;
                    }
                    else
                    {
                        // There's no directory with the provided name
                        // send error message
                        // TODO: should we collapse all nodes?

                        break;
                    }
                }

                if (lastNode != null)
                {
                    this.fileTree.SelectedNode = lastNode;
                }

            }
        }

        /// <summary>
        /// Filter a filename based on the full mask provide.  The full mask may be a collection a masks seperated by semi-colons.
        /// For example: *; *.txt
        /// </summary>
        public bool FilterFile(string fileName, string[] masks)
        {
            for (int index = 0; index < masks.Length; index++)
            {
                if (MatchFileToSubMask(fileName, masks[index]))
                {
                    return true;
                }
            }

            //Exhausted all posibilities and match was not found
            return false;
        }

        /// <summary>
        /// Compares a FileName to the user specified mask using a regular expression
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public bool MatchFileToSubMask(string fileName, string mask)
        {
            char[] delimeter = { '.' };
            String[] fileComponents = fileName.Split(delimeter);
            String[] maskElements = mask.Split(delimeter);
            string tempMask = mask;
            Regex oRE;

            ///If this mask is for all files (*) then there is no reason to continue processing because this
            ///mask indicates ALL FILES.  In the case, just return true.
            if (mask == "*")
            {
                return true;
            }

            ///First see if literal '.' were specified.  If they were substitute them with 
            mask = mask.Replace(".", "\\.");

            ///Now replace any occurence of '*' with '.*'
            mask = mask.Replace("*", ".*");

            ///Finally, replace any occurence of '?' with '.'
            mask = mask.Replace("?", ".");

            ///Perform case insensitive RegEx
            {
                oRE = new Regex(mask, RegexOptions.IgnoreCase);
            }

            if (!oRE.IsMatch(fileName))
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        //private void Refresh()
        //{
        //    this.ExpandNodes(); //this.tboxFilePath.Text
            
            //if (!string.IsNullOrEmpty(this.tboxFilePath.Text.Trim(this.unsupportedChars)))
            //{
            //    this.ProcessPath(this.tboxFilePath.Text);
            //    this.UpdateFilePath(this.treeViewFileTree.SelectedNode);
            //}
        //}
    }
}
