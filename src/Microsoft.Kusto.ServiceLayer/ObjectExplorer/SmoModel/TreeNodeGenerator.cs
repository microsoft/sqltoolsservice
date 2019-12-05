using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.Kusto.ServiceLayer;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.Kusto.ServiceLayer.DataSource;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
	
    internal sealed partial class DatabaseTreeNode : DataSourceTreeNode
    {
    	public DatabaseTreeNode(IDataSource dataSource, DataSourceObjectMetadata objectMetadata) 
            : base(dataSource, objectMetadata)
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "Database";
    		this.NodeTypeId = NodeTypes.Database;
	    	OnInitialize();
    	}
    }

    internal sealed partial class TableTreeNode : DataSourceTreeNode
    {
    	public TableTreeNode(IDataSource dataSource, DataSourceObjectMetadata objectMetadata) 
            : base(dataSource, objectMetadata)
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "Table";
    		this.NodeTypeId = NodeTypes.Table;
	    	OnInitialize();
    	}
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ServerChildFactory : DataSourceChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Server" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            DataSourceObjectMetadata folderMetadata = DataSourceFactory.CreateFolderMetadata(parent.ObjectMetadata, SR.SchemaHierarchy_Databases);

            currentChildren.Add(new FolderNode(parent.DataSource, folderMetadata) {
                NodeValue = SR.SchemaHierarchy_Databases,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Databases,
                IsSystemObject = false,
                SortPriority = DataSourceTreeNode.NextSortPriority,
            });
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class DatabasesChildFactory : DataSourceChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Databases" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(DatabaseQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, DataSourceObjectMetadata childMetadata)
        {
            var child = new DatabaseTreeNode(parent as ServerNode, parent.DataSource, childMetadata);
            InitializeChild(parent, child, childMetadata);
            return child;
        }
    }
   
    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class DatabaseChildFactory : DataSourceChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Database" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            DataSourceObjectMetadata folderMetadata = DataSourceFactory.CreateFolderMetadata(parent.ObjectMetadata, SR.SchemaHierarchy_Tables);

            currentChildren.Add(new FolderNode(parent.DataSource, folderMetadata) {
                NodeValue = SR.SchemaHierarchy_Tables,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Tables,
                IsSystemObject = false,
                SortPriority = DataSourceTreeNode.NextSortPriority,
            });            
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new Type[0];           
            }
        }

        public override TreeNode CreateChild(TreeNode parent, DataSourceObjectMetadata childMetadata)
        {
            var child = new DataSourceTreeNode(parent.DataSource, childMetadata);
            child.IsAlwaysLeaf = true;
            child.NodeType = "Database";
            InitializeChild(parent, child, childMetadata);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class TablesChildFactory : DataSourceChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Tables" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            DataSourceObjectMetadata folderMetadata = DataSourceFactory.CreateFolderMetadata(parent.ObjectMetadata, SR.SchemaHierarchy_SystemTables);

            currentChildren.Add(new FolderNode(parent.DataSource, folderMetadata) {
                NodeValue = SR.SchemaHierarchy_SystemTables,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemTables,
                IsSystemObject = true,
                IsMsShippedOwned = true,
                SortPriority = DataSourceTreeNode.NextSortPriority,
            });
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(TableQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, DataSourceObjectMetadata childMetadata)
        {
            var child = new TableTreeNode(parent.DataSource, childMetadata);
            InitializeChild(parent, child, childMetadata);
            return child;
        }
    }
    
    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class TableChildFactory : DataSourceChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Table" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            DataSourceObjectMetadata folderMetadata = DataSourceFactory.CreateFolderMetadata(parent.ObjectMetadata, SR.SchemaHierarchy_Columns);

            currentChildren.Add(new FolderNode(parent.DataSource, folderMetadata) {
                NodeValue = SR.SchemaHierarchy_Columns,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Columns,
                IsSystemObject = false,
                SortPriority = DataSourceTreeNode.NextSortPriority,
            });
        }
    }
    
    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ColumnsChildFactory : DataSourceChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Columns" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(ColumnQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, DataSourceObjectMetadata childMetadata)
        {
            var child = new DataSourceTreeNode(parent.DataSource, childMetadata);
            child.IsAlwaysLeaf = true;
            child.NodeType = "Column";
            child.SortPriority = DataSourceTreeNode.NextSortPriority;
            InitializeChild(parent, child, childMetadata);
            return child;
        }
    }
}

