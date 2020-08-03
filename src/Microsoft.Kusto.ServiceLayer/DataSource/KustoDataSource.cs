// <copyright file="KustoUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>
using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    /// <summary>
    /// Represents Kusto utilities.
    /// </summary>
    public class KustoDataSource : DataSourceBase
    {
        private ICslQueryProvider _kustoQueryProvider;

        private ICslAdminProvider _kustoAdminProvider;

        /// <summary>
        /// List of databases.
        /// </summary>
        private IEnumerable<DataSourceObjectMetadata> _databaseMetadata;

        /// <summary>
        /// List of tables per database. Key - DatabaseName
        /// </summary>
        private ConcurrentDictionary<string, IEnumerable<TableMetadata>> _tableMetadata = new ConcurrentDictionary<string, IEnumerable<TableMetadata>>();

        /// <summary>
        /// List of columns per table. Key - DatabaseName.TableName
        /// </summary>
        private ConcurrentDictionary<string, IEnumerable<DataSourceObjectMetadata>> _columnMetadata = new ConcurrentDictionary<string, IEnumerable<DataSourceObjectMetadata>>();
        
        /// <summary>
        /// List of tables per database. Key - DatabaseName
        /// </summary>
        private ConcurrentDictionary<string, IEnumerable<FolderMetadata>> _folderMetadata = new ConcurrentDictionary<string, IEnumerable<FolderMetadata>>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// List of functions per database. Key - DatabaseName.Folder
        /// </summary>
        private ConcurrentDictionary<string, IEnumerable<FunctionMetadata>> _functionMetadata = new ConcurrentDictionary<string, IEnumerable<FunctionMetadata>>();

        // Some clusters have this signature. Queries might slightly differ for Aria
        public const string AriaProxyURL = "kusto.aria.microsoft.com"; 

        /// <summary>
        /// The database schema query.  Performance: ".show database schema" is more efficient than ".show schema",
        /// especially for large clusters with many databases or tables.
        /// </summary>
        public const string ShowDatabaseSchema = ".show database [{0}] schema";

        /// <summary>
        /// Prevents a default instance of the <see cref="IDataSource"/> class from being created.
        /// </summary>
        public KustoDataSource(string connectionString, string azureAccountToken)
        {
            ClusterName = GetClusterName(connectionString);
            DatabaseName = GetDatabaseName(connectionString);
            UserToken = azureAccountToken;

            // Check if a connection can be made
            ValidationUtils.IsTrue<ArgumentException>(Exists().Result, $"Unable to connect. ClusterName = {ClusterName}, DatabaseName = {DatabaseName}");
        }

        /// <summary>
        /// Extracts the cluster name from the connectionstring. The string looks like the following:
        /// "Data Source=clustername.kusto.windows.net;User ID=;Password=;Pooling=False;Application Name=azdata-GeneralConnection"
        /// <summary>
        /// <param name="connectionString">A connection string coming over the Data management protocol</param>
        private static string GetClusterName(string connectionString)
        {
            var csb = new SqlConnectionStringBuilder(connectionString);

            // If there is no https:// prefix, add it
            Uri uri;
            if ((Uri.TryCreate(csb.DataSource, UriKind.Absolute, out uri) || Uri.TryCreate("https://" + csb.DataSource, UriKind.Absolute, out uri)) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return uri.AbsoluteUri;
            }

            throw new ArgumentException("Expected a URL of the form clustername.kusto.windows.net");
        }

        /// <summary>
        /// Extracts the database name from the connectionstring, if it exists
        /// <summary>
        /// <param name="connectionString">A connection string coming over the Data management protocol</param>
        private static string GetDatabaseName(string connectionString)
        {
            var csb = new SqlConnectionStringBuilder(connectionString);

            return csb.InitialCatalog;
        }

        /// <summary>
        /// The AAD user token.
        /// </summary>
        public string UserToken { get; private set; }

        /// <summary>
        /// The AAD application client id.
        /// </summary>
        public string ApplicationClientId { get; private set; }

        /// <summary>
        /// The AAD application client key.
        /// </summary>
        public string ApplicationKey { get; private set; }

        // The Kusto query provider.
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ICslQueryProvider KustoQueryProvider
        {
            get
            {
                if (_kustoQueryProvider == null)
                {
                    var kcsb = GetKustoConnectionStringBuilder();
                    _kustoQueryProvider = KustoClientFactory.CreateCslQueryProvider(kcsb);
                }

                return _kustoQueryProvider;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ICslAdminProvider KustoAdminProvider
        {
            get
            {
                if (_kustoAdminProvider == null)
                {
                    var kcsb = GetKustoConnectionStringBuilder();
                    _kustoAdminProvider = KustoClientFactory.CreateCslAdminProvider(kcsb);
                    if (!string.IsNullOrWhiteSpace(DatabaseName))
                    {
                        _kustoAdminProvider.DefaultDatabaseName = DatabaseName;
                    }
                }

                return _kustoAdminProvider;
            }
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
                _kustoQueryProvider?.Dispose();
                _kustoQueryProvider = null;

                _kustoAdminProvider?.Dispose();
                _kustoAdminProvider = null;
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
            var reader = ExecuteQuery(query, cancellationToken, databaseName);
            return Task.FromResult(reader);
        }

        private IDataReader ExecuteQuery(string query, CancellationToken cancellationToken, string databaseName = null)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(query, nameof(query));

            var clientRequestProperties = new ClientRequestProperties
            {
                ClientRequestId = Guid.NewGuid().ToString()
            };
            clientRequestProperties.SetOption(ClientRequestProperties.OptionNoTruncation, true);

            if(cancellationToken != null)
            {
                cancellationToken.Register(() => CancelQuery(clientRequestProperties.ClientRequestId));
            }

            IDataReader origReader = KustoQueryProvider.ExecuteQuery(
                KustoQueryUtils.IsClusterLevelQuery(query) ? "" : databaseName, 
                query, 
                clientRequestProperties);

            return new KustoResultsReader(origReader);
        }

        private void CancelQuery(string clientRequestId)
        {
            var query = ".cancel query " + clientRequestId;
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            using (var reader = ExecuteQuery(query, token))
            {
                // No-op
            }
        }

        /// <inheritdoc/>
        public override async Task<bool> Exists()
        {
            try
            {
                CancellationTokenSource source = new CancellationTokenSource();
                CancellationToken token = source.Token;

                var count = await ExecuteScalarQueryAsync<long>(".show databases | count", token);
                return count >= 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        /// <summary>
        /// Executes a Kusto control command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void ExecuteControlCommand(string command)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(command, nameof(command));

            using (var adminOutput = KustoAdminProvider.ExecuteControlCommand(command, null))
            {
            }
        }

        private KustoConnectionStringBuilder GetKustoConnectionStringBuilder()
        {
            ValidationUtils.IsNotNull(ClusterName, nameof(ClusterName));
            ValidationUtils.IsTrue<ArgumentException>(
                !string.IsNullOrWhiteSpace(UserToken)
                || (!string.IsNullOrWhiteSpace(ApplicationClientId) && !string.IsNullOrWhiteSpace(ApplicationKey)),
                $"the Kusto authentication is not specified - either set {nameof(UserToken)}, or set {nameof(ApplicationClientId)} and {nameof(ApplicationKey)}");

            var kcsb = new KustoConnectionStringBuilder
            {
                DataSource = ClusterName,

                // Perform federated auth based on the AAD user token, or based on the AAD application client id and key.
                FederatedSecurity = true
            };

            if (!string.IsNullOrWhiteSpace(DatabaseName))
            {
                kcsb.InitialCatalog = DatabaseName;
            }

            if (!string.IsNullOrWhiteSpace(UserToken))
            {
                kcsb.UserToken = UserToken;
            }

            if (!string.IsNullOrWhiteSpace(ApplicationClientId))
            {
                kcsb.ApplicationClientId = ApplicationClientId;
            }

            if (!string.IsNullOrWhiteSpace(ApplicationKey))
            {
                kcsb.ApplicationKey = ApplicationKey;
            }

            return kcsb;
        }

        #region IDataSource

        /// <inheritdoc/>
        private IEnumerable<DataSourceObjectMetadata> GetDatabaseMetadata()
        {
            if (_databaseMetadata == null)
            {
                CancellationTokenSource source = new CancellationTokenSource();
                CancellationToken token = source.Token;

                // Getting database names when we are connected to a specific database should not happen.
                ValidationUtils.IsNotNull(DatabaseName, nameof(DatabaseName)); 

                var query = ".show databases" + (this.ClusterName.IndexOf(AriaProxyURL, StringComparison.CurrentCultureIgnoreCase) == -1 ? " | project DatabaseName, PrettyName" : "");
                using (var reader = ExecuteQuery(query, token))
                {
                    var schemaTable = reader.GetSchemaTable();
                    
                    _databaseMetadata = reader.ToEnumerable()
                        .Where(row => !string.IsNullOrWhiteSpace(row["DatabaseName"].ToString()))
                        .Select(row => new DatabaseMetadata
                        {
                            ClusterName = this.ClusterName,
                            MetadataType = DataSourceMetadataType.Database,
                            MetadataTypeName = DataSourceMetadataType.Database.ToString(),
                            Name = row["DatabaseName"].ToString(),
                            PrettyName = String.IsNullOrEmpty(row["PrettyName"]?.ToString()) ? row["DatabaseName"].ToString() : row["PrettyName"].ToString(),
                            Urn = $"{this.ClusterName}.{row["DatabaseName"].ToString()}"
                        })
                        .Materialize()
                        .OrderBy(row => row.Name, StringComparer.Ordinal); // case-sensitive
                }
            }

            return _databaseMetadata;
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

        /// <inheritdoc/>
        public override void Refresh()
        {
            // This class caches objects. Throw them away so that the next call will re-query the data source for the objects.
            _databaseMetadata = null;
            _tableMetadata = new ConcurrentDictionary<string, IEnumerable<TableMetadata>>();
            _columnMetadata  = new ConcurrentDictionary<string, IEnumerable<DataSourceObjectMetadata>>();
            _folderMetadata = new ConcurrentDictionary<string, IEnumerable<FolderMetadata>>(StringComparer.OrdinalIgnoreCase);
            _functionMetadata = new ConcurrentDictionary<string, IEnumerable<FunctionMetadata>>();
        }

        /// <inheritdoc/>
        public override void Refresh(DataSourceObjectMetadata objectMetadata)
        {
            ValidationUtils.IsNotNull(objectMetadata, nameof(objectMetadata));

            switch(objectMetadata.MetadataType)
            {
                case DataSourceMetadataType.Cluster:
                    Refresh();
                    break;

                case DataSourceMetadataType.Database:
                    _tableMetadata.TryRemove(objectMetadata.Name, out _);
                    break;

                case DataSourceMetadataType.Table:
                    var tm = objectMetadata as TableMetadata;
                    _columnMetadata.TryRemove(GenerateMetadataKey(tm.DatabaseName, tm.Name), out _);
                    break;

                case DataSourceMetadataType.Column:
                    // Remove column metadata for the whole table
                    var cm = objectMetadata as ColumnMetadata;
                    _columnMetadata.TryRemove(GenerateMetadataKey(cm.DatabaseName, cm.TableName), out _);
                    break;
                
                case DataSourceMetadataType.Function:
                    var fm = objectMetadata as FunctionMetadata;
                    _functionMetadata.TryRemove(GenerateMetadataKey(fm.DatabaseName, fm.Name), out _);
                    break;
                
                case DataSourceMetadataType.Folder:
                    var folder = objectMetadata as FolderMetadata;
                    _folderMetadata.TryRemove(GenerateMetadataKey(folder.ParentMetadata.Name, folder.Name), out _);
                    break;

                default:
                    throw new ArgumentException($"Unexpected type {objectMetadata.MetadataType}.");
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<DataSourceObjectMetadata> GetChildObjects(DataSourceObjectMetadata objectMetadata)
        {
            ValidationUtils.IsNotNull(objectMetadata, nameof(objectMetadata));

            switch(objectMetadata.MetadataType)
            {
                case DataSourceMetadataType.Cluster: // show databases
                    return GetDatabaseMetadata();
                    
                case DataSourceMetadataType.Database: // show folders or tables
                    return GetDatabaseSchema(objectMetadata);

                case DataSourceMetadataType.Table: // show columns
                    TableMetadata tm = objectMetadata as TableMetadata;
                    return _columnMetadata[GenerateMetadataKey(tm.DatabaseName, tm.Name)].OrderBy(x => x.PrettyName);

                case DataSourceMetadataType.Folder: // show tables
                    FolderMetadata fm = objectMetadata as FolderMetadata;
                    var metadataKey = GenerateMetadataKey(fm.ParentMetadata.Name, fm.Name);
                    return GetAllMetadata(metadataKey);

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
                        
            LoadTableSchema(objectMetadata);
            LoadFunctionSchema(objectMetadata);

            return GetAllMetadata(objectMetadata.Name);
        }

        private IEnumerable<DataSourceObjectMetadata> GetAllMetadata(string key)
        {
            var returnList = new List<DataSourceObjectMetadata>();

            if (_folderMetadata.ContainsKey(key))
            {
                returnList.AddRange(_folderMetadata[key].OrderBy(x => x.PrettyName, StringComparer.OrdinalIgnoreCase));
            }

            if (_tableMetadata.ContainsKey(key))
            {
                returnList.AddRange(_tableMetadata[key].OrderBy(x => x.PrettyName, StringComparer.OrdinalIgnoreCase));
            }

            if (_functionMetadata.ContainsKey(key))
            {
                returnList.AddRange(_functionMetadata[key].OrderBy(x => x.PrettyName, StringComparer.OrdinalIgnoreCase));
            }
            
            return returnList;
        }

        private IEnumerable<ColumnInfo> GetColumnMetadata(string databaseName)
        {
            ValidationUtils.IsNotNullOrWhitespace(databaseName, nameof(databaseName));

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            const string SystemPrefix = "System.";
            var query = string.Format(CultureInfo.InvariantCulture, ShowDatabaseSchema, databaseName)
                + " | project TableName, ColumnName, ColumnType, Folder";
            
            using (var reader = ExecuteQuery(query, token, databaseName))
            {
                var schemaTable = reader.GetSchemaTable();
                // TODOKusto: Remove if not needed. We could index using the names directly.
                var tableNameProperty = schemaTable.Columns["TableName"];
                var columnNameProperty = schemaTable.Columns["ColumnName"];
                var ColumnTypeProperty = schemaTable.Columns["ColumnType"];

                var columns = reader.ToEnumerable()
                    .Select(row => new ColumnInfo
                    {
                        Table = row["TableName"]?.ToString(),
                        Name = row["ColumnName"]?.ToString(),
                        DataType = row["ColumnType"]?.ToString().TrimPrefix(SystemPrefix),
                        Folder = row["Folder"]?.ToString()
                    })
                    .Materialize()
                    .OrderBy(row => row.Name, StringComparer.Ordinal); // case-sensitive

                return columns;
            }
        }

        private void LoadTableSchema(DataSourceObjectMetadata objectMetadata)
        {
            IEnumerable<ColumnInfo> columnInfos = GetColumnMetadata(objectMetadata.Name).Materialize();

            if (!columnInfos.Any())
            {
                return;
            }
            
            if (columnInfos.Any(x => !string.IsNullOrWhiteSpace(x.Folder)))
            {
                SetFolderMetadataForTables(objectMetadata, columnInfos);
            }
            
            var columnsGroupByTable = columnInfos
                .Where(row =>
                    !string.IsNullOrWhiteSpace(row.Table)
                    && !string.IsNullOrWhiteSpace(row.Name)
                    && !string.IsNullOrWhiteSpace(row.DataType))
                .GroupBy(row => row.Table, StringComparer.OrdinalIgnoreCase);
            
            SetTableMetadata(objectMetadata.Name, columnInfos);
            SetColumnMetadata(objectMetadata.Name, columnsGroupByTable);
        }

        private void SetFolderMetadataForTables(DataSourceObjectMetadata objectMetadata, IEnumerable<ColumnInfo> columnInfos)
        {
            // create Table folder to hold functions tables
            var tableFolder = new FolderMetadata
            {
                ParentMetadata = objectMetadata,
                Name = "Tables",
                MetadataTypeName = DataSourceMetadataType.Folder.ToString(),
                MetadataType = DataSourceMetadataType.Folder,
                PrettyName = "Tables",
                Urn = $"{ClusterName}.{objectMetadata.Name}.Tables"
            };

            AddToFolderMetadata(objectMetadata.Name, new List<FolderMetadata> {tableFolder});
            
            var columnsGroupByFolder = columnInfos
                .GroupBy(x => x.Folder, StringComparer.OrdinalIgnoreCase)
                .ToList();
            
            var tableFolders = new List<FolderMetadata>();

            foreach (var columnGroup in columnsGroupByFolder)
            {
                // skip tables without folders
                if (string.IsNullOrWhiteSpace(columnGroup.Key))
                {
                    continue;
                }

                var folder = new FolderMetadata
                {
                    ParentMetadata = objectMetadata,
                    Name = columnGroup.Key,
                    MetadataTypeName = DataSourceMetadataType.Folder.ToString(),
                    MetadataType = DataSourceMetadataType.Folder,
                    PrettyName = columnGroup.Key,
                    Urn = $"{ClusterName}.{objectMetadata.Name}.{columnGroup.Key}"
                };

                tableFolders.Add(folder);
            }

            string functionFolderKey = GenerateMetadataKey(objectMetadata.Name, tableFolder.Name);
            AddToFolderMetadata(functionFolderKey, tableFolders);
        }

        private void AddToFolderMetadata(string databaseName, List<FolderMetadata> folders)
        {
            if (_folderMetadata.ContainsKey(databaseName))
            {
                folders.AddRange(_folderMetadata[databaseName]);
            }
            
            _folderMetadata[databaseName] = folders.OrderBy(x => x.PrettyName, StringComparer.OrdinalIgnoreCase);
        }

        private void LoadFunctionSchema(DataSourceObjectMetadata objectMetadata)
        {
            IEnumerable<FunctionInfo> functionInfos = GetFunctionInfos(objectMetadata.Name);

            if (!functionInfos.Any())
            {
                return;
            }

            // create Functions folder to hold functions folders
            var functionFolder = new FolderMetadata
            {
                ParentMetadata = objectMetadata,
                Name = "Functions",
                MetadataTypeName = DataSourceMetadataType.Folder.ToString(),
                MetadataType = DataSourceMetadataType.Folder,
                PrettyName = "Functions",
                Urn = $"{ClusterName}.{objectMetadata.Name}.Functions"
            };

            AddToFolderMetadata(objectMetadata.Name, new List<FolderMetadata>{functionFolder});

            // create each folder to hold functions
            var functionsGroupByFolder = functionInfos
                .GroupBy(x => x.Folder, StringComparer.OrdinalIgnoreCase)
                .ToList();
            
            var folders = new List<FolderMetadata>();
            foreach (var functionGroup in functionsGroupByFolder)
            {
                // skip functions with no folder
                if (string.IsNullOrWhiteSpace(functionGroup.Key))
                {
                    continue;
                }
                
                var folder = new FolderMetadata
                {
                    ParentMetadata = objectMetadata,
                    Name = functionGroup.Key,
                    MetadataTypeName = DataSourceMetadataType.Folder.ToString(),
                    MetadataType = DataSourceMetadataType.Folder,
                    PrettyName = functionGroup.Key,
                    Urn = $"{ClusterName}.{objectMetadata.Name}.{functionGroup.Key}"
                };

                folders.Add(folder);
            }
            
            string functionFolderKey = GenerateMetadataKey(objectMetadata.Name, functionFolder.Name);
            AddToFolderMetadata(functionFolderKey, folders);

            SetFunctionMetadata(objectMetadata.Name, functionFolder.Name, functionsGroupByFolder);
        }

        private void SetColumnMetadata(string databaseName, IEnumerable<IGrouping<string, ColumnInfo>> columnsGroupByTable)
        {
            foreach (var columnGroup in columnsGroupByTable)
            {
                string tableName = columnGroup.Key;
                var columns = new List<ColumnMetadata>();
                foreach (ColumnInfo columnInfo in columnGroup)
                {
                    var column = new ColumnMetadata
                    {
                        ClusterName = this.ClusterName,
                        DatabaseName = databaseName,
                        TableName = tableName,
                        MetadataType = DataSourceMetadataType.Column,
                        MetadataTypeName = DataSourceMetadataType.Column.ToString(),
                        Name = columnInfo.Name,
                        PrettyName = columnInfo.Name,
                        Urn = $"{this.ClusterName}.{databaseName}.{tableName}.{columnInfo.Name}",
                        DataType = columnInfo.DataType
                    };
                    
                    columns.Add(column);
                }

                _columnMetadata[GenerateMetadataKey(databaseName, tableName)] = columns;
            }
        }

        private void SetTableMetadata(string databaseName, IEnumerable<ColumnInfo> columnInfos)
        {
            var columnInfoTables =
                columnInfos.Where(x => !string.IsNullOrWhiteSpace(x.Table)
                                       && string.IsNullOrWhiteSpace(x.Name));

            var tables = new List<TableMetadata>();
            foreach (var columnInfoTable in columnInfoTables)
            {
                string key = GenerateMetadataKey(databaseName, columnInfoTable.Folder);

                var table = new TableMetadata
                {
                    ClusterName = ClusterName,
                    DatabaseName = databaseName,
                    MetadataType = DataSourceMetadataType.Table,
                    MetadataTypeName = DataSourceMetadataType.Table.ToString(),
                    Name = columnInfoTable.Table,
                    PrettyName = columnInfoTable.Table,
                    Folder = columnInfoTable.Folder,
                    Urn = $"{this.ClusterName}.{key}.{columnInfoTable.Table}"
                };

                tables.Add(table);
            }

            var folders = tables.GroupBy(x => x.Folder);

            foreach (var folder in folders)
            {
                _tableMetadata[GenerateMetadataKey(databaseName, folder.Key)] = folder.ToList();    
            }
        }

        private void SetFunctionMetadata(string databaseName, string functionFolderName,
            List<IGrouping<string, FunctionInfo>> functionGroupByFolder)
        {
            foreach (var functionGroup in functionGroupByFolder)
            {
                var functions = new SortedList<string, FunctionMetadata>(StringComparer.CurrentCultureIgnoreCase);
                
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
                        Urn = $"{ClusterName}.{GenerateMetadataKey(databaseName, functionInfo.Folder)}.{functionInfo.Name}"
                    };

                    functions.Add(function.Name, function);
                }

                string functionFolder = string.IsNullOrWhiteSpace(functionGroup.Key) ? functionFolderName : functionGroup.Key;
                string key = GenerateMetadataKey(databaseName, functionFolder); 
                _functionMetadata[key] = functions.Values;
            }
        }

        private IEnumerable<FunctionInfo> GetFunctionInfos(string databaseName)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            string query = ".show functions";

            using (var reader = ExecuteQuery(query, token, databaseName))
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

        internal FunctionInfo GetFunctionInfo(string functionName)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            string query = $".show function {functionName}";

            using (var reader = ExecuteQuery(query, token, DatabaseName))
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

        private string GenerateMetadataKey(string databaseName, string objectName)
        {
            return string.IsNullOrWhiteSpace(objectName) ? databaseName : $"{databaseName}.{objectName}";
        }
        #endregion
    }
}
