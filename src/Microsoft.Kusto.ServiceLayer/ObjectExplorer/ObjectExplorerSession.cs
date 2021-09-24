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
        private ObjectExplorerSession(string uri, TreeNode root)
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

        public static ObjectExplorerSession CreateSession(ConnectionCompleteParams completeParams, IMultiServiceProvider serviceProvider,
            IDataSource dataSource)
        {
            TreeNode rootNode;
            if (dataSource.DataSourceType == DataSourceType.LogAnalytics)
            {
                var databaseMetadata = MetadataFactory.CreateDatabaseMetadata(dataSource.ClusterName);
                rootNode = new DatabaseNode(completeParams, serviceProvider, dataSource, databaseMetadata);
            }
            else
            {
                DataSourceObjectMetadata clusterMetadata = MetadataFactory.CreateClusterMetadata(dataSource.ClusterName);
                rootNode = new ServerNode(completeParams, serviceProvider, dataSource, clusterMetadata);
            }


            return new ObjectExplorerSession(completeParams.OwnerUri, rootNode);
        }
    }
}