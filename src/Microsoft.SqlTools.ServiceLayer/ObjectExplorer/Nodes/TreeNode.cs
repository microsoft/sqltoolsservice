//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes
{
    /// <summary>
    /// Base class for elements in the object explorer tree. Provides common methods for tree navigation
    /// and other core functionality
    /// </summary>
    public class TreeNode : IComparable<TreeNode>
    {
        private NodeObservableCollection children = new NodeObservableCollection();
        private TreeNode parent;
        private string nodePath;
        private string label;
        public const char PathPartSeperator = '/';

        /// <summary>
        /// Constructor with no required inputs
        /// </summary>
        public TreeNode()
        {

        }

        /// <summary>
        /// Constructor that accepts a label to identify the node
        /// </summary>
        /// <param name="value">Label identifying the node</param>
        public TreeNode(string value)
        {
            // We intentionally do not valid this being null or empty since
            // some nodes may need to set it 
            NodeValue = value;
        }
        
        /// <summary>
        /// Value describing this node
        /// </summary>
        public string NodeValue { get; set; }


        /// <summary>
        /// Object metadata for smo objects
        /// </summary>
        public ObjectMetadata ObjectMetadata { get; set; }

        /// <summary>
        /// The type of the node - for example Server, Database, Folder, Table
        /// </summary>
        public string NodeType { get; set; }

        /// <summary>
        /// Enum defining the type of the node - for example Server, Database, Folder, Table
        /// </summary>
        public NodeTypes NodeTypeId { get; set; }

        /// <summary>
        /// Label to display to the user, describing this node.
        /// If not explicitly set this will fall back to the <see cref="NodeValue"/> but
        /// for many nodes such as the server, the display label will be different
        /// to the value.
        /// </summary>
        public string Label {
            get
            {
                if(label == null)
                {
                    return NodeValue;
                }
                return label;
            }
            set
            {
                label = value;
            }
        }

        /// <summary>
        /// Is this a leaf node (in which case no children can be generated) or
        /// is it expandable?
        /// </summary>
        public bool IsAlwaysLeaf { get; set; }

        /// <summary>
        /// Message to show if this Node is in an error state. This indicates
        /// that children could be retrieved
        /// </summary>
        public string ErrorStateMessage { get; set; }

        /// <summary>
        /// Parent of this node
        /// </summary>
        public TreeNode Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value;
                // Reset the node path since it's no longer valid
                nodePath = null;
            }
        }
        
        /// <summary>
        /// Path identifying this node: for example a table will be at ["server", "database", "tables", "tableName"].
        /// This enables rapid navigation of the tree without the need for a global registry of elements.
        /// The path functions as a unique ID and is used to disambiguate the node when sending requests for expansion.
        /// A common ID is needed since processes do not share address space and need a unique identifier
        /// </summary>
        public string GetNodePath()
        {
            if (nodePath == null)
            {
                GenerateNodePath();
            }
            return nodePath;
        }

        private void GenerateNodePath()
        {
            string path = "";
            ObjectExplorerUtils.VisitChildAndParents(this, node =>
            {
                if (string.IsNullOrEmpty(node.NodeValue))
                {
                    // Hit a node with no NodeValue. This indicates we need to stop traversing
                    return false;
                }
                // Otherwise add this value to the beginning of the path and keep iterating up
                path = string.Format(CultureInfo.InvariantCulture, 
                    "{0}{1}{2}", node.NodeValue, string.IsNullOrEmpty(path) ? "" : PathPartSeperator.ToString(), path);
                return true;
            });
            nodePath = path;
        }

        public TreeNode FindNodeByPath(string path)
        {
            TreeNode nodeForPath = ObjectExplorerUtils.FindNode(this, node =>
            {
                return node.GetNodePath() == path;
            }, nodeToFilter =>
            {
                return path.StartsWith(nodeToFilter.GetNodePath());
            });

            return nodeForPath;
        }

        /// <summary>
        /// Converts to a <see cref="NodeInfo"/> object for serialization with just the relevant properties 
        /// needed to identify the node
        /// </summary>
        /// <returns></returns>
        public NodeInfo ToNodeInfo()
        {
            return new NodeInfo()
            {
                IsLeaf = this.IsAlwaysLeaf,
                Label = this.Label,
                NodePath = this.GetNodePath(),
                NodeType = this.NodeType,
                Metadata = this.ObjectMetadata
            };
        }

        /// <summary>
        /// Expands this node and returns its children
        /// </summary>
        /// <returns>Children as an IList. This is the raw children collection, not a copy</returns>
        public IList<TreeNode> Expand()
        {
            // TODO consider why solution explorer has separate Children and Items options
            if (children.IsInitialized)
            {
                return children;
            }
            PopulateChildren();
            return children;
        }

        /// <summary>
        /// Gets a readonly view of the currently defined children for this node. 
        /// This does not expand the node at all
        /// Since the tree needs to keep track of parent relationships, directly 
        /// adding to the list is not supported. 
        /// </summary>
        /// <returns><see cref="IList{TreeNode}"/> containing all children for this node</returns>
        public IList<TreeNode> GetChildren()
        {
            return new ReadOnlyCollection<TreeNode>(children);
        }

        /// <summary>
        /// Adds a child to the list of children under this node
        /// </summary>
        /// <param name="newChild"><see cref="TreeNode"/></param>
        public void AddChild(TreeNode newChild)
        {
            Validate.IsNotNull(nameof(newChild), newChild);
            children.Add(newChild);
            newChild.Parent = this;
        }
        
        /// <summary>
        /// Optional context to help with lookup of children
        /// </summary>
        public virtual object GetContext()
        {
            return null;
        }

        /// <summary>
        /// Helper method to convert context to expected format
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <returns>context as expected type of null if it doesn't match</returns>
        public T GetContextAs<T>()
            where T : class
        {
            return GetContext() as T;
        }

        public T ParentAs<T>()
            where T : TreeNode
        {
            return Parent as T;
        }
        
        protected void PopulateChildren()
        {
            Debug.Assert(IsAlwaysLeaf == false);

            SmoQueryContext context = this.GetContextAs<SmoQueryContext>();

            if (children.IsPopulating || context == null)
                return;

            children.Clear();
            BeginChildrenInit();

            try
            {
                IEnumerable<ChildFactory> childFactories = context.GetObjectExplorerService().GetApplicableChildFactories(this);
                if (childFactories != null)
                {
                    foreach (var factory in childFactories)
                    {
                        IEnumerable<TreeNode> items = factory.Expand(this);
                        if (items != null)
                        {
                            foreach (TreeNode item in items)
                            {
                                children.Add(item);
                                item.Parent = this;

                            }
                        }
                    }
                }
            }
            finally
            {
                EndChildrenInit();
            }
        }

        public void BeginChildrenInit()
        {
            children.BeginInit();
        }

        public void EndChildrenInit()
        {
            children.EndInit();
            // TODO consider use of deferred children and if it's necessary
            // children.EndInit(this, ref deferredChildren);
        }


        /// <summary>
        /// Sort Priority to help when ordering elements in the tree
        /// </summary>
        public int? SortPriority { get; set; }

        protected virtual int CompareSamePriorities(TreeNode thisItem, TreeNode otherItem)
        {
            return string.Compare(thisItem.NodeValue, otherItem.NodeValue, StringComparison.OrdinalIgnoreCase);
        }
        
        public int CompareTo(TreeNode other)
        {

            if (!this.SortPriority.HasValue &&
                !other.SortPriority.HasValue)
            {
                return CompareSamePriorities(this, other);
            }

            if (this.SortPriority.HasValue &&
                !other.SortPriority.HasValue)
            {
                return -1; // this is above other
            }
            if (!this.SortPriority.HasValue)
            {
                return 1; // this is below other
            }

            // Both have sort priority
            int priDiff = this.SortPriority.Value - other.SortPriority.Value;
            if (priDiff < 0)
                return -1; // this is below other
            if (priDiff == 0)
                return 0;
            return 1;
        }
    }
}
