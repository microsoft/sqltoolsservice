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
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public class KustoClient : IKustoClient
    {
        private readonly string _ownerUri;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ICslAdminProvider _kustoAdminProvider;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ICslQueryProvider _kustoQueryProvider;

        /// <summary>
        /// SchemaState used for getting intellisense info.
        /// </summary>
        public GlobalState SchemaState { get; private set; }

        public string ClusterName { get; private set; }
        public string DatabaseName { get; private set; }

        public KustoClient(string connectionString, string azureAccountToken, string ownerUri)
        {
            _ownerUri = ownerUri;
            Initialize(connectionString, azureAccountToken);
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

        private void Initialize(string connectionString, string azureAccountToken)
        {
            var stringBuilder = GetKustoConnectionStringBuilder(azureAccountToken, connectionString);
            _kustoQueryProvider = KustoClientFactory.CreateCslQueryProvider(stringBuilder);
            _kustoAdminProvider = KustoClientFactory.CreateCslAdminProvider(stringBuilder);
        }

        private void RefreshAzureToken()
        {
            string azureAccountToken = ConnectionService.Instance.RefreshAzureToken(_ownerUri);
            _kustoQueryProvider.Dispose();
            _kustoAdminProvider.Dispose();
            Initialize("", azureAccountToken);
        }

        private KustoConnectionStringBuilder GetKustoConnectionStringBuilder(string userToken, string connectionString)
        {
            ValidationUtils.IsTrue<ArgumentException>(!string.IsNullOrWhiteSpace(userToken),
                $"the Kusto authentication is not specified - either set {nameof(userToken)}");
            
            var stringBuilder = string.IsNullOrWhiteSpace(connectionString)
                ? new KustoConnectionStringBuilder(connectionString)
                : new KustoConnectionStringBuilder(ClusterName, DatabaseName);
            ClusterName = stringBuilder.DataSource;
            DatabaseName = stringBuilder.InitialCatalog;
            
            ValidationUtils.IsNotNull(ClusterName, nameof(ClusterName));
            
            return stringBuilder.WithAadUserTokenAuthentication(userToken);
        }

        private ClientRequestProperties GetCLientRequestProperties(CancellationToken cancellationToken){
            var clientRequestProperties = new ClientRequestProperties
            {
                ClientRequestId = Guid.NewGuid().ToString()
            };
            clientRequestProperties.SetOption(ClientRequestProperties.OptionNoTruncation, true);
            cancellationToken.Register(() => CancelQuery(clientRequestProperties.ClientRequestId));

            return clientRequestProperties;
        }

        public IDataReader ExecuteQuery(string query, CancellationToken cancellationToken, string databaseName = null, int retryCount = 1)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(query, nameof(query));

            var script = CodeScript.From(query, GlobalState.Default);
            IDataReader[] origReaders = new IDataReader[script.Blocks.Count];
            try
            {
                var numOfQueries = 0;
                Parallel.ForEach(script.Blocks, (codeBlock, state, index) =>
                {
                    var minimalQuery =
                        codeBlock.Service.GetMinimalText(MinimalTextKind.RemoveLeadingWhitespaceAndComments);
                    
                    if(!string.IsNullOrEmpty(minimalQuery)){        // Query is empty in case of comments
                        IDataReader origReader;
                        var clientRequestProperties = GetCLientRequestProperties(cancellationToken);

                        if(minimalQuery.StartsWith(".") && !minimalQuery.StartsWith(".show")){
                            origReader = _kustoAdminProvider.ExecuteControlCommand(
                                KustoQueryUtils.IsClusterLevelQuery(minimalQuery) ? "" : databaseName,
                                minimalQuery,
                                clientRequestProperties);
                        }
                        else{
                            origReader = _kustoQueryProvider.ExecuteQuery(
                                KustoQueryUtils.IsClusterLevelQuery(minimalQuery) ? "" : databaseName,
                                minimalQuery,
                                clientRequestProperties);
                        }

                        origReaders[index] = origReader;
                        numOfQueries++;
                    }
                });

                if (numOfQueries == 0 && origReaders.Length > 0)                  // Covers the scenario when user tries to run comments.
                {
                    var clientRequestProperties = GetCLientRequestProperties(cancellationToken);
                    origReaders[0] = _kustoQueryProvider.ExecuteQuery(
                                KustoQueryUtils.IsClusterLevelQuery(query) ? "" : databaseName,
                                query,
                                clientRequestProperties);
                }
                
                return new KustoResultsReader(origReaders);
            }
            catch (AggregateException exception) 
                when (retryCount > 0 &&
                      exception.InnerException is KustoRequestException innerException 
                      && innerException.FailureCode == 401) // Unauthorized
            {
                RefreshAzureToken();
                retryCount--;
                return ExecuteQuery(query, cancellationToken, databaseName, retryCount);
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
        /// <param name="retryCount"></param>
        public void ExecuteControlCommand(string command, int retryCount = 1)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(command, nameof(command));

            try
            {
                using (var adminOutput = _kustoAdminProvider.ExecuteControlCommand(command, null))
                {
                }
            }
            catch (KustoRequestException exception) when (retryCount > 0 && exception.FailureCode == 401) // Unauthorized
            {
                RefreshAzureToken();
                retryCount--;
                ExecuteControlCommand(command, retryCount);
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