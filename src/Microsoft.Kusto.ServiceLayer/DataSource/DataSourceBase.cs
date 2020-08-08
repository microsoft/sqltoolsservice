// <copyright file="DataSourceUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.Kusto.ServiceLayer.Metadata.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Completion;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    /// <summary>
    /// Represents the type of a data source.
    /// </summary>
    public enum DataSourceType
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        None,

        /// <summary>
        /// A Kusto cluster.
        /// </summary>
        Kusto,

        /// <summary>
        /// An Application Insights subscription.
        /// </summary>
        ApplicationInsights,

        /// <summary>
        /// An Operations Management Suite (OMS) Log Analytics workspace.
        /// </summary>
        OmsLogAnalytics
    }

     /// <summary>
    /// Metadata type enumeration
    /// </summary>
    public enum DataSourceMetadataType
    {
        Cluster = 0,
        Database = 1,
        Table = 2,
        Column = 3,
        Function = 4,
        Folder = 5
    }

    public class DiagnosticsInfo
    {
        /// <summary>
        /// Gets or sets the options
        /// </summary>
        public Dictionary<string, object> Options { get; set; }

        public DiagnosticsInfo()
        {
            Options = new Dictionary<string, object>();
        }
        
    }

    /// <summary>
    /// Object metadata information
    /// </summary>
    public class DataSourceObjectMetadata 
    {
        public DataSourceMetadataType MetadataType { get; set; }
    
        public string MetadataTypeName { get; set; }

        public string Name { get; set; }

        public string PrettyName { get; set; }
        
        public string Urn { get; set; }

        public string SizeInMB { get; set; }

    }

    /// <summary>
    /// Database metadata information
    /// </summary>
    public class DatabaseMetadata : DataSourceObjectMetadata
    {
        public string ClusterName { get; set; }
    }

    /// <summary>
    /// Database metadata information
    /// </summary>
    public class TableMetadata : DatabaseMetadata
    {
        public string DatabaseName { get; set; }
    }

    /// <summary>
    /// Column metadata information
    /// </summary>
    public class ColumnMetadata : TableMetadata
    {
        public string TableName { get; set; }
        public string DataType { get; set; }
    }

    /// <summary>
    /// Folder metadata information
    /// </summary>
    public class FolderMetadata : DataSourceObjectMetadata
    {
        public DataSourceObjectMetadata ParentMetadata { get; set; }
    }
    
    /// <summary>
    /// Represents data source utilities.
    /// </summary>
    public interface IDataSource : IDisposable
    {
        /// <summary>
        /// The data source type.
        /// </summary>
        DataSourceType DataSourceType { get; }

        /// <summary>
        /// The cluster/server name.
        /// </summary>
        string ClusterName { get; }

        /// <summary>
        /// The current database name, if there is one.
        /// </summary>
        string DatabaseName { get; set; }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The results.</returns>
        Task<IDataReader> ExecuteQueryAsync(string query, CancellationToken cancellationToken, string databaseName = null);

        /// <summary>
        /// Executes a Kusto query that returns a scalar value.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>The result.</returns>
        Task<T> ExecuteScalarQueryAsync<T>(string query, CancellationToken cancellationToken, string databaseName = null);

        /// <summary>
        /// Executes a Kusto query that returns a scalar value.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>The result.</returns>
        Task<IEnumerable<T>> ExecuteControlCommandAsync<T>(string command, bool throwOnError, CancellationToken cancellationToken);

        /// <summary>
        /// Get children of the  given parent
        /// </summary>
        /// <param name="parentMetadata">Parent object metadata.</param>
        /// <returns>Metadata for all children.</returns>
        DiagnosticsInfo GetDiagnostics(DataSourceObjectMetadata parentMetadata);

        /// <summary>
        /// Get children of the  given parent
        /// </summary>
        /// <param name="parentMetadata">Parent object metadata.</param>
        /// <returns>Metadata for all children.</returns>
        IEnumerable<DataSourceObjectMetadata> GetChildObjects(DataSourceObjectMetadata parentMetadata, bool includeSizeDetails = false);

        /// <summary>
        /// Get folders of the  given parent
        /// </summary>
        /// <param name="parentMetadata">Parent object metadata.</param>
        /// <returns>List of all children.</returns>
        IEnumerable<DataSourceObjectMetadata> GetChildFolders(DataSourceObjectMetadata parentMetadata);

        /// <summary>
        /// Refresh object list for entire cluster.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Refresh object list for given object.
        /// </summary>
        /// <param name="objectMetadata">Object metadata.</param>
        void Refresh(DataSourceObjectMetadata objectMetadata);

        /// <summary>
        /// Updates database and affected variables like GlobalState for given object.
        /// </summary>
        /// <param name="updateDatabase">Object metadata.</param>
        void UpdateDatabase(string databaseName);

        /// <summary>
        /// Gets autocomplete suggestions at given position.
        /// </summary>
        /// <param name="GetAutoCompleteSuggestions">Object metadata.</param>
        CompletionItem[] GetAutoCompleteSuggestions(ScriptDocumentInfo queryText, Position index, bool throwOnError = false);
        /// <summary>
        /// Gets quick info hover tooltips for the current position.
        /// </summary>
        /// <param name="GetHoverHelp">Object metadata.</param>
        Hover GetHoverHelp(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false);

        /// <summary>
        /// Gets definition for a selected query text.
        /// </summary>
        /// <param name="GetDefinition">Object metadata.</param>
        DefinitionResult GetDefinition(string queryText, int index, int startLine, int startColumn, bool throwOnError = false);
        
        /// <summary>
        /// Gets a list of semantic diagnostic marks for the provided script file
        /// </summary>
        /// <param name="GetSemanticMarkers">Object metadata.</param>
        ScriptFileMarker[] GetSemanticMarkers(ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText);

        /// <summary>
        /// Tells whether the data source exists.
        /// </summary>
        /// <returns>true if it exists; false otherwise.</returns>
        Task<bool> Exists();

        /// <summary>
        /// Tells whether the object exists.
        /// </summary>
        /// <returns>true if it exists; false otherwise.</returns>
        bool Exists(DataSourceObjectMetadata objectMetadata);
    }

    /// <inheritdoc cref="IDataSource"/>
    public abstract class DataSourceBase : IDataSource
    {
        protected Object dataSourceLock = new Object();

        private string _database;

        #region IDisposable

        /// <summary>
        /// Finalizes an instance of the <see cref="DataSourceBase"/> class.
        /// </summary>
        ~DataSourceBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        /// <param name="disposing">true if disposing; false if finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        #endregion

        #region IDataSource

        /// <inheritdoc/>
        public abstract Task<IDataReader> ExecuteQueryAsync(string query, CancellationToken cancellationToken, string databaseName = null);

        /// <inheritdoc/>
        public async Task<T> ExecuteScalarQueryAsync<T>(string query, CancellationToken cancellationToken, string databaseName = null)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(query, nameof(query));

            using (var records = await ExecuteQueryAsync(query, cancellationToken, databaseName))
            {
                return records.ToScalar<T>();
            }
        }

        public abstract Task<IEnumerable<T>> ExecuteControlCommandAsync<T>(string command, bool throwOnError, CancellationToken cancellationToken);

        /// <inheritdoc/>
        public abstract DiagnosticsInfo GetDiagnostics(DataSourceObjectMetadata parentMetadata);

        /// <inheritdoc/>
        public abstract IEnumerable<DataSourceObjectMetadata> GetChildObjects(DataSourceObjectMetadata parentMetadata, bool includeSizeDetails = false);

        /// <inheritdoc/>
        public abstract IEnumerable<DataSourceObjectMetadata> GetChildFolders(DataSourceObjectMetadata parentMetadata);

        /// <inheritdoc/>
        public abstract void Refresh();

        /// <inheritdoc/>
        public abstract void Refresh(DataSourceObjectMetadata objectMetadata);

        /// <inheritdoc/>
        public abstract void UpdateDatabase(string databaseName);

        /// <inheritdoc/>
        public abstract CompletionItem[] GetAutoCompleteSuggestions(ScriptDocumentInfo queryText, Position index, bool throwOnError = false);
        /// <inheritdoc/>
        public abstract Hover GetHoverHelp(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false);
        
        /// <inheritdoc/>
        public abstract DefinitionResult GetDefinition(string queryText, int index, int startLine, int startColumn, bool throwOnError = false);

        /// <inheritdoc/>
        public abstract ScriptFileMarker[] GetSemanticMarkers(ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText);

        /// <inheritdoc/>
        public abstract Task<bool> Exists();

        /// <inheritdoc/>
        public abstract bool Exists(DataSourceObjectMetadata objectMetadata);

        /// <inheritdoc/>
        public DataSourceType DataSourceType { get; protected set; }

        /// <inheritdoc/>
        public string ClusterName { get; protected set; }

        /// <inheritdoc/>
        public string DatabaseName { 
            get
            {
                return _database;
            }
            
            set
            {
                lock(dataSourceLock)
                {
                    _database = value;
                }
            }
        }

        #endregion
    }

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

        public static DataSourceObjectMetadata CreateFolderMetadata(DataSourceObjectMetadata parentMetadata, string name)
        {
            ValidationUtils.IsNotNull(parentMetadata, nameof(parentMetadata));

            return new FolderMetadata{
                MetadataType = DataSourceMetadataType.Folder,
                MetadataTypeName = DataSourceMetadataType.Folder.ToString(),
                Name = name,
                PrettyName = name,
                ParentMetadata = parentMetadata,
                Urn = $"{parentMetadata.Urn}.Folder_{name}"
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