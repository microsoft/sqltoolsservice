//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Status for databases
    /// </summary>
    internal partial class DatabasesChildFactory : SmoChildFactoryBase
    {
        public override string GetNodeStatus(object context)
        {
            return DatabasesCustomNodeHelper.GetStatus(context);
        }
    }

    internal static class DatabasesCustomNodeHelper
    {
        internal static string GetStatus(object context)
        {
            Database db = context as Database;
            if (db != null)
            {
                DatabaseStatus status;
                try { 
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
