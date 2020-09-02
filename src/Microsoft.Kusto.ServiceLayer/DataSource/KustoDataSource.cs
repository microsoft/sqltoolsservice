// <copyright file="KustoDataSource.cs" company="Microsoft">
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
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Data;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Data;
using Kusto.Data.Net.Client;
using Kusto.Language;
using Kusto.Language.Editor;
using Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.DataSource.Models;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Completion;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;

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
        public KustoDataSource(string connectionString, string azureAccountToken)
        {
            ClusterName = GetClusterName(connectionString);
            DatabaseName = GetDatabaseName(connectionString);
            UserToken = azureAccountToken;
            SchemaState = Task.Run(() =>
                KustoIntellisenseHelper.AddOrUpdateDatabaseAsync(this, GlobalState.Default, DatabaseName, ClusterName,
                    throwOnError: false)).Result;
            // Check if a connection can be made
            ValidationUtils.IsTrue<ArgumentException>(Exists().Result,
                $"Unable to connect. ClusterName = {ClusterName}, DatabaseName = {DatabaseName}");
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
        /// SchemaState used for getting intellisense info.
        /// </summary>
        public GlobalState SchemaState { get; private set; }

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

        protected DiagnosticsInfo GetClusterDiagnostics(){
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            DiagnosticsInfo clusterDiagnostics = new DiagnosticsInfo();

            var query =  ".show diagnostics | extend Passed= (IsHealthy) and not(IsScaleOutRequired) | extend Summary = strcat('Cluster is ', iif(Passed, '', 'NOT'), 'healthy.'),Details=pack('MachinesTotal', MachinesTotal, 'DiskCacheCapacity', round(ClusterDataCapacityFactor,1)) | project Action = 'Cluster Diagnostics', Category='Info', Summary, Details;";
            using (var reader = ExecuteQuery(query, token))
                {
                    while(reader.Read()) 
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

            return _databaseMetadata;
        }

        private void SetDatabaseMetadata(bool includeSizeDetails)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            // Getting database names when we are connected to a specific database should not happen.
            ValidationUtils.IsNotNull(DatabaseName, nameof(DatabaseName));

            var query = ".show databases" + (this.ClusterName.IndexOf(AriaProxyURL, StringComparison.CurrentCultureIgnoreCase) == -1 ? " | project DatabaseName, PrettyName" : "");
                
            if (includeSizeDetails == true){
                query =  ".show cluster extents | summarize sum(OriginalSize) by tostring(DatabaseName)";
            }

            using (var reader = ExecuteQuery(query, token))
            {
                _databaseMetadata = reader.ToEnumerable()
                    .Where(row => !string.IsNullOrWhiteSpace(row["DatabaseName"].ToString()))
                    .Select(row => new DatabaseMetadata
                    {
                        ClusterName = this.ClusterName,
                        MetadataType = DataSourceMetadataType.Database,
                        MetadataTypeName = DataSourceMetadataType.Database.ToString(),
                        SizeInMB = includeSizeDetails == true ? row["sum_OriginalSize"].ToString() : null,
                        Name = row["DatabaseName"].ToString(),
                        PrettyName = includeSizeDetails == true ? row["DatabaseName"].ToString(): (String.IsNullOrEmpty(row["PrettyName"]?.ToString()) ? row["DatabaseName"].ToString() : row["PrettyName"].ToString()),
                        Urn = $"{this.ClusterName}.{row["DatabaseName"].ToString()}"
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
        public override void UpdateDatabase(string databaseName){
            DatabaseName = databaseName;
            SchemaState = Task.Run(() => KustoIntellisenseHelper.AddOrUpdateDatabaseAsync(this, GlobalState.Default, DatabaseName, ClusterName, throwOnError: false)).Result;
        }

        /// <inheritdoc/>
        public override LanguageServices.Contracts.CompletionItem[] GetAutoCompleteSuggestions(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false){
            var kustoCodeService = new KustoCodeService(scriptDocumentInfo.Contents, SchemaState);
            var script = CodeScript.From(scriptDocumentInfo.Contents, SchemaState);
            script.TryGetTextPosition(textPosition.Line + 1, textPosition.Character, out int position);     // Gets the actual offset based on line and local offset
            
            var completion = kustoCodeService.GetCompletionItems(position);
            scriptDocumentInfo.ScriptParseInfo.CurrentSuggestions = completion.Items;         // this is declaration item so removed for now, but keep the info when api gets updated

            List<LanguageServices.Contracts.CompletionItem> completions = new List<LanguageServices.Contracts.CompletionItem>();
            foreach (var autoCompleteItem in completion.Items)
            {
                var label = autoCompleteItem.DisplayText;
                completions.Add(AutoCompleteHelper.CreateCompletionItem(label, label + " keyword", label, KustoIntellisenseHelper.CreateCompletionItemKind(autoCompleteItem.Kind), scriptDocumentInfo.StartLine, scriptDocumentInfo.StartColumn, textPosition.Character));
            }

            return completions.ToArray();
        }

        /// <inheritdoc/>
        public override Hover GetHoverHelp(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false){
            var kustoCodeService = new KustoCodeService(scriptDocumentInfo.Contents, SchemaState);
            var script = CodeScript.From(scriptDocumentInfo.Contents, SchemaState);
            script.TryGetTextPosition(textPosition.Line + 1, textPosition.Character, out int position);

            var quickInfo = kustoCodeService.GetQuickInfo(position);

            return AutoCompleteHelper.ConvertQuickInfoToHover(
                                        quickInfo.Text,
                                        "kusto",
                                        scriptDocumentInfo.StartLine,
                                        scriptDocumentInfo.StartColumn,
                                        textPosition.Character);

        }

        /// <inheritdoc/>
        public override DefinitionResult GetDefinition(string queryText, int index, int startLine, int startColumn, bool throwOnError = false){
            var abc = KustoCode.ParseAndAnalyze(queryText, SchemaState);        //TODOKusto: API wasnt working properly, need to check that part.
            var kustoCodeService = new KustoCodeService(abc);
            //var kustoCodeService = new KustoCodeService(queryText, globals);
            var relatedInfo = kustoCodeService.GetRelatedElements(index);

            if (relatedInfo != null && relatedInfo.Elements.Count > 1)
            {

            }

            return null;
        }

        /// <inheritdoc/>
        public override ScriptFileMarker[] GetSemanticMarkers(ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText)
        {
            var kustoCodeService = new KustoCodeService(queryText, SchemaState);
            var script = CodeScript.From(queryText, SchemaState);
            var parseResult = kustoCodeService.GetDiagnostics();
            

            parseInfo.ParseResult = parseResult;
            
            // build a list of Kusto script file markers from the errors.
            List<ScriptFileMarker> markers = new List<ScriptFileMarker>();
            if (parseResult != null && parseResult.Count() > 0)
            {
                foreach (var error in parseResult)
                {
                    script.TryGetLineAndOffset(error.Start, out var startLine, out var startOffset);
                    script.TryGetLineAndOffset(error.End, out var endLine, out var endOffset);

                    // vscode specific format for error markers.
                    markers.Add(new ScriptFileMarker()
                    {
                        Message = error.Message,
                        Level = ScriptFileMarkerLevel.Error,
                        ScriptRegion = new ScriptRegion()
                        {
                            File = scriptFile.FilePath,
                            StartLineNumber = startLine,
                            StartColumnNumber = startOffset,
                            StartOffset = 0,
                            EndLineNumber = endLine,
                            EndColumnNumber = endOffset,
                            EndOffset = 0
                        }
                    });
                }
            }

            return markers.ToArray();
        }

        /// <summary>
        /// Clears everything
        /// </summary>
        private void RefreshAll(bool includeDatabase)
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
                    RefreshAll(true);
                    SetDatabaseMetadata(false);
                    break;
                
                case DataSourceMetadataType.Database:
                    RefreshAll(false);
                    LoadTableSchema(objectMetadata);
                    LoadFunctionSchema(objectMetadata);
                    break;

                case DataSourceMetadataType.Table:
                    var table = objectMetadata as TableMetadata;
                    _columnMetadata.TryRemove(GenerateMetadataKey(table.DatabaseName, table.Name), out _);
                    SetTableSchema(table);
                    break;

                case DataSourceMetadataType.Folder:
                    RefreshAll(false);
                    var folder = objectMetadata as FolderMetadata;
                    LoadTableSchema(folder.DatabaseMetadata);
                    LoadFunctionSchema(folder.DatabaseMetadata);
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

            using (var reader = ExecuteQuery(query.ToString(), token, databaseName))
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

            string query = $".show database {databaseName} cslschema";

            using (var reader = ExecuteQuery(query, token, databaseName))
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

        private FunctionInfo GetFunctionInfo(string functionName)
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
        #endregion
    }
}
