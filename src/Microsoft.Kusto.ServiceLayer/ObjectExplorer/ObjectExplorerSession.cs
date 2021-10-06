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

        public string Uri { get; }
        public TreeNode Root { get; }

        public ConnectionInfo ConnectionInfo { get; set; }

        public string ErrorMessage { get; set; }
        
        public static ObjectExplorerSession CreateSession(ConnectionCompleteParams response, IMultiServiceProvider serviceProvider,
            IDataSource dataSource)
        {
            DataSourceObjectMetadata objectMetadata = MetadataFactory.CreateClusterMetadata(dataSource.ClusterName);
            var rootNode = new ServerNode(response, serviceProvider, dataSource, objectMetadata);

            return new ObjectExplorerSession(response.OwnerUri, rootNode);
        }
    }
}