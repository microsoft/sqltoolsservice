//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    internal partial class DatabaseTreeNode
    {
        public DatabaseTreeNode(ServerNode serverNode, string databaseName): this()
        {
            Parent = serverNode;
            NodeValue = databaseName;
            Database db = new Database(serverNode.GetContextAs<SmoQueryContext>().Server, this.NodeValue);
            db.Refresh();
            CacheInfoFromModel(db);
        }

        /// <summary>
        /// Initializes the context and ensures that 
        /// </summary>
        protected override void EnsureContextInitialized()
        {
            if (context == null)
            {
                base.EnsureContextInitialized();
                Database db = SmoObject as Database;
                if (context != null && db != null)
                {
                    context.Database = db;
                }
            }
        }
    }
}
