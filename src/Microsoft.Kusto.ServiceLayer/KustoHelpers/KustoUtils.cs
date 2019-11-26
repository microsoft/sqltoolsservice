// <copyright file="KustoUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.Kusto.ServiceLayer.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Kusto.Data;
    using Kusto.Data.Common;
    using Kusto.Data.Net.Client;
    using Kusto.Ingest;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.Kusto.ServiceLayer.Metadata.Contracts;
    
    /// <summary>
    /// Represents Kusto utilities.
    /// </summary>
    public class KustoUtils : DataSourceUtils
    {
        private ICslQueryProvider kustoQueryProvider;

        private ICslAdminProvider kustoAdminProvider;

        private IEnumerable<DatabaseMetadata> databaseMetadata;

        /// <summary>
        /// Prevents a default instance of the <see cref="KustoUtils"/> class from being created.
        /// </summary>
        private KustoUtils(string connectionString, string azureAccountToken)
        {
            Cluster = GetClusterName(connectionString);
            DatabaseName = GetDatabaseName(connectionString);
            UserToken = azureAccountToken;

            // Check if a connection can be made
            Exists();
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

            return uri.InitialCatalog;
        }

        /// <summary>
        /// The Kusto cluster hostname, for example "lens.kusto.windows.net";
        /// </summary>
        public string Cluster { get; private set; }

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
        public override Task<IDataReader> ExecuteQueryAsync(string query)
        {
            var reader = ExecuteQuery(query);
            return Task.FromResult(reader);
        }

        private IDataReader ExecuteQuery(string query)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(query, nameof(query));

            var clientRequestProperties = new ClientRequestProperties
            {
                ClientRequestId = Guid.NewGuid().ToString()
            };
            clientRequestProperties.SetOption(ClientRequestProperties.OptionNoTruncation, true);

            return KustoQueryProvider.ExecuteQuery(DatabaseName, query, clientRequestProperties);
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
            ValidationUtils.IsNotNull(Cluster, nameof(Cluster));
            ValidationUtils.IsTrue<ArgumentException>(
                !string.IsNullOrWhiteSpace(UserToken)
                || (!string.IsNullOrWhiteSpace(ApplicationClientId) && !string.IsNullOrWhiteSpace(ApplicationKey)),
                $"the Kusto authentication is not specified - either set {nameof(UserToken)}, or set {nameof(ApplicationClientId)} and {nameof(ApplicationKey)}");

            var kcsb = new KustoConnectionStringBuilder
            {
                DataSource = Cluster,

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
        public override DataSourceType DataSourceTypeEnum => DataSourceType.Kusto;

        /// <inheritdoc/>
        public override Task<IEnumerable<ObjectMetadata>> GetDatabaseMetadata()
        {
            if (databaseMetadata == null)
            {
                // Getting database names when we are connected to a specific database should not happen.
                ValidationUtils.IsNotNull(DatabaseName, nameof(DatabaseName)); 

                var query = ".show databases" + (clusterName.IndexOf(KustoHelperQueries.AriaProxyURL, StringComparison.CurrentCultureIgnoreCase) == -1 ? " | project DatabaseName, PrettyName" : "");
                using (var reader = ExecuteQuery(query))
                {
                    var schemaTable = reader.GetSchemaTable();
                    var databaseNameProperty = schemaTable.Columns["DatabaseName"];
                    var prettyNameProperty = schemaTable.Columns["PrettyName"];

                    databaseMetadata = reader.ToEnumerable()
                        .Select(row => new ObjectMetadata
                        {
                            MetadataType = MetadataType.Database,
                            MetadataTypeName = MetadataType.ToString(),
                            Name = row[databaseNameProperty]?.ToString(),
                            PrettyName = row[prettyNameProperty]?.ToString(),
                            Urn = $"{Cluster}.{Name}"
                        });
                }
            }

            return Task.FromResult(syncSchema);
        }

        #endregion
    }
}
