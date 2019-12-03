//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Utility;
using Microsoft.Kusto.ServiceLayer.DataSource;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    internal partial class DatabaseTreeNode
    {
        public DatabaseTreeNode(ServerNode serverNode, string databaseName): this()
        {
            Parent = serverNode;
            NodeValue = databaseName;

            CacheInfoFromModel(DataSourceFactory.CreateDatabaseMetadata(serverNode.ObjectMetadata, databaseName));
        }

        /// <summary>
        /// Initializes the context and sets its ValidFor property 
        /// </summary>
        protected override void EnsureContextInitialized()
        {
        }

        protected override void PopulateChildren(bool refresh, string name, CancellationToken cancellationToken)
        {
            base.PopulateChildren(refresh, name, cancellationToken);
        }
    }
}
