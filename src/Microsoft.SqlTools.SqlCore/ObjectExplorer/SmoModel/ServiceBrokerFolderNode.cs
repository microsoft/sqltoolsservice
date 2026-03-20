//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel
{
    /// <summary>
    /// A folder node for the Service Broker section of the object explorer tree.
    /// Overrides GetContext to provide the ServiceBroker SMO object as the context parent,
    /// enabling child nodes (Message Types, Contracts, Queues, etc.) to query against
    /// ServiceBroker. This is necessary because ServiceBroker extends SqlSmoObject rather
    /// than NamedSmoObject, so it cannot be cached in the standard SmoTreeNode mechanism.
    /// </summary>
    public class ServiceBrokerFolderNode : FolderNode
    {
        public override object GetContext()
        {
            // Get the parent context, which should be the Database node's context
            SmoQueryContext parentContext = Parent?.GetContextAs<SmoQueryContext>();
            if (parentContext != null)
            {
                Database db = parentContext.Parent as Database;
                if (db?.ServiceBroker != null)
                {
                    // Return a context with ServiceBroker as parent so that child queriers
                    // (SqlMessageTypeQuerier, SqlContractQuerier, etc.) can navigate correctly
                    return parentContext.CopyWithParent(db.ServiceBroker);
                }
            }
            return base.GetContext();
        }
    }
}
