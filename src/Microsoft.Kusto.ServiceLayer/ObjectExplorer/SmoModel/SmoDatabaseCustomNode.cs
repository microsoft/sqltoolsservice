//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Status for databases
    /// </summary>
    internal partial class DatabasesChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeStatus(object smoObject, SmoQueryContext smoContext)
        {
            return DatabasesCustomNodeHelper.GetStatus(smoObject, smoContext, CachedSmoProperties);
        }

        protected override void InitializeChild(TreeNode parent, TreeNode child, object context)
        {
            base.InitializeChild(parent, child, context);
            var smoTreeNode = child as SmoTreeNode;
            if (smoTreeNode != null && smoTreeNode.SmoObject != null
                && DatabasesCustomNodeHelper.GetDatabaseIsUnavailable(smoTreeNode.SmoObject, parent.GetContextAs<SmoQueryContext>(), CachedSmoProperties))
            {
                child.IsAlwaysLeaf = true;
            }
        }
    }

    internal static class DatabasesCustomNodeHelper
    {
        private static readonly DatabaseStatus[] UnavailableDatabaseStatuses = { DatabaseStatus.Inaccessible, DatabaseStatus.Offline, DatabaseStatus.Recovering,
            DatabaseStatus.RecoveryPending, DatabaseStatus.Restoring, DatabaseStatus.Suspect, DatabaseStatus.Shutdown };

        internal static bool GetDatabaseIsUnavailable(object smoObject, SmoQueryContext smoContext, IEnumerable<NodeSmoProperty> supportedProperties)
        {
            Database db = smoObject as Database;
            if (db != null && SmoChildFactoryBase.IsPropertySupported("Status", smoContext, db, supportedProperties))
            {
                DatabaseStatus status;
                try
                { 
                    status = db.Status;
                }
                catch (SqlServer.Management.Common.ConnectionFailureException)
                {
                    // We get into this situation with DW Nodes which are paused.
                    return true;
                }

                foreach (DatabaseStatus unavailableStatus in DatabasesCustomNodeHelper.UnavailableDatabaseStatuses)
                {
                    if (status.HasFlag(unavailableStatus))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static string GetStatus(object smoObject, SmoQueryContext smoContext, IEnumerable<NodeSmoProperty> supportedProperties)
        {
            Database db = smoObject as Database;
            if (db != null && SmoChildFactoryBase.IsPropertySupported("Status", smoContext, db, supportedProperties))
            {
                DatabaseStatus status;
                try
                { 
                    status = db.Status;
                }
                catch (SqlServer.Management.Common.ConnectionFailureException)
                {
                    // We get into this situation with DW Nodes which are paused.
                    return "Unknown";
                }
                if ((status & DatabaseStatus.Offline) == DatabaseStatus.Offline)
                {
                    return "Offline";
                }
                else if ((status & DatabaseStatus.Recovering) == DatabaseStatus.Recovering)
                {
                    return "Recovering";
                }
                else if ((status & DatabaseStatus.RecoveryPending) == DatabaseStatus.RecoveryPending)
                {
                    return "Recovery Pending";
                }
                else if ((status & DatabaseStatus.Restoring) == DatabaseStatus.Restoring)
                {
                    return "Restoring";
                }
                else if ((status & DatabaseStatus.EmergencyMode) == DatabaseStatus.EmergencyMode)
                {
                    return "Emergency Mode";
                }
                else if ((status & DatabaseStatus.Inaccessible) == DatabaseStatus.Inaccessible)
                {
                    return "Inaccessible";
                }              
                else if ((status & DatabaseStatus.Shutdown) == DatabaseStatus.Shutdown)
                {
                    return "Shutdown";
                }
                else if ((status & DatabaseStatus.Standby) == DatabaseStatus.Standby)
                {
                    return "Standby";
                }
                else if ((status & DatabaseStatus.Suspect) == DatabaseStatus.Suspect)
                {
                    return "Suspect";
                }
                else if ((status & DatabaseStatus.AutoClosed) == DatabaseStatus.AutoClosed)
                {
                    return "Auto Closed";
                }	
            }

            return string.Empty;
        }
    }
}
