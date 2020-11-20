using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
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
using Microsoft.Kusto.ServiceLayer.DataSource.Contracts;
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

        public KustoClient(KustoConnectionDetails connectionDetails, string ownerUri)
        {
            _ownerUri = ownerUri;
            Initialize(connectionDetails);
            SchemaState = LoadSchemaState();
        }

        private string ParseDatabaseName(string databaseName)
        {
            var regex = new Regex(@"(?<=\().+?(?=\))");
            
            return regex.IsMatch(databaseName)
                ? regex.Match(databaseName).Value
                : databaseName;
        }

        private GlobalState LoadSchemaState()
        {
            IEnumerable<ShowDatabaseSchemaResult> tableSchemas = Enumerable.Empty<ShowDatabaseSchemaResult>();
            IEnumerable<ShowFunctionsResult> functionSchemas = Enumerable.Empty<ShowFunctionsResult>();

            if (!string.IsNullOrWhiteSpace(DatabaseName))
            {
                var source = new CancellationTokenSource();
                Parallel.Invoke(() =>
                    {
                        tableSchemas =
                            ExecuteQueryAsync<ShowDatabaseSchemaResult>($".show database {DatabaseName} schema", source.Token, DatabaseName)
                                .Result;
                    },
                    () =>
                    {
                        functionSchemas = ExecuteQueryAsync<ShowFunctionsResult>(".show functions", source.Token, DatabaseName).Result;
                    });
            }

            return KustoIntellisenseHelper.AddOrUpdateDatabase(tableSchemas, functionSchemas,
                GlobalState.Default,
                DatabaseName, ClusterName);
        }

        private void Initialize(KustoConnectionDetails connectionDetails)
        {
            var stringBuilder = GetKustoConnectionStringBuilder(connectionDetails);
            _kustoQueryProvider = KustoClientFactory.CreateCslQueryProvider(stringBuilder);
            _kustoAdminProvider = KustoClientFactory.CreateCslAdminProvider(stringBuilder);
        }

        private void RefreshAzureToken()
        {
            string azureAccountToken = ConnectionService.Instance.RefreshAzureToken(_ownerUri);
            _kustoQueryProvider.Dispose();
            _kustoAdminProvider.Dispose();
            
            var connectionDetails = new KustoConnectionDetails
            {
                ServerName = ClusterName,
                DatabaseName = DatabaseName,
                UserToken = azureAccountToken
            };
            
            Initialize(connectionDetails);
        }

        private KustoConnectionStringBuilder GetKustoConnectionStringBuilder(KustoConnectionDetails connectionDetails)
        {
            ValidationUtils.IsTrue<ArgumentException>(!string.IsNullOrWhiteSpace(connectionDetails.UserToken),
                $"the Kusto authentication is not specified - either set {nameof(connectionDetails.UserToken)}");
            
            var stringBuilder = string.IsNullOrWhiteSpace(connectionDetails.ConnectionString)
                ? new KustoConnectionStringBuilder(connectionDetails.ServerName, connectionDetails.DatabaseName)
                : new KustoConnectionStringBuilder(connectionDetails.ConnectionString);
            
            ClusterName = stringBuilder.DataSource;
            var databaseName = ParseDatabaseName(stringBuilder.InitialCatalog);
            DatabaseName = databaseName;
            stringBuilder.InitialCatalog = databaseName;
            
            ValidationUtils.IsNotNull(ClusterName, nameof(ClusterName));
            
            return connectionDetails.AuthenticationType == "dstsAuth" 
                ? stringBuilder.WithDstsUserTokenAuthentication(connectionDetails.UserToken) 
                : stringBuilder.WithAadUserTokenAuthentication(connectionDetails.UserToken);
        }

        private ClientRequestProperties GetClientRequestProperties(CancellationToken cancellationToken)
        {
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

                    if (!string.IsNullOrEmpty(minimalQuery))
                    {
                        // Query is empty in case of comments
                        IDataReader origReader;
                        var clientRequestProperties = GetClientRequestProperties(cancellationToken);

                        if (minimalQuery.StartsWith(".") && !minimalQuery.StartsWith(".show"))
                        {
                            origReader = _kustoAdminProvider.ExecuteControlCommand(
                                KustoQueryUtils.IsClusterLevelQuery(minimalQuery) ? "" : databaseName,
                                minimalQuery,
                                clientRequestProperties);
                        }
                        else
                        {
                            origReader = _kustoQueryProvider.ExecuteQuery(
                                KustoQueryUtils.IsClusterLevelQuery(minimalQuery) ? "" : databaseName,
                                minimalQuery,
                                clientRequestProperties);
                        }

                        origReaders[index] = origReader;
                        numOfQueries++;
                    }
                });

                if (numOfQueries == 0 && origReaders.Length > 0) // Covers the scenario when user tries to run comments.
                {
                    var clientRequestProperties = GetClientRequestProperties(cancellationToken);
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
        public async Task ExecuteControlCommandAsync(string command, bool throwOnError, int retryCount = 1)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(command, nameof(command));

            try
            {
                using (var adminOutput = await _kustoAdminProvider.ExecuteControlCommandAsync(DatabaseName, command))
                {
                }
            }
            catch (KustoRequestException exception) when (retryCount > 0 && exception.FailureCode == 401) // Unauthorized
            {
                RefreshAzureToken();
                retryCount--;
                await ExecuteControlCommandAsync(command, throwOnError, retryCount);
            }
            catch (Exception) when (!throwOnError)
            {
            }
        }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The results.</returns>
        public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(string query, CancellationToken cancellationToken, string databaseName = null)
        {
            try
            {
                var resultReader = ExecuteQuery(query, cancellationToken, databaseName);
                var results = KustoDataReaderParser.ParseV1(resultReader, null);
                var tableReader = results[WellKnownDataSet.PrimaryResult].Single().TableData.CreateDataReader();
                return await Task.FromResult(new ObjectReader<T>(tableReader));
            }
            catch (Exception)
            {
                return null;
            }
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
                using (var adminOutput = _kustoAdminProvider.ExecuteControlCommand(command))
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
            DatabaseName = ParseDatabaseName(databaseName);
            SchemaState = LoadSchemaState();
        }

        public void Dispose()
        {
            _kustoQueryProvider.Dispose();
            _kustoAdminProvider.Dispose();
        }
    }
}