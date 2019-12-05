using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.Kusto.ServiceLayer;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.Kusto.ServiceLayer.DataSource;

// TODOKusto: This file is not needed.

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

