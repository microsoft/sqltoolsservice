//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Linq;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes
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
        private string nodePathName;
        private const char PathPartSeperator = '/';

        /// <summary>
        /// Object metadata
        /// </summary>
        public DataSourceObjectMetadata ObjectMetadata { get; set; }

        /// <summary>
        /// The DataSource this tree node is representing
        /// </summary>
        public IDataSource DataSource { get; set; }

        /// <summary>
        /// Constructor with DataSource and DataSourceObjectMetadata 
        /// </summary>
        /// <param name="dataSource"></param>
        /// <param name="objectMetadata"></param>
        protected TreeNode(IDataSource dataSource, DataSourceObjectMetadata objectMetadata)
        {
            DataSource = dataSource;
            ObjectMetadata = objectMetadata;
            NodeValue = objectMetadata.Name;
        }

        private object buildingMetadataLock = new object();

        /// <summary>
        /// Event which tells if MetadataProvider is built fully or not
        /// </summary>
        public object BuildingMetadataLock
        {
            get { return this.buildingMetadataLock; }
        }

        /// <summary>
        /// Value describing this node
        /// </summary>
        public string NodeValue { get; set; }

        /// <summary>
        /// The name of this object as included in its node path
        /// </summary>
        public string NodePathName {
            get
            {
                if (string.IsNullOrEmpty(nodePathName))
                {
                    return NodeValue;
                }
                return nodePathName;
            }
            set
            {
                nodePathName = value;
            }
        }

        /// <summary>
        /// The type of the node - for example Server, Database, Folder, Table
        /// </summary>
        public string NodeType { get; set; }

        /// <summary>
        // True if the node includes system object
        /// </summary>
        public bool IsSystemObject { get; set; }

        /// <summary>
        /// Enum defining the type of the node - for example Server, Database, Folder, Table
        /// </summary>
        public NodeTypes NodeTypeId { get; set; }

        /// <summary>
        /// Node Sub type - for example a key can have type as "Key" and sub type as "PrimaryKey"
        /// </summary>
        public string NodeSubType { get; set; }

        /// <summary>
        /// Error message returned from the engine for a object explorer node failure reason, if any.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Node status - for example login can be disabled/enabled
        /// </summary>
        public string NodeStatus { get; set; }

        /// <summary>
        /// Label to display to the user, describing this node.
        /// If not explicitly set this will fall back to the <see cref="NodeValue"/> but
        /// for many nodes such as the server, the display label will be different
        /// to the value.
        /// </summary>
        protected string Label {
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
                    "{0}{1}{2}", node.NodePathName, string.IsNullOrEmpty(path) ? "" : PathPartSeperator.ToString(), path);
                return true;
            });
            nodePath = path;
        }

        public TreeNode FindNodeByPath(string path, bool expandIfNeeded = false)
        {
            TreeNode nodeForPath = ObjectExplorerUtils.FindNode(this, node =>
            {
                return node.GetNodePath() == path;
            }, nodeToFilter =>
            {
                return path.StartsWith(nodeToFilter.GetNodePath());
            }, expandIfNeeded);

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
                Metadata = this.ObjectMetadata,
                NodeStatus = this.NodeStatus,
                NodeSubType = this.NodeSubType,
                ErrorMessage = this.ErrorMessage
            };
        }

        /// <summary>
        /// Expands this node and returns its children
        /// </summary>
        /// <returns>Children as an IList. This is the raw children collection, not a copy</returns>
        public IList<TreeNode> Expand(string name, CancellationToken cancellationToken)
        {
            // TODO consider why solution explorer has separate Children and Items options
            if (children.IsInitialized)
            {
                return children;
            }
            PopulateChildren(false, name, cancellationToken);
            return children;
        }

        /// <summary>
        /// Expands this node and returns its children
        /// </summary>
        /// <returns>Children as an IList. This is the raw children collection, not a copy</returns>
        public IList<TreeNode> Expand(CancellationToken cancellationToken)
        {
            return Expand(null, cancellationToken);
        }

        /// <summary>
        /// Refresh this node and returns its children
        /// </summary>
        /// <returns>Children as an IList. This is the raw children collection, not a copy</returns>
        public virtual IList<TreeNode> Refresh(CancellationToken cancellationToken)
        {
            // TODO consider why solution explorer has separate Children and Items options
            PopulateChildren(true, null, cancellationToken);
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

        protected void PopulateChildren(bool refresh, string name, CancellationToken cancellationToken)
        {
            Logger.Write(TraceEventType.Verbose, string.Format(CultureInfo.InvariantCulture, "Populating oe node :{0}", this.GetNodePath()));
            Debug.Assert(IsAlwaysLeaf == false);

            QueryContext context = this.GetContextAs<QueryContext>();
            
            if (children.IsPopulating || context == null)
            {
                return;
            }

            children.Clear();
            BeginChildrenInit();

            try
            {
                ErrorMessage = null;
                cancellationToken.ThrowIfCancellationRequested();
                IEnumerable<TreeNode> items = ExpandChildren(this, refresh, name, true, cancellationToken);
                if (items != null)
                {
                    foreach (TreeNode item in items)
                    {
                        children.Add(item);
                        item.Parent = this;
                    }
                }    
            }
            catch (Exception ex)
            {
                string error = string.Format(CultureInfo.InvariantCulture, "Failed populating oe children. error:{0} inner:{1} stacktrace:{2}",
                    ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", ex.StackTrace);
                Logger.Write(TraceEventType.Error, error);
                ErrorMessage = ex.Message;
            }
            finally
            {
                EndChildrenInit();
            }
        }

        protected IEnumerable<TreeNode> ExpandChildren(TreeNode parent, bool refresh, string name,
            bool includeSystemObjects, CancellationToken cancellationToken)
        {
            try
            {
                return OnExpandPopulateNonFolders(parent, refresh, name, cancellationToken);
            }
            catch (Exception ex)
            {
                string error = string.Format(CultureInfo.InvariantCulture,
                    "Failed expanding oe children. parent:{0} error:{1} inner:{2} stacktrace:{3}",
                    parent != null ? parent.GetNodePath() : "", ex.Message,
                    ex.InnerException != null ? ex.InnerException.Message : "", ex.StackTrace);
                Logger.Write(TraceEventType.Error, error);
                throw ex;
            }
        }

        /// <summary>
        /// Populates any non-folder nodes such as specific items in the tree.
        /// </summary>
        /// <param name="allChildren">List to which nodes should be added</param>
        /// <param name="parent">Parent the nodes are being added to</param>
        private List<TreeNode> OnExpandPopulateNonFolders(TreeNode parent, bool refresh, string name, CancellationToken cancellationToken)
        {
            Logger.Write(TraceEventType.Verbose, string.Format(CultureInfo.InvariantCulture, "child factory parent :{0}", parent.GetNodePath()));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var objectMetadataList = Enumerable.Empty<DataSourceObjectMetadata>();

                if (parent.DataSource != null)
                {
                    if (refresh)
                    {
                        parent.DataSource.Refresh(parent.ObjectMetadata);
                    }
                    
                    objectMetadataList = parent.DataSource.GetChildObjects(parent.ObjectMetadata);
                }

                List<TreeNode> allChildren = new List<TreeNode>();
                foreach (var objectMetadata in objectMetadataList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (objectMetadata == null)
                    {
                        Logger.Write(TraceEventType.Error, "kustoMetadata should not be null");
                    }
                    TreeNode childNode = CreateChild(parent, objectMetadata);
                    if (childNode != null)
                    {
                        allChildren.Add(childNode);
                    }
                }

                return allChildren;
            }
            catch (Exception ex)
            {
                string error = string.Format(CultureInfo.InvariantCulture, "Failed getting child objects. parent:{0} error:{1} inner:{2} stacktrace:{3}",
                parent != null ? parent.GetNodePath() : "", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", ex.StackTrace);
                Logger.Write(TraceEventType.Error, error);
                throw ex;
            }
        }

        /// <summary>
        /// The glue between the DataSource and the Object Explorer models. Creates the right tree node for each data source type
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="childMetadata"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        
        private TreeNode CreateChild(TreeNode parent, DataSourceObjectMetadata childMetadata)
        {
            ValidationUtils.IsNotNull(parent, nameof(parent));
            ValidationUtils.IsNotNull(childMetadata, nameof(childMetadata));

            switch(childMetadata.MetadataType)
            {
                 case DataSourceMetadataType.Database:
                    return new DataSourceTreeNode(parent.DataSource, childMetadata) {
                        Parent = parent as ServerNode,
                        NodeType = "Database",
    		            NodeTypeId = NodeTypes.Database
                    };

                case DataSourceMetadataType.Table:
                    return new DataSourceTreeNode(parent.DataSource, childMetadata) {
                        NodeType = "Table",
    		            NodeTypeId = NodeTypes.Table
                    };

                case DataSourceMetadataType.Column:
                    return new DataSourceTreeNode(parent.DataSource, childMetadata) {
                        IsAlwaysLeaf = true,
                        NodeType = "Column",
                        SortPriority = DataSourceTreeNode.NextSortPriority
                    };
                
                case DataSourceMetadataType.Folder:
                    return new DataSourceTreeNode(parent.DataSource, childMetadata)
                    {
                        Parent = parent,
                        NodeType = "Folder",
                        NodeTypeId = NodeTypes.Folder
                    };
                
                case DataSourceMetadataType.Function:
                    return new DataSourceTreeNode(parent.DataSource, childMetadata)
                    {
                        parent = parent,
                        NodeType = "Function",
                        NodeTypeId = NodeTypes.Functions,
                        IsAlwaysLeaf = true,
                    };

                default:
                    throw new ArgumentException($"Unexpected type {childMetadata.MetadataType}.");
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
