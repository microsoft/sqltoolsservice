using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.Kusto.ServiceLayer.Metadata.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Completion;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    /// <summary>
    /// Data source factory.
    /// </summary>
    public static class DataSourceFactory
    {
        public static IDataSource Create(DataSourceType dataSourceType, string connectionString, string azureAccountToken)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(connectionString, nameof(connectionString));
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(azureAccountToken, nameof(azureAccountToken));

            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                    {
                        return new KustoDataSource(connectionString, azureAccountToken);
                    }

                default:
                    throw new ArgumentException($"Unsupported data source type \"{dataSourceType}\"", nameof(dataSourceType));
            }
        }

        public static DataSourceObjectMetadata CreateClusterMetadata(string clusterName)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(clusterName, nameof(clusterName));

            return new DataSourceObjectMetadata{
                MetadataType = DataSourceMetadataType.Cluster,
                MetadataTypeName = DataSourceMetadataType.Cluster.ToString(),
                Name = clusterName,
                PrettyName = clusterName,
                Urn = $"{clusterName}"
            };
        }

        public static DataSourceObjectMetadata CreateDatabaseMetadata(DataSourceObjectMetadata clusterMetadata, string databaseName)
        {
            ValidationUtils.IsTrue<ArgumentException>(clusterMetadata.MetadataType == DataSourceMetadataType.Cluster, nameof(clusterMetadata));
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(databaseName, nameof(databaseName));

            return new DatabaseMetadata{
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

            return new FolderMetadata{
                MetadataType = DataSourceMetadataType.Folder,
                MetadataTypeName = DataSourceMetadataType.Folder.ToString(),
                Name = name,
                PrettyName = name,
                ParentMetadata = parentMetadata,
                Urn = $"{path}.{name}"
            };
        }

        // Gets default keywords for intellisense when there is no connection.
        public static CompletionItem[] GetDefaultAutoComplete(DataSourceType dataSourceType, ScriptDocumentInfo scriptDocumentInfo, Position textDocumentPosition){
            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                    {
                        return KustoIntellisenseHelper.GetDefaultKeywords(scriptDocumentInfo, textDocumentPosition);
                    }

                default:
                    throw new ArgumentException($"Unsupported data source type \"{dataSourceType}\"", nameof(dataSourceType));
            }
        }

        // Gets default keywords errors related to intellisense when there is no connection.
        public static ScriptFileMarker[] GetDefaultSemanticMarkers(DataSourceType dataSourceType, ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText){
            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                    {
                        return KustoIntellisenseHelper.GetDefaultDiagnostics(parseInfo, scriptFile, queryText);
                    }

                default:
                    throw new ArgumentException($"Unsupported data source type \"{dataSourceType}\"", nameof(dataSourceType));
            }
        }

        // Converts database details shown on cluster manage dashboard to DatabaseInfo type. Add DataSourceType as param if required to show different properties
        public static List<DatabaseInfo> ConvertToDatabaseInfo(IEnumerable<DataSourceObjectMetadata> clusterDBDetails)
        {
            var databaseDetails = new List<DatabaseInfo>();

            if(typeof(DatabaseMetadata) == clusterDBDetails.FirstOrDefault().GetType()){
                foreach(var dbDetail in clusterDBDetails)
                {
                    DatabaseInfo databaseInfo = new DatabaseInfo();
                    Int64.TryParse(dbDetail.SizeInMB.ToString(), out long sum_OriginalSize);
                    databaseInfo.Options["name"] = dbDetail.Name;
                    databaseInfo.Options["sizeInMB"] = (sum_OriginalSize /(1024 * 1024)).ToString();
                    databaseDetails.Add(databaseInfo);
                }
            }

            return databaseDetails;
        }

        // Converts tables details shown on database manage dashboard to ObjectMetadata type. Add DataSourceType as param if required to show different properties
        public static List<ObjectMetadata> ConvertToObjectMetadata(IEnumerable<DataSourceObjectMetadata> dbChildDetails)
        {
            var databaseChildDetails = new List<ObjectMetadata>();

            foreach(var childDetail in dbChildDetails)
            {
                ObjectMetadata dbChildInfo = new ObjectMetadata();
                dbChildInfo.Name = childDetail.PrettyName;
                dbChildInfo.MetadataTypeName = childDetail.MetadataTypeName;
                dbChildInfo.MetadataType = MetadataType.Table;         // Add mapping here.
                databaseChildDetails.Add(dbChildInfo);
            }
            return databaseChildDetails;
        }

        public static ReliableConnectionHelper.ServerInfo ConvertToServerinfoFormat(DataSourceType dataSourceType, DiagnosticsInfo clusterDiagnostics)
        {
            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                    {
                        ReliableConnectionHelper.ServerInfo serverInfo = new ReliableConnectionHelper.ServerInfo();
                        serverInfo.Options = new Dictionary<string, object>(clusterDiagnostics.Options);
                        return serverInfo;
                    }

                default:
                    throw new ArgumentException($"Unsupported data source type \"{dataSourceType}\"", nameof(dataSourceType));
            }
        }
    }
}