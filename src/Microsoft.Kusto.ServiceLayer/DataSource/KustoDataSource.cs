// <copyright file="KustoUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>
using System;
using System.Collections.Generic;
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
using Microsoft.SqlServer.Management.Common;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    /// <summary>
    /// Represents Kusto utilities.
    /// </summary>
    public class KustoDataSource : DataSourceBase
    {
        private ICslQueryProvider kustoQueryProvider;

        private ICslAdminProvider kustoAdminProvider;

        /// <summary>
        /// List of databases.
        /// </summary>
        private IEnumerable<DataSourceObjectMetadata> databaseMetadata;

        /// <summary>
        /// List of tables per database. Key - DatabaseName
        /// </summary>
        private ConcurrentDictionary<string, IEnumerable<DataSourceObjectMetadata>> tableMetadata = new ConcurrentDictionary<string, IEnumerable<DataSourceObjectMetadata>>();

        /// <summary>
        /// List of columns per table. Key - DatabaseName.TableName
        /// </summary>
        private ConcurrentDictionary<string, IEnumerable<DataSourceObjectMetadata>> columnMetadata = new ConcurrentDictionary<string, IEnumerable<DataSourceObjectMetadata>>();

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
        static private string GetClusterName(string connectionString)
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
        static private string GetDatabaseName(string connectionString)
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

        /// <summary>
        /// The Kusto database name.
        /// </summary>
        public string DatabaseName { get; private set; }

        // The Kusto query provider.
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ICslQueryProvider KustoQueryProvider
        {
            get
            {
                if (kustoQueryProvider == null)
                {
                    var kcsb = GetKustoConnectionStringBuilder();
                    kustoQueryProvider = KustoClientFactory.CreateCslQueryProvider(kcsb);
                }

                return kustoQueryProvider;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ICslAdminProvider KustoAdminProvider
        {
            get
            {
                if (kustoAdminProvider == null)
                {
                    var kcsb = GetKustoConnectionStringBuilder();
                    kustoAdminProvider = KustoClientFactory.CreateCslAdminProvider(kcsb);
                    if (!string.IsNullOrWhiteSpace(DatabaseName))
                    {
                        kustoAdminProvider.DefaultDatabaseName = DatabaseName;
                    }
                }

                return kustoAdminProvider;
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
                kustoQueryProvider?.Dispose();
                kustoQueryProvider = null;

                kustoAdminProvider?.Dispose();
                kustoAdminProvider = null;
            }

            base.Dispose(disposing);
        }

        #region DataSourceUtils

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The results.</returns>
        public override Task<IDataReader> ExecuteQueryAsync(string query, string databaseName = null)
        {
            var reader = ExecuteQuery(query, databaseName);
            return Task.FromResult(reader);
        }

        private IDataReader ExecuteQuery(string query, string databaseName = null)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(query, nameof(query));

            var clientRequestProperties = new ClientRequestProperties
            {
                ClientRequestId = Guid.NewGuid().ToString()
            };
            clientRequestProperties.SetOption(ClientRequestProperties.OptionNoTruncation, true);

            return KustoQueryProvider.ExecuteQuery(databaseName, query, clientRequestProperties);
        }

        /// <inheritdoc/>
        public override async Task<bool> Exists()
        {
            try
            {
                var count = await ExecuteScalarQueryAsync<long>(".show databases | count");
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
        protected IEnumerable<DataSourceObjectMetadata> GetDatabaseMetadata()
        {
            if (databaseMetadata == null)
            {
                // Getting database names when we are connected to a specific database should not happen.
                ValidationUtils.IsNotNull(DatabaseName, nameof(DatabaseName)); 

                var query = ".show databases" + (this.ClusterName.IndexOf(AriaProxyURL, StringComparison.CurrentCultureIgnoreCase) == -1 ? " | project DatabaseName, PrettyName" : "");
                using (var reader = ExecuteQuery(query))
                {
                    var schemaTable = reader.GetSchemaTable();
                    // TODOKusto: Remove if not needed. Index using the name directly.
                    var databaseNameProperty = schemaTable.Columns["DatabaseName"];
                    var prettyNameProperty = schemaTable.Columns["PrettyName"];

                    databaseMetadata = reader.ToEnumerable()
                        .Where(row => !string.IsNullOrWhiteSpace(row["DatabaseName"].ToString()))
                        .Select(row => new DatabaseMetadata
                        {
                            ClusterName = this.ClusterName,
                            MetadataType = DataSourceMetadataType.Database,
                            MetadataTypeName = DataSourceMetadataType.Database.ToString(),
                            Name = row["DatabaseName"].ToString(),
                            PrettyName = row["PrettyName"]?.ToString(),
                            Urn = $"{this.ClusterName}.{row["DatabaseName"].ToString()}"
                        })
                        .Materialize();
                }
            }

            return databaseMetadata;
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
            databaseMetadata = null;
            tableMetadata = new ConcurrentDictionary<string, IEnumerable<DataSourceObjectMetadata>>();
            columnMetadata  = new ConcurrentDictionary<string, IEnumerable<DataSourceObjectMetadata>>();
        }

        /// <inheritdoc/>
        public override void Refresh(DataSourceObjectMetadata objectMetadata)
        {
            ValidationUtils.IsNotNull(objectMetadata, nameof(objectMetadata));

            IEnumerable<DataSourceObjectMetadata> discardOutput = null;

            switch(objectMetadata.MetadataType)
            {
                case DataSourceMetadataType.Cluster:
                    Refresh();
                    break;

                case DataSourceMetadataType.Database:
                    this.tableMetadata.TryRemove(objectMetadata.Name, out discardOutput);
                    break;

                case DataSourceMetadataType.Table:
                    TableMetadata tm = objectMetadata as TableMetadata;
                    this.columnMetadata.TryRemove(GenerateColumnMetadataKey(tm.DatabaseName, tm.Name), out discardOutput);
                    break;

                case DataSourceMetadataType.Column:
                    // Remove column metadata for the whole table
                    ColumnMetadata cm = objectMetadata as ColumnMetadata;
                    this.columnMetadata.TryRemove(GenerateColumnMetadataKey(cm.DatabaseName, cm.TableName), out discardOutput);
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
                case DataSourceMetadataType.Cluster:
                    return GetDatabaseMetadata();
                    
                case DataSourceMetadataType.Database:
                    return GetTableMetadata(objectMetadata.Name);

                case DataSourceMetadataType.Table:
                    TableMetadata tm = objectMetadata as TableMetadata;
                    GetSchema(tm.DatabaseName);
                    return columnMetadata[GenerateColumnMetadataKey(tm.DatabaseName, tm.Name)];

                case DataSourceMetadataType.Folder:
                    FolderMetadata fm = objectMetadata as FolderMetadata;
                    return GetFolderChildrenMetadata(fm);

                default:
                    throw new ArgumentException($"Unexpected type {objectMetadata.MetadataType}.");
            }
        }

        /// <summary>
        /// Get folders of the  given parent
        /// </summary>
        /// <param name="parentMetadata">Parent object metadata.</param>
        /// <returns>List of all children.</returns>
        public override IEnumerable<DataSourceObjectMetadata> GetChildFolders(DataSourceObjectMetadata objectMetadata)
        {
            // TODO: Add logic to add folders if they are defined in the Kusto schema
            return Enumerable.Empty<DataSourceObjectMetadata>();   
        }

        internal async Task<bool> DatabaseExists(string databaseName)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(databaseName, nameof(databaseName));

            try
            {
                var count = await ExecuteScalarQueryAsync<long>(".show tables | count", databaseName);
                return count >= 0;
            }
            catch
            {
                return false;
            }
        }


        /// <inheritdoc/>
        internal IEnumerable<DataSourceObjectMetadata> GetTableMetadata(string databaseName)
        {
            if (!tableMetadata.ContainsKey(databaseName))
            {
                // Check if the database exists
                ValidationUtils.IsTrue<ArgumentException>(DatabaseExists(databaseName).Result == true, $"Database '{databaseName}' does not exist.");

                GetSchema(databaseName);
            }

            return tableMetadata[databaseName];
        }

        internal IEnumerable<DataSourceObjectMetadata> GetFolderChildrenMetadata(DataSourceObjectMetadata objectMetadata)
        {
            // TODOKusto: Process folders
            return Enumerable.Empty<DataSourceObjectMetadata>();
        }

        public class ColumnInfo
        {
            /// <summary>
            /// The table name.
            /// </summary>
            public string Table { get; set; }

            /// <summary>
            /// The column name.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The data type.
            /// </summary>
            public string DataType { get; set; }
        }

        internal IEnumerable<ColumnInfo> GetColumnMetadata(string databaseName)
        {
            ValidationUtils.IsNotNullOrWhitespace(databaseName, nameof(databaseName));

            const string SystemPrefix = "System.";
            var query = string.Format(CultureInfo.InvariantCulture, ShowDatabaseSchema, databaseName)
                + " | project TableName, ColumnName, ColumnType";
            using (var reader = ExecuteQuery(query, databaseName))
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
                        DataType = row["ColumnType"]?.ToString().TrimPrefix(SystemPrefix)
                    })
                    .Where(row =>
                        !string.IsNullOrWhiteSpace(row.Table)
                        && !string.IsNullOrWhiteSpace(row.Name)
                        && !string.IsNullOrWhiteSpace(row.DataType))
                    .Materialize();

                return columns;
            }
        }

        /// <summary>
        /// Gets the table schema.
        /// </summary>
        /// <param name="databaseName">The database for which Schema needs to be pulled.</param>
        /// <returns>The schema.</returns>
        internal void GetSchema(string databaseName)
        {
            ValidationUtils.IsNotNull(databaseName, nameof(databaseName));

            if(!tableMetadata.ContainsKey(databaseName))
            {
                var columns = GetColumnMetadata(databaseName).Materialize();
                
                if (!columns.Any())
                {
                    tableMetadata[databaseName] = Enumerable.Empty<DataSourceObjectMetadata>();
                }

                var tableGroups = columns
                    .GroupBy(row => row.Table, StringComparer.Ordinal); // case-sensitive

                // Get all table names
                tableMetadata[databaseName] = tableGroups
                    .Select(grouping => new TableMetadata
                            {
                                ClusterName = this.ClusterName,
                                DatabaseName = databaseName,
                                MetadataType = DataSourceMetadataType.Table,
                                MetadataTypeName = DataSourceMetadataType.Table.ToString(),
                                Name = grouping.Key, // table name
                                PrettyName = grouping.Key,
                                Urn = $"{this.ClusterName}.{databaseName}.{grouping.Key}"
                            })
                    .OrderBy(row => row.Name, StringComparer.Ordinal); // case-sensitive

                // Fill in all column metadata
                tableGroups.ToList().ForEach(grouping => {
                    columnMetadata[GenerateColumnMetadataKey(databaseName, grouping.Key)] = grouping
                        .Select(row => new ColumnMetadata
                            {
                                ClusterName = this.ClusterName,
                                DatabaseName = databaseName,
                                TableName = grouping.Key,
                                MetadataType = DataSourceMetadataType.Column,
                                MetadataTypeName = DataSourceMetadataType.Column.ToString(),
                                Name = row.Name, // column name
                                PrettyName = row.Name,
                                Urn = $"{this.ClusterName}.{databaseName}.{grouping.Key}.{row.Name}",
                                DataType = row.DataType
                            })
                        .OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase); // case-insensitive
                });
            }
        }

        private string GenerateColumnMetadataKey(string databaseName, string tableName)
        {
            return $"{databaseName}.{tableName}";
        }
        #endregion
    }
}
