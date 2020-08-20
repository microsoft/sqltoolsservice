using System.Collections.Generic;
using Microsoft.Kusto.ServiceLayer.Admin.Contracts;
using Microsoft.Kusto.ServiceLayer.Metadata.Contracts;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Metadata
{
    public interface IMetadataFactory
    {
        DataSourceObjectMetadata CreateClusterMetadata(string clusterName);
        DataSourceObjectMetadata CreateDatabaseMetadata(DataSourceObjectMetadata clusterMetadata, string databaseName);
        FolderMetadata CreateFolderMetadata(DataSourceObjectMetadata parentMetadata, string path, string name);

        /// <summary>
        /// Converts database details shown on cluster manage dashboard to DatabaseInfo type. Add DataSourceType as param if required to show different properties
        /// </summary>
        /// <param name="clusterDBDetails"></param>
        /// <returns></returns>
        List<DatabaseInfo> ConvertToDatabaseInfo(IEnumerable<DataSourceObjectMetadata> clusterDBDetails);

        /// <summary>
        /// Converts tables details shown on database manage dashboard to ObjectMetadata type. Add DataSourceType as param if required to show different properties
        /// </summary>
        /// <param name="dbChildDetails"></param>
        /// <returns></returns>
        List<ObjectMetadata> ConvertToObjectMetadata(IEnumerable<DataSourceObjectMetadata> dbChildDetails);
    }
}