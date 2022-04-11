//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Data;
using Kusto.Data;
using Kusto.Data.Data;
using Microsoft.Kusto.ServiceLayer.Admin.Contracts;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource.Intellisense;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.DataSource.Models;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using Newtonsoft.Json;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Kusto
{
    /// <summary>
    /// Represents Kusto utilities.
    /// </summary>
    public class KustoDataSource : DataSourceBase
    {
        private readonly IKustoClient _kustoClient;
        private readonly IntellisenseClientBase _intellisenseClient;

        /// <summary>
        /// List of databases.
        /// </summary>
        private IEnumerable<DataSourceObjectMetadata> _databaseMetadata;

        /// <summary>
        /// List of tables per database. Key - Parent Folder or Database Urn
        /// </summary>
        private ConcurrentDictionary<string, IEnumerable<TableMetadata>> _tableMetadata = new ConcurrentDictionary<string, IEnumerable<TableMetadata>>();

        /// <summary>
        /// List of columns per table. Key - DatabaseName.TableName
        /// </summary>
        private ConcurrentDictionary<string, IEnumerable<DataSourceObjectMetadata>> _columnMetadata = new ConcurrentDictionary<string, IEnumerable<DataSourceObjectMetadata>>();
        
        /// <summary>
        /// List of tables per database. Key - Parent Folder or Database Urn
        /// </summary>
        private ConcurrentDictionary<string, IEnumerable<FolderMetadata>> _folderMetadata = new ConcurrentDictionary<string, IEnumerable<FolderMetadata>>();
        
        /// <summary>
        /// List of functions per database. Key - Parent Folder or Database Urn
        /// </summary>
        private ConcurrentDictionary<string, IEnumerable<FunctionMetadata>> _functionMetadata = new ConcurrentDictionary<string, IEnumerable<FunctionMetadata>>();

        public override string DatabaseName
        {
            get => _kustoClient.DatabaseName;
            set => throw new NotImplementedException();
        }

        public override string ClusterName => _kustoClient.ClusterName;

        // Some clusters have this signature. Queries might slightly differ for Aria
        private const string AriaProxyURL = "kusto.aria.microsoft.com"; 

        /// <summary>
        /// The database schema query.  Performance: ".show database schema" is more efficient than ".show schema",
        /// especially for large clusters with many databases or tables.
        /// </summary>
        private const string ShowDatabaseSchema = ".show database [{0}] schema";

        /// <summary>
        /// The dashboard needs a list of all tables regardless of the folder structure of the table. The
        /// tables are stored with the key in the following format: OnlyTables.ClusterName.DatabaseName
        /// </summary>
        private const string DatabaseKeyPrefix = "OnlyTables";

        /// <summary>
        /// Prevents a default instance of the <see cref="IDataSource"/> class from being created.
        /// </summary>
        public KustoDataSource(IKustoClient kustoClient, IntellisenseClientBase intellisenseClient)
        {
            _kustoClient = kustoClient;
            _intellisenseClient = intellisenseClient;
            // Check if a connection can be made
            ValidationUtils.IsTrue<ArgumentException>(Exists().Result,
                $"Unable to connect. ClusterName = {ClusterName}, DatabaseName = {DatabaseName}");
        }
        
        /// <summary>
        /// Disposes resources.
        /// </summary>
        /// <param name="disposing">True if disposing.  False otherwise.</param>
        protected override void Dispose(bool disposing)
        {
            // Dispose managed resources.
            if (disposing)
            {
                _kustoClient.Dispose();
            }

            base.Dispose(disposing);
        }

        #region DataSourceUtils

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The results.</returns>
        public override Task<IDataReader> ExecuteQueryAsync(string query, CancellationToken cancellationToken, string databaseName = null)
        {
            var reader = _kustoClient.ExecuteQuery(query, cancellationToken, databaseName);
            return Task.FromResult(reader);
        }

        /// <inheritdoc/>
        public override async Task<bool> Exists()
        {
            try
            {
                var source = new CancellationTokenSource();

                if (ClusterName.Contains(AriaProxyURL, StringComparison.OrdinalIgnoreCase))
                {

                    var result = await ExecuteScalarQueryAsync<string>(".show databases | take 1 | project DatabaseName", source.Token); 
                    return !string.IsNullOrWhiteSpace(result);
                }
                
                var count = await ExecuteScalarQueryAsync<long>(".show databases | count", source.Token);
                return count >= 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion
        
        #region IDataSource

        private DiagnosticsInfo GetClusterDiagnostics()
        {
            if (ClusterName.Contains(AriaProxyURL, StringComparison.CurrentCultureIgnoreCase))
            {
                return new DiagnosticsInfo();
            }
            
            var source = new CancellationTokenSource();
            var clusterDiagnostics = new DiagnosticsInfo();

            var query =  ".show diagnostics | extend Passed= (IsHealthy) and not(IsScaleOutRequired) | extend Summary = strcat('Cluster is ', iif(Passed, '', 'NOT'), 'healthy.'),Details=pack('MachinesTotal', MachinesTotal, 'DiskCacheCapacity', round(ClusterDataCapacityFactor,1)) | project Action = 'Cluster Diagnostics', Category='Info', Summary, Details;";
            using (var reader = _kustoClient.ExecuteQuery(query, source.Token))
            {
                while (reader.Read())
                {
                    var details = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader["Details"].ToString());
                    clusterDiagnostics.Options["summary"] = reader["Summary"].ToString();
                    clusterDiagnostics.Options["machinesTotal"] = details["MachinesTotal"].ToString();
                    clusterDiagnostics.Options["diskCacheCapacity"] = details["DiskCacheCapacity"].ToString() + "%";
                }
            }

            return clusterDiagnostics;
        }

        /// <inheritdoc/>
        private IEnumerable<DataSourceObjectMetadata> GetDatabaseMetadata(bool includeSizeDetails)
        {
            if (_databaseMetadata == null)
            {
                SetDatabaseMetadata(includeSizeDetails);
            }

            return _databaseMetadata.OrderBy(x => x.PrettyName);
        }

        private void SetDatabaseMetadata(bool includeSizeDetails)
        {
            if (ClusterName.Contains(AriaProxyURL, StringComparison.CurrentCultureIgnoreCase))
            {
                includeSizeDetails = false;
            }
            
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            // Getting database names when we are connected to a specific database should not happen.
            ValidationUtils.IsNotNull(DatabaseName, nameof(DatabaseName));

            var query = includeSizeDetails
                ? ".show cluster extents | summarize sum(OriginalSize) by tostring(DatabaseName)"
                : ".show databases | project DatabaseName, PrettyName";

            using (var reader = _kustoClient.ExecuteQuery(query, token))
            {
                _databaseMetadata = reader.ToEnumerable()
                    .Where(row => !string.IsNullOrWhiteSpace(row["DatabaseName"].ToString()))
                    .Select(row => new DatabaseMetadata
                    {
                        ClusterName = this.ClusterName,
                        MetadataType = DataSourceMetadataType.Database,
                        MetadataTypeName = DataSourceMetadataType.Database.ToString(),
                        SizeInMB = includeSizeDetails ? row["sum_OriginalSize"].ToString() : "",
                        Name = row["DatabaseName"].ToString(),
                        PrettyName = includeSizeDetails ? row["DatabaseName"].ToString(): (string.IsNullOrEmpty(row["PrettyName"]?.ToString()) ? row["DatabaseName"].ToString() : row["PrettyName"].ToString()),
                        Urn = $"{ClusterName}.{row["DatabaseName"]}"
                    })
                    .Materialize()
                    .OrderBy(row => row.Name, StringComparer.Ordinal); // case-sensitive
            }
        }

        /// <inheritdoc/>
        public override bool Exists(DataSourceObjectMetadata objectMetadata)
        {
            ValidationUtils.IsNotNull(objectMetadata, "Need a datasource object");

            switch(objectMetadata.MetadataType)
            {
                case DataSourceMetadataType.Database: return DatabaseExists(objectMetadata.Name).Result;
                default: throw new ArgumentException($"Unexpected type {objectMetadata.MetadataType}.");
            }
        }

        /// <summary>
        /// Executes a query or command against a kusto cluster and returns a sequence of result row instances.
        /// </summary>
        public override async Task<IEnumerable<T>> ExecuteControlCommandAsync<T>(string command, bool throwOnError, CancellationToken cancellationToken)
        {
            try
            {
                var resultReader = await ExecuteQueryAsync(command, cancellationToken, DatabaseName);
                var results = KustoDataReaderParser.ParseV1(resultReader, null);
                var tableReader = results[WellKnownDataSet.PrimaryResult].Single().TableData.CreateDataReader();
                return new ObjectReader<T>(tableReader);
            }
            catch (Exception) when (!throwOnError)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public override void UpdateDatabase(string databaseName)
        {
            // Aria has a GUID as a database name so parse it from the display name
            var parsedDatabase = KustoQueryUtils.ParseDatabaseName(databaseName);
            _kustoClient.UpdateDatabase(parsedDatabase);
            _intellisenseClient.UpdateDatabase(parsedDatabase);
        }
        
        /// <summary>
        /// Clears everything
        /// </summary>
        /// <param name="includeDatabase"></param>
        public override void Refresh(bool includeDatabase)
        {
            // This class caches objects. Throw them away so that the next call will re-query the data source for the objects.
            if (includeDatabase)
            {
                _databaseMetadata = null;
            }
            _tableMetadata = new ConcurrentDictionary<string, IEnumerable<TableMetadata>>();
            _columnMetadata  = new ConcurrentDictionary<string, IEnumerable<DataSourceObjectMetadata>>();
            _folderMetadata = new ConcurrentDictionary<string, IEnumerable<FolderMetadata>>();
            _functionMetadata = new ConcurrentDictionary<string, IEnumerable<FunctionMetadata>>();
        }

        /// <inheritdoc/>
        public override void Refresh(DataSourceObjectMetadata objectMetadata)
        {
            ValidationUtils.IsNotNull(objectMetadata, nameof(objectMetadata));

            switch(objectMetadata.MetadataType)
            {
                case DataSourceMetadataType.Cluster:
                    Refresh(true);
                    SetDatabaseMetadata(false);
                    break;
                
                case DataSourceMetadataType.Database:
                    Refresh(false);
                    LoadTableSchema(objectMetadata);
                    LoadFunctionSchema(objectMetadata);
                    break;

                case DataSourceMetadataType.Table:
                    var table = objectMetadata as TableMetadata;
                    _columnMetadata.TryRemove(GenerateMetadataKey(table.DatabaseName, table.Name), out _);
                    SetTableSchema(table);
                    break;

                case DataSourceMetadataType.Folder:
                    Refresh(false);
                    var folder = objectMetadata as FolderMetadata;
                    LoadTableSchema(folder.ParentMetadata);
                    LoadFunctionSchema(folder.ParentMetadata);
                    break;
                
                default:
                    throw new ArgumentException($"Unexpected type {objectMetadata.MetadataType}.");
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<DataSourceObjectMetadata> GetChildObjects(DataSourceObjectMetadata objectMetadata,
            bool includeSizeDetails = false)
        {
            ValidationUtils.IsNotNull(objectMetadata, nameof(objectMetadata));

            switch (objectMetadata.MetadataType)
            {
                case DataSourceMetadataType.Cluster: // show databases
                    return GetDatabaseMetadata(includeSizeDetails);

                case DataSourceMetadataType.Database: // show folders, tables, and functions
                    return includeSizeDetails
                        ? GetTablesForDashboard(objectMetadata)
                        : GetDatabaseSchema(objectMetadata);

                case DataSourceMetadataType.Table: // show columns
                    var table = objectMetadata as TableMetadata;
                    return GetTableSchema(table);

                case DataSourceMetadataType.Folder: // show subfolders, functions, and tables
                    var folder = objectMetadata as FolderMetadata;
                    return GetAllMetadata(folder.Urn);

                default:
                    throw new ArgumentException($"Unexpected type {objectMetadata.MetadataType}.");
            }
        }

        public override DiagnosticsInfo GetDiagnostics(DataSourceObjectMetadata objectMetadata)
        {
            ValidationUtils.IsNotNull(objectMetadata, nameof(objectMetadata));

            // Add more cases when required.
            switch(objectMetadata.MetadataType)
            {
                case DataSourceMetadataType.Cluster:
                    return GetClusterDiagnostics();

                default:
                    throw new ArgumentException($"Unexpected type {objectMetadata.MetadataType}.");
            }
        }

        internal async Task<bool> DatabaseExists(string databaseName)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(databaseName, nameof(databaseName));

            try
            {
                CancellationTokenSource source = new CancellationTokenSource();
                CancellationToken token = source.Token;

                var count = await ExecuteScalarQueryAsync<long>(".show tables | count", token, databaseName);
                return count >= 0;
            }
            catch
            {
                return false;
            }
        }
        
        /// <inheritdoc/>
        private IEnumerable<DataSourceObjectMetadata> GetDatabaseSchema(DataSourceObjectMetadata objectMetadata)
        {
            // Check if the database exists
            ValidationUtils.IsTrue<ArgumentException>(DatabaseExists(objectMetadata.Name).Result, $"Database '{objectMetadata}' does not exist.");

            var allMetadata = GetAllMetadata(objectMetadata.Urn);
            
            // if the records have already been loaded them return them
            if (allMetadata.Any())
            {
                return allMetadata;
            }

            LoadTableSchema(objectMetadata);
            LoadFunctionSchema(objectMetadata);
            
            return GetAllMetadata(objectMetadata.Urn);
        }

        private IEnumerable<DataSourceObjectMetadata> GetTablesForDashboard(DataSourceObjectMetadata objectMetadata)
        {
            string newKey = $"{DatabaseKeyPrefix}.{objectMetadata.Urn}";

            if (!_tableMetadata.ContainsKey(newKey) || !_tableMetadata[newKey].Any())
            {
                 LoadTableSchema(objectMetadata);   
            }
            
            return _tableMetadata[newKey].OrderBy(x => x.PrettyName, StringComparer.OrdinalIgnoreCase);
        }

        private IEnumerable<DataSourceObjectMetadata> GetAllMetadata(string key)
        {
            var returnList = new List<DataSourceObjectMetadata>();

            if (_folderMetadata.ContainsKey(key))
            {
                returnList.AddRange(_folderMetadata[key]
                    .OrderBy(x => x.PrettyName, StringComparer.OrdinalIgnoreCase));
            }

            if (_tableMetadata.ContainsKey(key))
            {
                returnList.AddRange(_tableMetadata[key]
                    .OrderBy(x => x.PrettyName, StringComparer.OrdinalIgnoreCase));
            }

            if (_functionMetadata.ContainsKey(key))
            {
                returnList.AddRange(_functionMetadata[key]
                    .OrderBy(x => x.PrettyName, StringComparer.OrdinalIgnoreCase));
            }

            return returnList;
        }

        /// <summary>
        /// Gets column data which includes tables and table folders.
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private IEnumerable<ColumnInfo> GetColumnInfos(string databaseName, string tableName)
        {
            ValidationUtils.IsNotNullOrWhitespace(databaseName, nameof(databaseName));

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            const string systemPrefix = "System.";
            var query = new StringBuilder(string.Format(CultureInfo.InvariantCulture, ShowDatabaseSchema,
                databaseName));
            query.Append($" | where TableName == '{tableName}' ");
            query.Append(" | project TableName, ColumnName, ColumnType, Folder");

            using (var reader = _kustoClient.ExecuteQuery(query.ToString(), token, databaseName))
            {
                var columns = reader.ToEnumerable()
                    .Select(row => new ColumnInfo
                    {
                        Table = row["TableName"]?.ToString(),
                        Name = row["ColumnName"]?.ToString(),
                        DataType = row["ColumnType"]?.ToString().TrimPrefix(systemPrefix),
                        Folder = row["Folder"]?.ToString()
                    })
                    .Materialize()
                    .OrderBy(row => row.Name, StringComparer.Ordinal); // case-sensitive

                return columns;
            }
        }

        private IEnumerable<TableInfo> GetTableInfos(string databaseName)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            string query = $".show database {KustoQueryUtils.EscapeName(databaseName)} cslschema";

            using (var reader = _kustoClient.ExecuteQuery(query, token, databaseName))
            {
                return reader.ToEnumerable()
                    .Select(row => new TableInfo
                    {
                        TableName = row["TableName"]?.ToString(),
                        Folder = row["Folder"]?.ToString()
                    })
                    .Materialize();
            }
        }

        private void LoadTableSchema(DataSourceObjectMetadata databaseMetadata)
        {
            var tableInfos = GetTableInfos(databaseMetadata.Name);

            if (!tableInfos.Any())
            {
                return;
            }
            
            var rootTableFolderKey = new StringBuilder($"{databaseMetadata.Urn}");
            if (tableInfos.Any(x => !string.IsNullOrWhiteSpace(x.Folder)))
            {
                // create Table folder to hold functions tables
                var tableFolder = MetadataFactory.CreateFolderMetadata(databaseMetadata, rootTableFolderKey.ToString(), "Tables");
                _folderMetadata.AddRange(rootTableFolderKey.ToString(), new List<FolderMetadata> {tableFolder});
                rootTableFolderKey.Append($".{tableFolder.Name}");
                
                SetFolderMetadataForTables(databaseMetadata, tableInfos, rootTableFolderKey.ToString());
            }
            
            SetTableMetadata(databaseMetadata, tableInfos, rootTableFolderKey.ToString());
        }

        private IEnumerable<DataSourceObjectMetadata> GetTableSchema(TableMetadata tableMetadata)
        {
            var key = GenerateMetadataKey(tableMetadata.DatabaseName, tableMetadata.Name);
            if (_columnMetadata.ContainsKey(key))
            {
                return _columnMetadata[key];
            }
            
            SetTableSchema(tableMetadata);

            return _columnMetadata.ContainsKey(key)
                ? _columnMetadata[key]
                : Enumerable.Empty<DataSourceObjectMetadata>();
        }

        private void SetTableSchema(TableMetadata tableMetadata)
        {
            IEnumerable<ColumnInfo> columnInfos = GetColumnInfos(tableMetadata.DatabaseName, tableMetadata.Name);

            if (!columnInfos.Any())
            {
                return;
            }
            
            SetColumnMetadata(tableMetadata.DatabaseName, tableMetadata.Name, columnInfos);
        }

        private void SetFolderMetadataForTables(DataSourceObjectMetadata objectMetadata, IEnumerable<TableInfo> tableInfos, string rootTableFolderKey)
        {
            var tablesByFolder = tableInfos
                .GroupBy(x => x.Folder, StringComparer.OrdinalIgnoreCase)
                .ToList();
            
            var tableFolders = new List<FolderMetadata>();

            foreach (var columnGroup in tablesByFolder)
            {
                // skip tables without folders
                if (string.IsNullOrWhiteSpace(columnGroup.Key))
                {
                    continue;
                }

                var folder = MetadataFactory.CreateFolderMetadata(objectMetadata, rootTableFolderKey, columnGroup.Key);
                tableFolders.Add(folder);
            }

            _folderMetadata.AddRange(rootTableFolderKey, tableFolders);
        }

        private void LoadFunctionSchema(DataSourceObjectMetadata databaseMetadata)
        {
            IEnumerable<FunctionInfo> functionInfos = GetFunctionInfos(databaseMetadata.Name);

            if (!functionInfos.Any())
            {
                return;
            }

            // create Functions folder to hold functions folders
            var rootFunctionFolderKey = $"{databaseMetadata.Urn}";
            var rootFunctionFolder = MetadataFactory.CreateFolderMetadata(databaseMetadata, rootFunctionFolderKey, "Functions");
            _folderMetadata.AddRange(rootFunctionFolderKey, new List<FolderMetadata> {rootFunctionFolder});

            // create each folder to hold functions
            var functionsGroupByFolder = functionInfos
                .GroupBy(x => x.Folder, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (functionsGroupByFolder.Any())
            {
                SetFolderMetadataForFunctions(databaseMetadata, functionsGroupByFolder, rootFunctionFolder);    
            }

            SetFunctionMetadata(databaseMetadata.Name, rootFunctionFolder.Name, functionsGroupByFolder);
        }

        private void SetFolderMetadataForFunctions(DataSourceObjectMetadata databaseMetadata, List<IGrouping<string, FunctionInfo>> functionsGroupByFolder,
            FolderMetadata functionFolder)
        {
            var functionFolders = new Dictionary<string, Dictionary<string, FolderMetadata>>();

            foreach (var functionGroup in functionsGroupByFolder)
            {
                // skip functions with no folder
                if (string.IsNullOrWhiteSpace(functionGroup.Key))
                {
                    continue;
                }

                // folders are in the following format: folder1/folder2/folder3/folder4
                var subFolders = functionGroup.Key.Replace(@"\", @"/").Split(@"/");
                var topFolder = subFolders.First();

                var folderKey = functionFolder.Urn;
                var folder = MetadataFactory.CreateFolderMetadata(databaseMetadata, folderKey, topFolder);
                functionFolders.SafeAdd(folderKey, folder);

                for (int i = 1; i < subFolders.Length; i++)
                {
                    folderKey = $"{folderKey}.{subFolders[i - 1]}";
                    var subFolder = MetadataFactory.CreateFolderMetadata(databaseMetadata, folderKey, subFolders[i]);
                    functionFolders.SafeAdd(folderKey, subFolder);
                }
            }

            foreach (var folder in functionFolders)
            {
                _folderMetadata.AddRange(folder.Key, folder.Value.Values.ToList());
            }
        }

        private void SetColumnMetadata(string databaseName, string tableName, IEnumerable<ColumnInfo> columnInfos)
        {
            var columns = columnInfos
                .Where(row =>
                    !string.IsNullOrWhiteSpace(row.Table)
                    && !string.IsNullOrWhiteSpace(row.Name)
                    && !string.IsNullOrWhiteSpace(row.DataType));

            var columnMetadatas = new SortedList<string, ColumnMetadata>();
            
            foreach (ColumnInfo columnInfo in columns)
            {
                var column = new ColumnMetadata
                {
                    ClusterName = ClusterName,
                    DatabaseName = databaseName,
                    TableName = tableName,
                    MetadataType = DataSourceMetadataType.Column,
                    MetadataTypeName = DataSourceMetadataType.Column.ToString(),
                    Name = columnInfo.Name,
                    PrettyName = columnInfo.Name,
                    Urn = $"{ClusterName}.{databaseName}.{tableName}.{columnInfo.Name}",
                    DataType = columnInfo.DataType
                };

                columnMetadatas.Add(column.PrettyName, column);
            }

            _columnMetadata[GenerateMetadataKey(databaseName, tableName)] = columnMetadatas.Values;
        }

        private void SetTableMetadata(DataSourceObjectMetadata databaseName, IEnumerable<TableInfo> tableInfos, string rootTableFolderKey)
        {
            var tableFolders = new Dictionary<string, List<TableMetadata>>
            {
                {$"{DatabaseKeyPrefix}.{databaseName.Urn}", new List<TableMetadata>()}
            };
            
            foreach (var table in tableInfos)
            {
                var tableKey = new StringBuilder(rootTableFolderKey);

                if (!string.IsNullOrWhiteSpace(table.Folder))
                {
                    tableKey.Append($".{table.Folder}");
                }

                var tableMetadata = new TableMetadata
                {
                    ClusterName = ClusterName,
                    DatabaseName = databaseName.Name,
                    MetadataType = DataSourceMetadataType.Table,
                    MetadataTypeName = DataSourceMetadataType.Table.ToString(),
                    Name = table.TableName,
                    PrettyName = table.TableName,
                    Folder = table.Folder,
                    Urn = $"{tableKey}.{table.TableName}"
                };

                if (tableFolders.ContainsKey(tableKey.ToString()))
                {
                    tableFolders[tableKey.ToString()].Add(tableMetadata);
                }
                else
                {
                    tableFolders[tableKey.ToString()] = new List<TableMetadata>{tableMetadata};
                }
                
                // keep a list of all tables for the database
                // this is used for the dashboard
                tableFolders[$"{DatabaseKeyPrefix}.{databaseName.Urn}"].Add(tableMetadata);
            }
            
            foreach (var table in tableFolders)
            {
                _tableMetadata.AddRange(table.Key, table.Value);
            }
        }

        private void SetFunctionMetadata(string databaseName, string rootFunctionFolderKey,
            List<IGrouping<string, FunctionInfo>> functionGroupByFolder)
        {
            foreach (var functionGroup in functionGroupByFolder)
            {
                var stringBuilder = new StringBuilder(rootFunctionFolderKey);
                
                if (!string.IsNullOrWhiteSpace(functionGroup.Key))
                {
                    stringBuilder.Append(".");
                    
                    // folders are in the following format: folder1/folder2/folder3/folder4
                    var folderKey = functionGroup.Key
                        .Replace(@"\", ".")
                        .Replace(@"/", ".");
                    
                    stringBuilder.Append(folderKey);
                }

                var functionKey = $"{ClusterName}.{databaseName}.{stringBuilder}";
                var functions = new List<FunctionMetadata>();
                
                foreach (FunctionInfo functionInfo in functionGroup)
                {
                    var function = new FunctionMetadata
                    {
                        DatabaseName = databaseName,
                        Parameters = functionInfo.Parameters,
                        Body = functionInfo.Body,
                        MetadataType = DataSourceMetadataType.Function,
                        MetadataTypeName = DataSourceMetadataType.Function.ToString(),
                        Name = $"{functionInfo.Name}{functionInfo.Parameters}",
                        PrettyName = functionInfo.Name,
                        Urn = $"{functionKey}.{functionInfo.Name}"
                    };

                    functions.Add(function);
                }

                _functionMetadata.AddRange(functionKey, functions);
            }
        }

        private IEnumerable<FunctionInfo> GetFunctionInfos(string databaseName)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            string query = ".show functions";

            using (var reader = _kustoClient.ExecuteQuery(query, token, databaseName))
            {
                return reader.ToEnumerable()
                    .Select(row => new FunctionInfo
                    {
                        Name = row["Name"]?.ToString(),
                        Body = row["Body"]?.ToString(),
                        DocString = row["DocString"]?.ToString(),
                        Folder = row["Folder"]?.ToString(),
                        Parameters = row["Parameters"]?.ToString()
                    })
                    .Materialize();
            }
        }

        private FunctionInfo GetFunctionInfo(string functionName)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            string query = $".show function {functionName}";

            using (var reader = _kustoClient.ExecuteQuery(query, token, DatabaseName))
            {
                return reader.ToEnumerable()
                    .Select(row => new FunctionInfo
                    {
                        Name = row["Name"]?.ToString(),
                        Body = row["Body"]?.ToString(),
                        DocString = row["DocString"]?.ToString(),
                        Folder = row["Folder"]?.ToString(),
                        Parameters = row["Parameters"]?.ToString()
                    })
                    .FirstOrDefault();
            }
        }

        public override string GenerateAlterFunctionScript(string functionName)
        {
            var functionInfo = GetFunctionInfo(functionName);
            
            if (functionInfo == null)
            {
                return string.Empty;
            }
            
            var alterCommand = new StringBuilder();

            alterCommand.Append(".alter function with ");
            alterCommand.Append($"(folder = \"{functionInfo.Folder}\", docstring = \"{functionInfo.DocString}\", skipvalidation = \"false\" ) ");
            alterCommand.Append($"{functionInfo.Name}{functionInfo.Parameters} ");
            alterCommand.Append($"{functionInfo.Body}");

            return alterCommand.ToString();
        }

        public override string GenerateExecuteFunctionScript(string functionName)
        {
            var functionInfo = GetFunctionInfo(functionName);
            
            return functionInfo == null 
                ? string.Empty 
                : $"{functionInfo.Name}{functionInfo.Parameters}";
        }
        
        private string GenerateMetadataKey(string databaseName, string objectName)
        {
            return string.IsNullOrWhiteSpace(objectName) ? databaseName : $"{databaseName}.{objectName}";
        }

        public override ScriptFileMarker[] GetSemanticMarkers(ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText)
        {
            return _intellisenseClient.GetSemanticMarkers(parseInfo, scriptFile, queryText);
        }

        public override DefinitionResult GetDefinition(string queryText, int index, int startLine, int startColumn, bool throwOnError = false)
        {
            return _intellisenseClient.GetDefinition(queryText, index, startLine, startColumn, throwOnError);
        }

        public override Hover GetHoverHelp(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false)
        {
            return _intellisenseClient.GetHoverHelp(scriptDocumentInfo, textPosition, throwOnError);
        }

        public override CompletionItem[] GetAutoCompleteSuggestions(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false)
        {
            return _intellisenseClient.GetAutoCompleteSuggestions(scriptDocumentInfo, textPosition, throwOnError);
        }

        public override ListDatabasesResponse GetDatabases(string serverName, bool includeDetails)
        {
            DataSourceObjectMetadata objectMetadata = MetadataFactory.CreateClusterMetadata(serverName);

            // Mainly used by "manage" dashboard
            if (includeDetails)
            {
                IEnumerable<DataSourceObjectMetadata> databaseMetadataInfo = GetChildObjects(objectMetadata, true);
                List<DatabaseInfo> metadata = MetadataFactory.ConvertToDatabaseInfo(databaseMetadataInfo);

                return new ListDatabasesResponse
                {
                    Databases = metadata.ToArray()
                };
            }

            IEnumerable<DataSourceObjectMetadata> databaseMetadata = GetChildObjects(objectMetadata);
            if (databaseMetadata != null)
            {
                return new ListDatabasesResponse
                {
                    DatabaseNames = databaseMetadata
                        .Select(objMeta => objMeta.PrettyName == objMeta.Name ? objMeta.PrettyName : $"{objMeta.PrettyName} ({objMeta.Name})")
                        .ToArray()
                };
            }

            return new ListDatabasesResponse();;
        }
        
        public override DatabaseInfo GetDatabaseInfo(string serverName, string databaseName)
        {
            DataSourceObjectMetadata objectMetadata = MetadataFactory.CreateClusterMetadata(serverName);
            var metadata = GetChildObjects(objectMetadata, true).Where(o => o.Name == databaseName).ToList();
            List<DatabaseInfo> databaseInfo = MetadataFactory.ConvertToDatabaseInfo(metadata);
            return databaseInfo.ElementAtOrDefault(0);
        }

        #endregion
    }
}
