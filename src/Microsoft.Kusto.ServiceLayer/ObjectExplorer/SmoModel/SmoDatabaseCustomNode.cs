//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    /// <summary>
    /// Status for databases
    /// </summary>
    internal partial class DatabasesChildFactory : DataSourceChildFactoryBase
    {
        public override string GetNodeStatus(object objectMetadata, QueryContext oeContext)
        {
            return DatabasesCustomNodeHelper.GetStatus(objectMetadata, oeContext, CachedSmoProperties);
        }

        protected override void InitializeChild(TreeNode parent, TreeNode child, object context)
        {
            base.InitializeChild(parent, child, context);
            var dsTreeNode = child as DataSourceTreeNode;
            if (dsTreeNode != null && dsTreeNode.ObjectMetadata != null
                && DatabasesCustomNodeHelper.GetDatabaseIsUnavailable(dsTreeNode.ObjectMetadata, parent.GetContextAs<QueryContext>(), CachedSmoProperties))
            {
                child.IsAlwaysLeaf = true;
            }
        }
    }

    internal static class DatabasesCustomNodeHelper
    {
        private static readonly DatabaseStatus[] UnavailableDatabaseStatuses = { DatabaseStatus.Inaccessible, DatabaseStatus.Offline, DatabaseStatus.Recovering,
            DatabaseStatus.RecoveryPending, DatabaseStatus.Restoring, DatabaseStatus.Suspect, DatabaseStatus.Shutdown };

        internal static bool GetDatabaseIsUnavailable(object objectMetadata, QueryContext oeContext, IEnumerable<NodeSmoProperty> supportedProperties)
        {
            if(oeContext.DataSource == null) return false; // Assume that database is available

            return !oeContext.DataSource.Exists(objectMetadata);
        }

        internal static string GetStatus(object objectMetadata, QueryContext oeContext, IEnumerable<NodeSmoProperty> supportedProperties)
        {
            if(oeContext.DataSource == null) return "Unknown"; // Assume that database is available

            if(oeContext.DataSource.Exists(objectMetadata)) return "Online";

            return "Offline";
        }
    }
}
