//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer
{
    internal class ObjectExplorerSession
    {
        internal ObjectExplorerSession(string uri, TreeNode root)
        {
            Validate.IsNotNullOrEmptyString("uri", uri);
            Validate.IsNotNull("root", root);
            Uri = uri;
            Root = root;
        }

        public string Uri { get; private set; }
        public TreeNode Root { get; private set; }

        public ConnectionInfo ConnectionInfo { get; set; }

        public string ErrorMessage { get; set; }

        public static ObjectExplorerSession CreateSession(ConnectionCompleteParams response, IMultiServiceProvider serviceProvider,
            IDataSource dataSource, bool isDefaultOrSystemDatabase)
        {
            DataSourceObjectMetadata objectMetadata = MetadataFactory.CreateClusterMetadata(dataSource.ClusterName);
            var rootNode = new ServerNode(response, serviceProvider, dataSource, objectMetadata);

            var session = new ObjectExplorerSession(response.OwnerUri, rootNode);
            if (!isDefaultOrSystemDatabase)
            {
                DataSourceObjectMetadata databaseMetadata =
                    MetadataFactory.CreateDatabaseMetadata(objectMetadata, response.ConnectionSummary.DatabaseName);
            }

            return session;
        }
    }
}