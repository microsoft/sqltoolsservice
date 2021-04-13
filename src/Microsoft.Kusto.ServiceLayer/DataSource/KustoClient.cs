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
        
        public string ClusterName { get; private set; }
        public string DatabaseName { get; private set; }

        public KustoClient(DataSourceConnectionDetails connectionDetails, string ownerUri)
        {
            _ownerUri = ownerUri;
            Initialize(connectionDetails);
        }

        private string ParseDatabaseName(string databaseName)
        {
            var regex = new Regex(@"(?<=\().+?(?=\))");
            
            return regex.IsMatch(databaseName)
                ? regex.Match(databaseName).Value
                : databaseName;
        }

        private void Initialize(DataSourceConnectionDetails connectionDetails)
        {
            var stringBuilder = GetKustoConnectionStringBuilder(connectionDetails);
            _kustoQueryProvider = KustoClientFactory.CreateCslQueryProvider(stringBuilder);
            _kustoAdminProvider = KustoClientFactory.CreateCslAdminProvider(stringBuilder);

            if (DatabaseName != "NetDefaultDB")
            {
                return;
            }
            
            var dataReader = ExecuteQuery(".show databases schema | order by DatabaseName asc | take 1", new CancellationToken());
            var databaseName = dataReader.ToEnumerable().Select(row => row["DatabaseName"]).FirstOrDefault();
            DatabaseName = databaseName?.ToString() ?? "";
        }

        private void RefreshAuthToken()
        {
            string accountToken = ConnectionService.Instance.RefreshAuthToken(_ownerUri);
            _kustoQueryProvider.Dispose();
            _kustoAdminProvider.Dispose();
            
            var connectionDetails = new DataSourceConnectionDetails
            {
                ServerName = ClusterName,
                DatabaseName = DatabaseName,
                UserToken = accountToken,
                AuthenticationType = "AzureMFA"
            };
            
            Initialize(connectionDetails);
        }

        private KustoConnectionStringBuilder GetKustoConnectionStringBuilder(DataSourceConnectionDetails connectionDetails)
        {
            var stringBuilder = string.IsNullOrWhiteSpace(connectionDetails.ConnectionString)
                ? new KustoConnectionStringBuilder(connectionDetails.ServerName, connectionDetails.DatabaseName)
                : new KustoConnectionStringBuilder(connectionDetails.ConnectionString);
            
            ClusterName = stringBuilder.DataSource;
            var databaseName = ParseDatabaseName(stringBuilder.InitialCatalog);
            DatabaseName = databaseName;
            stringBuilder.InitialCatalog = databaseName;
            
            ValidationUtils.IsNotNull(ClusterName, nameof(ClusterName));

            switch (connectionDetails.AuthenticationType)
            {
                case "AzureMFA": return stringBuilder.WithAadUserTokenAuthentication(connectionDetails.UserToken);
                case "dstsAuth": return stringBuilder.WithDstsUserTokenAuthentication(connectionDetails.UserToken);
                default:
                    return string.IsNullOrWhiteSpace(connectionDetails.UserName) && string.IsNullOrWhiteSpace(connectionDetails.Password)
                        ? stringBuilder
                        : stringBuilder.WithKustoBasicAuthentication(connectionDetails.UserName, connectionDetails.Password);
            }
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
                RefreshAuthToken();
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
                RefreshAuthToken();
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
        /// <param name="cancellationToken"></param>
        /// <param name="databaseName"></param>
        /// <returns>The results.</returns>
        public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(string query, CancellationToken cancellationToken,
            string databaseName = null)
        {
            var resultReader = ExecuteQuery(query, cancellationToken, databaseName);
            var results = KustoDataReaderParser.ParseV1(resultReader, null);
            var tableReader = results[WellKnownDataSet.PrimaryResult].Single().TableData.CreateDataReader();
            return await Task.FromResult(new ObjectReader<T>(tableReader));
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
                RefreshAuthToken();
                retryCount--;
                ExecuteControlCommand(command, retryCount);
            }
        }

        public void UpdateDatabase(string databaseName)
        {
            DatabaseName = ParseDatabaseName(databaseName);
        }

        public void Dispose()
        {
            _kustoQueryProvider.Dispose();
            _kustoAdminProvider.Dispose();
        }
    }
}