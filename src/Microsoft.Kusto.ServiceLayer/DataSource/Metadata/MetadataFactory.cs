//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Kusto.ServiceLayer.Admin.Contracts;
using Microsoft.Kusto.ServiceLayer.Metadata.Contracts;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Metadata
{
    public class MetadataFactory
    {
        public static DataSourceObjectMetadata CreateClusterMetadata(string clusterName)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(clusterName, nameof(clusterName));

            return new DataSourceObjectMetadata
            {
                MetadataType = DataSourceMetadataType.Cluster,
                MetadataTypeName = DataSourceMetadataType.Cluster.ToString(),
                Name = clusterName,
                PrettyName = clusterName,
                Urn = $"{clusterName}"
            };
        }

        public static DataSourceObjectMetadata CreateDatabaseMetadata(DataSourceObjectMetadata clusterMetadata,
            string databaseName)
        {
            ValidationUtils.IsTrue<ArgumentException>(clusterMetadata.MetadataType == DataSourceMetadataType.Cluster,
                nameof(clusterMetadata));
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(databaseName, nameof(databaseName));

            return new DatabaseMetadata
            {
                ClusterName = clusterMetadata.Name,
                MetadataType = DataSourceMetadataType.Database,
                MetadataTypeName = DataSourceMetadataType.Database.ToString(),
                Name = databaseName,
                PrettyName = databaseName,
                Urn = $"{clusterMetadata.Urn}.{databaseName}"
            };
        }

        public static FolderMetadata CreateFolderMetadata(DataSourceObjectMetadata parentMetadata, string path, string name)
        {
            ValidationUtils.IsNotNull(parentMetadata, nameof(parentMetadata));

            return new FolderMetadata
            {
                MetadataType = DataSourceMetadataType.Folder,
                MetadataTypeName = DataSourceMetadataType.Folder.ToString(),
                Name = name,
                PrettyName = name,
                ParentMetadata = parentMetadata,
                Urn = $"{path}.{name}"
            };
        }

        /// <summary>
        /// Converts database details shown on cluster manage dashboard to DatabaseInfo type. Add DataSourceType as param if required to show different properties
        /// </summary>
        /// <param name="clusterDbDetails"></param>
        /// <returns></returns>
        public static List<DatabaseInfo> ConvertToDatabaseInfo(IEnumerable<DataSourceObjectMetadata> clusterDbDetails)
        {
            if (clusterDbDetails.FirstOrDefault() is not DatabaseMetadata)
            {
                return new List<DatabaseInfo>();
            }

            var databaseDetails = new List<DatabaseInfo>();
            
            foreach (var dataSourceObjectMetadata in clusterDbDetails)
            {
                var dbDetail = (DatabaseMetadata) dataSourceObjectMetadata;
                long.TryParse(dbDetail.SizeInMB, out long sizeInMb);

                var databaseInfo = new DatabaseInfo
                {
                    Options =
                    {
                        ["name"] = dbDetail.Name, 
                        ["sizeInMB"] = (sizeInMb / (1024 * 1024)).ToString()
                    }
                };
                
                databaseDetails.Add(databaseInfo);
            }
            
            return databaseDetails;
        }

        /// <summary>
        /// Converts tables details shown on database manage dashboard to ObjectMetadata type. Add DataSourceType as param if required to show different properties
        /// </summary>
        /// <param name="dbChildDetails"></param>
        /// <returns></returns>
        public static List<ObjectMetadata> ConvertToObjectMetadata(IEnumerable<DataSourceObjectMetadata> dbChildDetails)
        {
            var databaseChildDetails = new List<ObjectMetadata>();

            foreach (var childDetail in dbChildDetails)
            {
                ObjectMetadata dbChildInfo = new ObjectMetadata();
                dbChildInfo.Name = childDetail.PrettyName;
                dbChildInfo.MetadataTypeName = childDetail.MetadataTypeName;
                dbChildInfo.MetadataType = childDetail.MetadataType == DataSourceMetadataType.MaterializedView ?
                    MetadataType.View :
                    MetadataType.Table;
                databaseChildDetails.Add(dbChildInfo);
            }

            return databaseChildDetails;
        }

        public static DataSourceObjectMetadata CreateDataSourceObjectMetadata(DataSourceMetadataType datatype, string name, string urn)
        {
            return new DataSourceObjectMetadata
            {
                MetadataType = datatype,
                MetadataTypeName = datatype.ToString(),
                Name = name,
                PrettyName = name,
                Urn = $"{urn}",
            };
        }
    }
}