using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Kusto.ServiceLayer;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
	
    internal sealed partial class DatabaseTreeNode : DataSourceTreeNode
    {
    	public DatabaseTreeNode() : base()
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "Database";
    		this.NodeTypeId = NodeTypes.Database;
	    	OnInitialize();
    	}
    }

    internal sealed partial class TableTreeNode : DataSourceTreeNode
    {
    	public TableTreeNode() : base()
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
            currentChildren.Add(new FolderNode {
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

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new DatabaseTreeNode();
            InitializeChild(parent, child, context);
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
            currentChildren.Add(new FolderNode {
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

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new DataSourceTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Database";
            InitializeChild(parent, child, context);
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
            currentChildren.Add(new FolderNode {
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

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new TableTreeNode();
            InitializeChild(parent, child, context);
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
            currentChildren.Add(new FolderNode {
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

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new DataSourceTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Column";
            child.SortPriority = DataSourceTreeNode.NextSortPriority;
            InitializeChild(parent, child, context);
            return child;
        }
    }
}

