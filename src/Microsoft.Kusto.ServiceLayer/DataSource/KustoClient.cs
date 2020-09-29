using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Data;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Data;
using Kusto.Data.Exceptions;
using Kusto.Data.Net.Client;
using Kusto.Language;
using Kusto.Language.Editor;
using Microsoft.Data.SqlClient;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense;
using Microsoft.Kusto.ServiceLayer.DataSource.Exceptions;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public class KustoClient : IKustoClient
    {
        private readonly string _ownerUri;

        private int _retryCount;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ICslAdminProvider _kustoAdminProvider;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ICslQueryProvider _kustoQueryProvider;

        /// <summary>
        /// SchemaState used for getting intellisense info.
        /// </summary>
        public GlobalState SchemaState { get; private set; }

        public string ClusterName { get; }
        public string DatabaseName { get; private set; }

        public KustoClient(string connectionString, string azureAccountToken, string ownerUri)
        {
            _ownerUri = ownerUri;
            _retryCount = 1;
            ClusterName = GetClusterName(connectionString);
            var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
            Initialize(ClusterName, databaseName, azureAccountToken);
            DatabaseName = string.IsNullOrWhiteSpace(databaseName) ? GetFirstDatabaseName() : databaseName;
            SchemaState = LoadSchemaState();
        }

        private GlobalState LoadSchemaState()
        {
            CancellationTokenSource source = new CancellationTokenSource();

            IEnumerable<ShowDatabaseSchemaResult> tableSchemas = Enumerable.Empty<ShowDatabaseSchemaResult>();
            IEnumerable<ShowFunctionsResult> functionSchemas = Enumerable.Empty<ShowFunctionsResult>();

            Parallel.Invoke(() =>
            {
                tableSchemas = ExecuteControlCommandAsync<ShowDatabaseSchemaResult>(
                    $".show database {DatabaseName} schema",
                    false, source.Token).Result;
            }, () =>
            {
                functionSchemas = ExecuteControlCommandAsync<ShowFunctionsResult>(".show functions", false,
                    source.Token).Result;
            });

            return KustoIntellisenseHelper.AddOrUpdateDatabase(tableSchemas, functionSchemas,
                GlobalState.Default,
                DatabaseName, ClusterName);
        }

        private void Initialize(string clusterName, string databaseName, string azureAccountToken)
        {
            var stringBuilder = GetKustoConnectionStringBuilder(clusterName, databaseName, azureAccountToken, "", "");
            _kustoQueryProvider = KustoClientFactory.CreateCslQueryProvider(stringBuilder);
            _kustoAdminProvider = KustoClientFactory.CreateCslAdminProvider(stringBuilder);
        }

        private void RefreshAzureToken()
        {
            string azureAccountToken = ConnectionService.Instance.RefreshAzureToken(_ownerUri);
            _kustoQueryProvider.Dispose();
            _kustoAdminProvider.Dispose();
            Initialize(ClusterName, DatabaseName, azureAccountToken);
        }

        /// <summary>
        /// Extracts the cluster name from the connectionstring. The string looks like the following:
        /// "Data Source=clustername.kusto.windows.net;User ID=;Password=;Pooling=False;Application Name=azdata-GeneralConnection"
        /// <summary>
        /// <param name="connectionString">A connection string coming over the Data management protocol</param>
        private string GetClusterName(string connectionString)
        {
            var csb = new SqlConnectionStringBuilder(connectionString);

            // If there is no https:// prefix, add it
            Uri uri;
            if ((Uri.TryCreate(csb.DataSource, UriKind.Absolute, out uri) ||
                 Uri.TryCreate("https://" + csb.DataSource, UriKind.Absolute, out uri)) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return uri.AbsoluteUri;
            }

            throw new ArgumentException("Expected a URL of the form clustername.kusto.windows.net");
        }

        private KustoConnectionStringBuilder GetKustoConnectionStringBuilder(string clusterName, string databaseName,
            string userToken, string applicationClientId, string applicationKey)
        {
            ValidationUtils.IsNotNull(clusterName, nameof(clusterName));
            ValidationUtils.IsTrue<ArgumentException>(
                !string.IsNullOrWhiteSpace(userToken)
                || (!string.IsNullOrWhiteSpace(applicationClientId) && !string.IsNullOrWhiteSpace(applicationKey)),
                $"the Kusto authentication is not specified - either set {nameof(userToken)}, or set {nameof(applicationClientId)} and {nameof(applicationKey)}");

            var kcsb = new KustoConnectionStringBuilder
            {
                DataSource = clusterName,

                // Perform federated auth based on the AAD user token, or based on the AAD application client id and key.
                FederatedSecurity = true
            };

            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                kcsb.InitialCatalog = databaseName;
            }

            if (!string.IsNullOrWhiteSpace(userToken))
            {
                kcsb.UserToken = userToken;
            }

            if (!string.IsNullOrWhiteSpace(applicationClientId))
            {
                kcsb.ApplicationClientId = applicationClientId;
            }

            if (!string.IsNullOrWhiteSpace(applicationKey))
            {
                kcsb.ApplicationKey = applicationKey;
            }

            return kcsb;
        }

        /// <summary>
        /// Extracts the database name from the connectionString if it exists
        /// otherwise it takes the first database name from the server
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>Database Name</returns>
        private string GetFirstDatabaseName()
        {
            var source = new CancellationTokenSource();
            string query = ".show databases | project DatabaseName";

            using (var reader = ExecuteQuery(query, source.Token))
            {
                var rows = reader.ToEnumerable();
                var row = rows?.FirstOrDefault();
                return row?[0].ToString() ?? string.Empty;
            }
        }

        public IDataReader ExecuteQuery(string query, CancellationToken cancellationToken, string databaseName = null)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(query, nameof(query));

            var clientRequestProperties = new ClientRequestProperties
            {
                ClientRequestId = Guid.NewGuid().ToString()
            };
            clientRequestProperties.SetOption(ClientRequestProperties.OptionNoTruncation, true);
            cancellationToken.Register(() => CancelQuery(clientRequestProperties.ClientRequestId));

            var kustoCodeService = new KustoCodeService(query);
            var minimalQuery = kustoCodeService.GetMinimalText(MinimalTextKind.RemoveLeadingWhitespaceAndComments);

            try
            {
                IDataReader origReader = _kustoQueryProvider.ExecuteQuery(
                    KustoQueryUtils.IsClusterLevelQuery(minimalQuery) ? "" : databaseName,
                    minimalQuery,
                    clientRequestProperties);
                
                return new KustoResultsReader(origReader);
            }
            catch (KustoRequestException exception) when (exception.FailureCode == 401) // Unauthorized
            {
                if (_retryCount <= 0)
                {
                    throw;
                }

                _retryCount--;
                RefreshAzureToken();
                return ExecuteQuery(query, cancellationToken, databaseName);
            }
        }

        /// <summary>
        /// Executes a query or command against a kusto cluster and returns a sequence of result row instances.
        /// </summary>
        public async Task<IEnumerable<T>> ExecuteControlCommandAsync<T>(string command, bool throwOnError,
            CancellationToken cancellationToken)
        {
            try
            {
                var resultReader = await ExecuteQueryAsync(command, cancellationToken, DatabaseName);
                var results = KustoDataReaderParser.ParseV1(resultReader, null);
                var tableReader = results[WellKnownDataSet.PrimaryResult].Single().TableData.CreateDataReader();
                return new ObjectReader<T>(tableReader);
            }
            catch (DataSourceUnauthorizedException)
            {
                throw;
            }
            catch (Exception) when (!throwOnError)
            {
                return null;
            }
        }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The results.</returns>
        public Task<IDataReader> ExecuteQueryAsync(string query, CancellationToken cancellationToken,
            string databaseName = null)
        {
            var reader = ExecuteQuery(query, cancellationToken, databaseName);
            return Task.FromResult(reader);
        }

        private void CancelQuery(string clientRequestId)
        {
            var query = $".cancel query \"{clientRequestId}\"";
            ExecuteControlCommand(query);
        }

        /// <summary>
        /// Executes a Kusto control command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void ExecuteControlCommand(string command)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(command, nameof(command));

            using (var adminOutput = _kustoAdminProvider.ExecuteControlCommand(command, null))
            {
            }
        }

        public void UpdateDatabase(string databaseName)
        {
            DatabaseName = databaseName;
            SchemaState = LoadSchemaState();
        }

        public void Dispose()
        {
            _kustoQueryProvider.Dispose();
            _kustoAdminProvider.Dispose();
        }
    }
}