using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Kusto.ServiceLayer;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.SmoModel
{
	
    internal sealed partial class DatabaseTreeNode : SmoTreeNode
    {
    	public DatabaseTreeNode() : base()
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "Database";
    		this.NodeTypeId = NodeTypes.Database;
	    	OnInitialize();
    	}
    }

    internal sealed partial class TableTreeNode : SmoTreeNode
    {
    	public TableTreeNode() : base()
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "Table";
    		this.NodeTypeId = NodeTypes.Table;
	    	OnInitialize();
    	}
    }

    internal sealed partial class ViewTreeNode : SmoTreeNode
    {
    	public ViewTreeNode() : base()
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "View";
    		this.NodeTypeId = NodeTypes.View;
	    	OnInitialize();
    	}
    }

    internal sealed partial class UserDefinedTableTypeTreeNode : SmoTreeNode
    {
    	public UserDefinedTableTypeTreeNode() : base()
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "UserDefinedTableType";
    		this.NodeTypeId = NodeTypes.UserDefinedTableType;
	    	OnInitialize();
    	}
    }

    internal sealed partial class StoredProcedureTreeNode : SmoTreeNode
    {
    	public StoredProcedureTreeNode() : base()
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "StoredProcedure";
    		this.NodeTypeId = NodeTypes.StoredProcedure;
	    	OnInitialize();
    	}
    }

    internal sealed partial class TableValuedFunctionTreeNode : SmoTreeNode
    {
    	public TableValuedFunctionTreeNode() : base()
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "TableValuedFunction";
    		this.NodeTypeId = NodeTypes.TableValuedFunction;
	    	OnInitialize();
    	}
    }

    internal sealed partial class ScalarValuedFunctionTreeNode : SmoTreeNode
    {
    	public ScalarValuedFunctionTreeNode() : base()
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "ScalarValuedFunction";
    		this.NodeTypeId = NodeTypes.ScalarValuedFunction;
	    	OnInitialize();
    	}
    }

    internal sealed partial class AggregateFunctionTreeNode : SmoTreeNode
    {
    	public AggregateFunctionTreeNode() : base()
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "AggregateFunction";
    		this.NodeTypeId = NodeTypes.AggregateFunction;
	    	OnInitialize();
    	}
    }

    internal sealed partial class FileGroupTreeNode : SmoTreeNode
    {
    	public FileGroupTreeNode() : base()
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "FileGroup";
    		this.NodeTypeId = NodeTypes.FileGroup;
	    	OnInitialize();
    	}
    }

    internal sealed partial class ExternalTableTreeNode : SmoTreeNode
    {
    	public ExternalTableTreeNode() : base()
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "ExternalTable";
    		this.NodeTypeId = NodeTypes.ExternalTable;
	    	OnInitialize();
    	}
    }

    internal sealed partial class ExternalResourceTreeNode : SmoTreeNode
    {
    	public ExternalResourceTreeNode() : base()
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "ExternalResource";
    		this.NodeTypeId = NodeTypes.ExternalResource;
	    	OnInitialize();
    	}
    }

    internal sealed partial class HistoryTableTreeNode : SmoTreeNode
    {
    	public HistoryTableTreeNode() : base()
    	{
    		NodeValue = string.Empty;
    		this.NodeType = "HistoryTable";
    		this.NodeTypeId = NodeTypes.HistoryTable;
	    	OnInitialize();
    	}
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ServerChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Server" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Databases,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Databases,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Security,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ServerLevelSecurity,
                IsSystemObject = false,
                ValidFor = ValidForFlag.All,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ServerObjects,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ServerLevelServerObjects,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class DatabasesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Databases" }; }

        public override IEnumerable<NodeFilter> Filters
        {
           get
           {
                var filters = new List<NodeFilter>();
                filters.Add(new NodeFilter
                {
                   Property = "IsSystemObject",
                   Type = typeof(bool),
                   Values = new List<object> { 0 },
                });
                return filters;
           }
        }

        public override IEnumerable<NodeSmoProperty> SmoProperties
        {
           get
           {
                var properties = new List<NodeSmoProperty>();
                properties.Add(new NodeSmoProperty
                {
                   Name = "Status",
                   ValidFor = ValidForFlag.All
                });
                return properties;
           }
        }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemDatabases,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemDatabases,
                IsSystemObject = true,
                ValidFor = ValidForFlag.All,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlDatabaseQuerier), };
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
    internal partial class ServerLevelServerObjectsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ServerLevelServerObjects" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Endpoints,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ServerLevelEndpoints,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_LinkedServers,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ServerLevelLinkedServers,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ServerTriggers,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ServerLevelServerTriggers,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ErrorMessages,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ServerLevelErrorMessages,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
        }

        internal override Type[] ChildQuerierTypes { get {return null;} }


        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            return null;
        }
    }
   
    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class DatabaseChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Database" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Tables,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Tables,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Views,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Views,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Synonyms,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Synonyms,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2005|ValidForFlag.Sql2008|ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016|ValidForFlag.Sql2017|ValidForFlag.AzureV12,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Programmability,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Programmability,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ExternalResources,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ExternalResources,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2016|ValidForFlag.Sql2017|ValidForFlag.AzureV12,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ServiceBroker,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ServiceBroker,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Storage,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Storage,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AzureV12|ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Security,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Security,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new Type[0];           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Database";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class TablesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Tables" }; }

        public override IEnumerable<NodeFilter> Filters
        {
           get
           {
                var filters = new List<NodeFilter>();
                filters.Add(new NodeFilter
                {
                   Property = "IsSystemObject",
                   Type = typeof(bool),
                   Values = new List<object> { 0 },
                });
                filters.Add(new NodeFilter
                {
                   Property = "TemporalType",
                   Type = typeof(Enum),
                   ValidFor = ValidForFlag.Sql2016|ValidForFlag.Sql2017|ValidForFlag.AzureV12,
                   Values = new List<object>
                   {
                      { TableTemporalType.None },
                      { TableTemporalType.SystemVersioned }
                   }
                });
                return filters;
           }
        }

        public override IEnumerable<NodeSmoProperty> SmoProperties
        {
           get
           {
                var properties = new List<NodeSmoProperty>();
                properties.Add(new NodeSmoProperty
                {
                   Name = "IsFileTable",
                   ValidFor = ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016|ValidForFlag.Sql2017
                });
                properties.Add(new NodeSmoProperty
                {
                   Name = "IsSystemVersioned",
                   ValidFor = ValidForFlag.Sql2016|ValidForFlag.Sql2017|ValidForFlag.AzureV12
                });
                properties.Add(new NodeSmoProperty
                {
                   Name = "TemporalType",
                   ValidFor = ValidForFlag.Sql2016|ValidForFlag.Sql2017|ValidForFlag.AzureV12
                });
                properties.Add(new NodeSmoProperty
                {
                   Name = "IsExternal",
                   ValidFor = ValidForFlag.Sql2016|ValidForFlag.Sql2017|ValidForFlag.AzureV12
                });
                return properties;
           }
        }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemTables,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemTables,
                IsSystemObject = true,
                IsMsShippedOwned = true,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlTableQuerier), };
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
    internal partial class TableChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Table" }; }

        public override IEnumerable<NodeFilter> Filters
        {
           get
           {
                var filters = new List<NodeFilter>();
                filters.Add(new NodeFilter
                {
                   Property = "TemporalType",
                   Type = typeof(Enum),
                   TypeToReverse = typeof(SqlHistoryTableQuerier),
                   ValidFor = ValidForFlag.Sql2016|ValidForFlag.Sql2017|ValidForFlag.AzureV12,
                   Values = new List<object>
                   {
                      { TableTemporalType.HistoryTable }
                   }
                });
                return filters;
           }
        }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Columns,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Columns,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Keys,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Keys,
                IsSystemObject = false,
                ValidFor = ValidForFlag.NotSqlDw,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Constraints,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Constraints,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Triggers,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Triggers,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2005|ValidForFlag.Sql2008|ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016|ValidForFlag.Sql2017|ValidForFlag.AzureV12,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Indexes,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Indexes,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Statistics,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Statistics,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlTableQuerier), typeof(SqlHistoryTableQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new HistoryTableTreeNode();
            InitializeChild(parent, child, context);
            return child;
        }
    }
    
    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ColumnsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Columns" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlColumnQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Column";
            child.SortPriority = SmoTreeNode.NextSortPriority;
            InitializeChild(parent, child, context);
            return child;
        }
    }

    

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class FunctionsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Functions" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemFunctions,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemFunctions,
                IsSystemObject = true,
                IsMsShippedOwned = true,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_TableValuedFunctions,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.TableValuedFunctions,
                IsSystemObject = false,
                ValidFor = ValidForFlag.NotSqlDw,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ScalarValuedFunctions,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ScalarValuedFunctions,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_AggregateFunctions,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.AggregateFunctions,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AzureV12|ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
        }

        internal override Type[] ChildQuerierTypes { get {return null;} }


        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            return null;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class UserDefinedTypesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "UserDefinedTypes" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlUserDefinedTypeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "UserDefinedType";
            InitializeChild(parent, child, context);
            return child;
        }
    }
}

