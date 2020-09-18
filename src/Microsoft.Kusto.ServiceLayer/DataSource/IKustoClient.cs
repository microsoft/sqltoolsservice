using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Language;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public interface IKustoClient
    {
        /// <summary>
        /// SchemaState used for getting intellisense info.
        /// </summary>
        GlobalState SchemaState { get; }

        string ClusterName { get; }
        string DatabaseName { get; }
        bool RefreshToken(string azureAccountToken);
        IDataReader ExecuteQuery(string query, CancellationToken cancellationToken, string databaseName = null);

        /// <summary>
        /// Executes a query or command against a kusto cluster and returns a sequence of result row instances.
        /// </summary>
        Task<IEnumerable<T>> ExecuteControlCommandAsync<T>(string command, bool throwOnError, CancellationToken cancellationToken);

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The results.</returns>
        Task<IDataReader> ExecuteQueryAsync(string query, CancellationToken cancellationToken, string databaseName = null);

        /// <summary>
        /// Executes a Kusto control command.
        /// </summary>
        /// <param name="command">The command.</param>
        void ExecuteControlCommand(string command);

        void UpdateDatabase(string databaseName);
        void Dispose();
    }
}