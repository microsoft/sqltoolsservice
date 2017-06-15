using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
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
    internal partial class ServerLevelSecurityChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ServerLevelSecurity" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_LinkedServerLogins,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ServerLevelLinkedServerLogins,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Logins,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ServerLevelLogins,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ServerRoles,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ServerLevelServerRoles,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Credentials,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ServerLevelCredentials,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_CryptographicProviders,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ServerLevelCryptographicProviders,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ServerAudits,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ServerLevelServerAudits,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ServerAuditSpecifications,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ServerLevelServerAuditSpecifications,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AllOnPrem,
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
    internal partial class SystemDatabasesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemDatabases" }; }

        public override IEnumerable<NodeFilter> Filters
        {
           get
           {
                var filters = new List<NodeFilter>();
                filters.Add(new NodeFilter
                {
                   Property = "IsSystemObject",
                   Type = typeof(bool),
                   Values = new List<object> { 1 },
                });
                return filters;
           }
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
    internal partial class ServerLevelLinkedServerLoginsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ServerLevelLinkedServerLogins" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlLinkedServerLoginQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ServerLevelLinkedServerLogin";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ServerLevelLoginsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ServerLevelLogins" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlLoginQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ServerLevelLogin";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ServerLevelServerRolesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ServerLevelServerRoles" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlServerRoleQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ServerLevelServerRole";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ServerLevelCredentialsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ServerLevelCredentials" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlCredentialQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ServerLevelCredential";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ServerLevelCryptographicProvidersChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ServerLevelCryptographicProviders" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlCryptographicProviderQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ServerLevelCryptographicProvider";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ServerLevelServerAuditsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ServerLevelServerAudits" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlServerAuditQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ServerLevelServerAudit";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ServerLevelServerAuditSpecificationsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ServerLevelServerAuditSpecifications" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlServerAuditSpecificationQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ServerLevelServerAuditSpecification";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ServerLevelEndpointsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ServerLevelEndpoints" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlEndpointQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ServerLevelEndpoint";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ServerLevelLinkedServersChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ServerLevelLinkedServers" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlLinkedServerQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ServerLevelLinkedServer";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ServerLevelServerTriggersChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ServerLevelServerTriggers" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlServerDdlTriggerQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ServerLevelServerTrigger";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ServerLevelErrorMessagesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ServerLevelErrorMessages" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlErrorMessageQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ServerLevelErrorMessage";
            InitializeChild(parent, child, context);
            return child;
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
                ValidFor = ValidForFlag.Sql2005|ValidForFlag.Sql2008|ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016|ValidForFlag.AzureV12,
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
                ValidFor = ValidForFlag.Sql2016|ValidForFlag.AzureV12,
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
                   ValidFor = ValidForFlag.Sql2016|ValidForFlag.AzureV12,
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
                   Name = "IsSystemVersioned",
                   ValidFor = ValidForFlag.Sql2016|ValidForFlag.AzureV12
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
    internal partial class ViewsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Views" }; }

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

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemViews,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemViews,
                IsSystemObject = true,
                IsMsShippedOwned = true,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlViewQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new ViewTreeNode();
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SynonymsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Synonyms" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlSynonymQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Synonym";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ProgrammabilityChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Programmability" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_StoredProcedures,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.StoredProcedures,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Functions,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Functions,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_DatabaseTriggers,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.DatabaseTriggers,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2005|ValidForFlag.Sql2008|ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016|ValidForFlag.AzureV12,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Assemblies,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Assemblies,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AzureV12|ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Types,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Types,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Sequences,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Sequences,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016|ValidForFlag.AzureV12,
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
    internal partial class ExternalResourcesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ExternalResources" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ExternalDataSources,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ExternalDataSources,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2016|ValidForFlag.AzureV12,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ExternalFileFormats,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ExternalFileFormats,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2016,
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
    internal partial class ServiceBrokerChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ServiceBroker" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_MessageTypes,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.MessageTypes,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Contracts,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Contracts,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Queues,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Queues,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Services,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Services,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_RemoteServiceBindings,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.RemoteServiceBindings,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_BrokerPriorities,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.BrokerPriorities,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2008|ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016,
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
    internal partial class StorageChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Storage" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_FileGroups,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.FileGroups,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_FullTextCatalogs,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.FullTextCatalogs,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_FullTextStopLists,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.FullTextStopLists,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2008|ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016|ValidForFlag.AzureV12,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_LogFiles,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SqlLogFiles,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_PartitionFunctions,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.PartitionFunctions,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_PartitionSchemes,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.PartitionSchemes,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SearchPropertyLists,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SearchPropertyLists,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016|ValidForFlag.AzureV12,
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
    internal partial class SecurityChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Security" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Users,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Users,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Roles,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Roles,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Schemas,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Schemas,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_AsymmetricKeys,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.AsymmetricKeys,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Certificates,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Certificates,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SymmetricKeys,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SymmetricKeys,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_DatabaseScopedCredentials,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.DatabaseScopedCredentials,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2016|ValidForFlag.AzureV12,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_DatabaseEncryptionKeys,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.DatabaseEncryptionKeys,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2008|ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_MasterKeys,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.MasterKeys,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_DatabaseAuditSpecifications,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.DatabaseAuditSpecifications,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2008|ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SecurityPolicies,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SecurityPolicies,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2016|ValidForFlag.AzureV12,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_AlwaysEncryptedKeys,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.AlwaysEncryptedKeys,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2016|ValidForFlag.AzureV12,
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
    internal partial class SystemTablesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemTables" }; }

        public override IEnumerable<NodeFilter> Filters
        {
           get
           {
                var filters = new List<NodeFilter>();
                filters.Add(new NodeFilter
                {
                   Property = "IsSystemObject",
                   Type = typeof(bool),
                   Values = new List<object> { 1 },
                });
                return filters;
           }
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
                   ValidFor = ValidForFlag.Sql2016|ValidForFlag.AzureV12,
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
                ValidFor = ValidForFlag.Sql2005|ValidForFlag.Sql2008|ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016|ValidForFlag.AzureV12,
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
    internal partial class HistoryTableChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "HistoryTable" }; }

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
                NodeValue = SR.SchemaHierarchy_Constraints,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Constraints,
                IsSystemObject = false,
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
              return new Type[0];           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Table";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ExternalTableChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ExternalTable" }; }

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
              return new [] { typeof(SqlTableQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Table";
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
    internal partial class KeysChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Keys" }; }

        public override IEnumerable<NodeFilter> Filters
        {
           get
           {
                var filters = new List<NodeFilter>();
                filters.Add(new NodeFilter
                {
                   Property = "IndexKeyType",
                   Type = typeof(Enum),
                   TypeToReverse = typeof(SqlIndexQuerier),
                   ValidFor = ValidForFlag.Sql2016|ValidForFlag.AzureV12,
                   Values = new List<object>
                   {
                      { IndexKeyType.DriPrimaryKey },
                      { IndexKeyType.DriUniqueKey }
                   }
                });
                return filters;
           }
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlIndexQuerier), typeof(SqlForeignKeyConstraintQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Key";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ConstraintsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Constraints" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlDefaultConstraintQuerier), typeof(SqlCheckQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Constraint";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class TriggersChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Triggers" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlDmlTriggerQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Trigger";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class IndexesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Indexes" }; }

        public override IEnumerable<NodeFilter> Filters
        {
           get
           {
                var filters = new List<NodeFilter>();
                filters.Add(new NodeFilter
                {
                   Property = "IndexKeyType",
                   Type = typeof(Enum),
                   TypeToReverse = typeof(SqlIndexQuerier),
                   ValidFor = ValidForFlag.Sql2016|ValidForFlag.AzureV12,
                   Values = new List<object>
                   {
                      { IndexKeyType.None }
                   }
                });
                return filters;
           }
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlIndexQuerier), typeof(SqlFullTextIndexQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Index";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class StatisticsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Statistics" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlStatisticQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Statistic";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemViewsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemViews" }; }

        public override IEnumerable<NodeFilter> Filters
        {
           get
           {
                var filters = new List<NodeFilter>();
                filters.Add(new NodeFilter
                {
                   Property = "IsSystemObject",
                   Type = typeof(bool),
                   Values = new List<object> { 1 },
                });
                return filters;
           }
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlViewQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new ViewTreeNode();
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ViewChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "View" }; }

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
                NodeValue = SR.SchemaHierarchy_Triggers,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.Triggers,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2005|ValidForFlag.Sql2008|ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016|ValidForFlag.AzureV12,
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
              return new Type[0];           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "View";
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
    internal partial class SystemFunctionsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemFunctions" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_TableValuedFunctions,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemTableValuedFunctions,
                IsSystemObject = true,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ScalarValuedFunctions,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemScalarValuedFunctions,
                IsSystemObject = true,
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
            var child = new TableValuedFunctionTreeNode();
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class DatabaseTriggersChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "DatabaseTriggers" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlDatabaseDdlTriggerQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "DatabaseTrigger";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class AssembliesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Assemblies" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlAssemblyQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Assembly";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class TypesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Types" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemDataTypes,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemDataTypes,
                IsSystemObject = true,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_UserDefinedDataTypes,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.UserDefinedDataTypes,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2005|ValidForFlag.Sql2008|ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016|ValidForFlag.AzureV12,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_UserDefinedTableTypes,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.UserDefinedTableTypes,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2008|ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016|ValidForFlag.AzureV12,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_UserDefinedTypes,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.UserDefinedTypes,
                IsSystemObject = false,
                ValidFor = ValidForFlag.AzureV12|ValidForFlag.AllOnPrem,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_XMLSchemaCollections,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.XmlSchemaCollections,
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
    internal partial class SequencesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Sequences" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlSequenceQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Sequence";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemDataTypesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemDataTypes" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemExactNumerics,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemExactNumerics,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemApproximateNumerics,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemApproximateNumerics,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemDateAndTime,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemDateAndTimes,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemCharacterStrings,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemCharacterStrings,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemUnicodeCharacterStrings,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemUnicodeCharacterStrings,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemBinaryStrings,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemBinaryStrings,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemOtherDataTypes,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemOtherDataTypes,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemCLRDataTypes,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemClrDataTypes,
                IsSystemObject = false,
                ValidFor = ValidForFlag.All,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemSpatialDataTypes,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemSpatialDataTypes,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2008|ValidForFlag.Sql2012|ValidForFlag.Sql2014|ValidForFlag.Sql2016|ValidForFlag.AzureV12,
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
    internal partial class UserDefinedDataTypesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "UserDefinedDataTypes" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlUserDefinedDataTypeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "UserDefinedDataType";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class UserDefinedTableTypesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "UserDefinedTableTypes" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlUserDefinedTableTypeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new UserDefinedTableTypeTreeNode();
            InitializeChild(parent, child, context);
            return child;
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

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class XmlSchemaCollectionsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "XmlSchemaCollections" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlXmlSchemaCollectionQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "XmlSchemaCollection";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class UserDefinedTableTypeChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "UserDefinedTableType" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Columns,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.UserDefinedTableTypeColumns,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Keys,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.UserDefinedTableTypeKeys,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Constraints,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.UserDefinedTableTypeConstraints,
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
            child.NodeType = "UserDefinedTableType";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class UserDefinedTableTypeColumnsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "UserDefinedTableTypeColumns" }; }

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
            child.NodeType = "UserDefinedTableTypeColumn";
            child.SortPriority = SmoTreeNode.NextSortPriority;
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class UserDefinedTableTypeKeysChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "UserDefinedTableTypeKeys" }; }

        public override IEnumerable<NodeFilter> Filters
        {
           get
           {
                var filters = new List<NodeFilter>();
                filters.Add(new NodeFilter
                {
                   Property = "IndexKeyType",
                   Type = typeof(Enum),
                   TypeToReverse = typeof(SqlIndexQuerier),
                   ValidFor = ValidForFlag.Sql2016|ValidForFlag.AzureV12,
                   Values = new List<object>
                   {
                      { IndexKeyType.DriPrimaryKey },
                      { IndexKeyType.DriUniqueKey }
                   }
                });
                return filters;
           }
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlIndexQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "UserDefinedTableTypeKey";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class UserDefinedTableTypeConstraintsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "UserDefinedTableTypeConstraints" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlDefaultConstraintQuerier), typeof(SqlCheckQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "UserDefinedTableTypeConstraint";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemExactNumericsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemExactNumerics" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlBuiltInTypeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SystemExactNumeric";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemApproximateNumericsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemApproximateNumerics" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlBuiltInTypeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SystemApproximateNumeric";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemDateAndTimesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemDateAndTimes" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlBuiltInTypeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SystemDateAndTime";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemCharacterStringsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemCharacterStrings" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlBuiltInTypeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SystemCharacterString";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemUnicodeCharacterStringsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemUnicodeCharacterStrings" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlBuiltInTypeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SystemUnicodeCharacterString";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemBinaryStringsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemBinaryStrings" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlBuiltInTypeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SystemBinaryString";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemOtherDataTypesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemOtherDataTypes" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlBuiltInTypeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SystemOtherDataType";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemClrDataTypesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemClrDataTypes" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlBuiltInTypeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SystemClrDataType";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemSpatialDataTypesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemSpatialDataTypes" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlBuiltInTypeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SystemSpatialDataType";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ExternalDataSourcesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ExternalDataSources" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlExternalDataSourceQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ExternalDataSource";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ExternalFileFormatsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ExternalFileFormats" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlExternalFileFormatQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ExternalFileFormat";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class StoredProceduresChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "StoredProcedures" }; }

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

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemStoredProcedures,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemStoredProcedures,
                IsSystemObject = true,
                IsMsShippedOwned = true,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlProcedureQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new StoredProcedureTreeNode();
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemStoredProceduresChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemStoredProcedures" }; }

        public override IEnumerable<NodeFilter> Filters
        {
           get
           {
                var filters = new List<NodeFilter>();
                filters.Add(new NodeFilter
                {
                   Property = "IsSystemObject",
                   Type = typeof(bool),
                   Values = new List<object> { 1 },
                });
                return filters;
           }
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlProcedureQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new StoredProcedureTreeNode();
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class StoredProcedureChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "StoredProcedure" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Parameters,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.StoredProcedureParameters,
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
            child.NodeType = "StoredProcedure";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class StoredProcedureParametersChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "StoredProcedureParameters" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlSubroutineParameterQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "StoredProcedureParameter";
            child.SortPriority = SmoTreeNode.NextSortPriority;
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class TableValuedFunctionsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "TableValuedFunctions" }; }

        public override IEnumerable<NodeFilter> Filters
        {
           get
           {
                var filters = new List<NodeFilter>();
                filters.Add(new NodeFilter
                {
                   Property = "FunctionType",
                   Type = typeof(Enum),
                   Values = new List<object>
                   {
                      { UserDefinedFunctionType.Table }
                   }
                });
                filters.Add(new NodeFilter
                {
                   Property = "IsSystemObject",
                   Type = typeof(bool),
                   Values = new List<object> { 0 },
                });
                return filters;
           }
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlUserDefinedFunctionQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new TableValuedFunctionTreeNode();
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemTableValuedFunctionsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemTableValuedFunctions" }; }

        public override IEnumerable<NodeFilter> Filters
        {
           get
           {
                var filters = new List<NodeFilter>();
                filters.Add(new NodeFilter
                {
                   Property = "FunctionType",
                   Type = typeof(Enum),
                   Values = new List<object>
                   {
                      { UserDefinedFunctionType.Table }
                   }
                });
                filters.Add(new NodeFilter
                {
                   Property = "IsSystemObject",
                   Type = typeof(bool),
                   Values = new List<object> { 1 },
                });
                return filters;
           }
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlUserDefinedFunctionQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new TableValuedFunctionTreeNode();
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class TableValuedFunctionChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "TableValuedFunction" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Parameters,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.TableValuedFunctionParameters,
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
            child.NodeType = "TableValuedFunction";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class TableValuedFunctionParametersChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "TableValuedFunctionParameters" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlSubroutineParameterQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "TableValuedFunctionParameter";
            child.SortPriority = SmoTreeNode.NextSortPriority;
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ScalarValuedFunctionsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ScalarValuedFunctions" }; }

        public override IEnumerable<NodeFilter> Filters
        {
           get
           {
                var filters = new List<NodeFilter>();
                filters.Add(new NodeFilter
                {
                   Property = "FunctionType",
                   Type = typeof(Enum),
                   ValidFor = ValidForFlag.Sql2016|ValidForFlag.AzureV12,
                   Values = new List<object>
                   {
                      { UserDefinedFunctionType.Scalar }
                   }
                });
                filters.Add(new NodeFilter
                {
                   Property = "IsSystemObject",
                   Type = typeof(bool),
                   Values = new List<object> { 0 },
                });
                return filters;
           }
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlUserDefinedFunctionQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new ScalarValuedFunctionTreeNode();
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemScalarValuedFunctionsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemScalarValuedFunctions" }; }

        public override IEnumerable<NodeFilter> Filters
        {
           get
           {
                var filters = new List<NodeFilter>();
                filters.Add(new NodeFilter
                {
                   Property = "FunctionType",
                   Type = typeof(Enum),
                   Values = new List<object>
                   {
                      { UserDefinedFunctionType.Scalar }
                   }
                });
                filters.Add(new NodeFilter
                {
                   Property = "IsSystemObject",
                   Type = typeof(bool),
                   Values = new List<object> { 1 },
                });
                return filters;
           }
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlUserDefinedFunctionQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new ScalarValuedFunctionTreeNode();
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ScalarValuedFunctionChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ScalarValuedFunction" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Parameters,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ScalarValuedFunctionParameters,
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
            child.NodeType = "ScalarValuedFunction";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ScalarValuedFunctionParametersChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ScalarValuedFunctionParameters" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlSubroutineParameterQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ScalarValuedFunctionParameter";
            child.SortPriority = SmoTreeNode.NextSortPriority;
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class AggregateFunctionsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "AggregateFunctions" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlUserDefinedAggregateQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new AggregateFunctionTreeNode();
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class AggregateFunctionChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "AggregateFunction" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_Parameters,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.AggregateFunctionParameters,
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
            child.NodeType = "AggregateFunction";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class AggregateFunctionParametersChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "AggregateFunctionParameters" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlSubroutineParameterQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "AggregateFunctionParameter";
            child.SortPriority = SmoTreeNode.NextSortPriority;
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class RemoteServiceBindingsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "RemoteServiceBindings" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlRemoteServiceBindingQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "RemoteServiceBinding";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class BrokerPrioritiesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "BrokerPriorities" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlBrokerPriorityQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "BrokerPriority";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class FileGroupsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "FileGroups" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlFileGroupQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new FileGroupTreeNode();
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class FullTextCatalogsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "FullTextCatalogs" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlFullTextCatalogQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "FullTextCatalog";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class FullTextStopListsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "FullTextStopLists" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlFullTextStopListQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "FullTextStopList";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SqlLogFilesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SqlLogFiles" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlFileQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SqlLogFile";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class PartitionFunctionsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "PartitionFunctions" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlPartitionFunctionQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "PartitionFunction";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class PartitionSchemesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "PartitionSchemes" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlPartitionSchemeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "PartitionScheme";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SearchPropertyListsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SearchPropertyLists" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlSearchPropertyListQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SearchPropertyList";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class FileGroupChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "FileGroup" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_FilegroupFiles,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.FileGroupFiles,
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
    internal partial class FileGroupFilesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "FileGroupFiles" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlFileQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "FileGroupFile";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class UsersChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Users" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlUserQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "User";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class RolesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Roles" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_DatabaseRoles,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.DatabaseRoles,
                IsSystemObject = false,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ApplicationRoles,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ApplicationRoles,
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
    internal partial class SchemasChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Schemas" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlSchemaQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Schema";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class AsymmetricKeysChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "AsymmetricKeys" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlAsymmetricKeyQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "AsymmetricKey";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class CertificatesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Certificates" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlCertificateQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Certificate";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SymmetricKeysChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SymmetricKeys" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlSymmetricKeyQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SymmetricKey";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class DatabaseEncryptionKeysChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "DatabaseEncryptionKeys" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlDatabaseEncryptionKeyQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "DatabaseEncryptionKey";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class MasterKeysChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "MasterKeys" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlMasterKeyQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "MasterKey";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class DatabaseAuditSpecificationsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "DatabaseAuditSpecifications" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlDatabaseAuditSpecificationQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "DatabaseAuditSpecification";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SecurityPoliciesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SecurityPolicies" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlSecurityPolicyQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SecurityPolicie";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class DatabaseScopedCredentialsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "DatabaseScopedCredentials" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlDatabaseCredentialQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "DatabaseScopedCredential";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class AlwaysEncryptedKeysChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "AlwaysEncryptedKeys" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ColumnMasterKeys,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ColumnMasterKeys,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2016|ValidForFlag.AzureV12,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_ColumnEncryptionKeys,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.ColumnEncryptionKeys,
                IsSystemObject = false,
                ValidFor = ValidForFlag.Sql2016|ValidForFlag.AzureV12,
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
    internal partial class DatabaseRolesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "DatabaseRoles" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlRoleQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "DatabaseRole";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ApplicationRolesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ApplicationRoles" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlApplicationRoleQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ApplicationRole";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ColumnMasterKeysChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ColumnMasterKeys" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlColumnMasterKeyQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ColumnMasterKey";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ColumnEncryptionKeysChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "ColumnEncryptionKeys" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlColumnEncryptionKeyQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "ColumnEncryptionKey";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class MessageTypesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "MessageTypes" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemMessageTypes,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemMessageTypes,
                IsSystemObject = true,
                IsMsShippedOwned = true,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlMessageTypeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "MessageType";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemMessageTypesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemMessageTypes" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlMessageTypeQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SystemMessageType";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ContractsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Contracts" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemContracts,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemContracts,
                IsSystemObject = true,
                IsMsShippedOwned = true,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlContractQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Contract";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemContractsChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemContracts" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlContractQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SystemContract";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class QueuesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Queues" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemQueues,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemQueues,
                IsSystemObject = true,
                IsMsShippedOwned = true,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlQueueQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Queue";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemQueuesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemQueues" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlQueueQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SystemQueue";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class ServicesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "Services" }; }

        protected override void OnExpandPopulateFolders(IList<TreeNode> currentChildren, TreeNode parent)
        {
            currentChildren.Add(new FolderNode {
                NodeValue = SR.SchemaHierarchy_SystemServices,
                NodeType = "Folder",
                NodeTypeId = NodeTypes.SystemServices,
                IsSystemObject = true,
                IsMsShippedOwned = true,
                SortPriority = SmoTreeNode.NextSortPriority,
            });
        }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlServiceQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "Service";
            InitializeChild(parent, child, context);
            return child;
        }
    }

    [Export(typeof(ChildFactory))]
    [Shared]
    internal partial class SystemServicesChildFactory : SmoChildFactoryBase
    {
        public override IEnumerable<string> ApplicableParents() { return new[] { "SystemServices" }; }

        internal override Type[] ChildQuerierTypes
        {
           get
           {
              return new [] { typeof(SqlServiceQuerier), };
           }
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            var child = new SmoTreeNode();
            child.IsAlwaysLeaf = true;
            child.NodeType = "SystemService";
            InitializeChild(parent, child, context);
            return child;
        }
    }

}

