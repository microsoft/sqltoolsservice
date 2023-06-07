//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Kusto
{
    public interface IKustoClient
    {
        string ClusterName { get; }
        
        string DatabaseName { get; }
        
        IDataReader ExecuteQuery(string query, CancellationToken cancellationToken, string databaseName = null, int retryCount = 1);

        /// <summary>
        /// Executes a query or command against a kusto cluster and returns a sequence of result row instances.
        /// </summary>
        Task ExecuteControlCommandAsync(string command, bool throwOnError, int retryCount = 1);

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The results.</returns>
        Task<IEnumerable<T>> ExecuteQueryAsync<T>(string query, CancellationToken cancellationToken, string databaseName = null);

        /// <summary>
        /// Executes a Kusto control command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="retryCount"></param>
        void ExecuteControlCommand(string command, int retryCount = 1);

        void UpdateDatabase(string databaseName);
        
        void Dispose();
    }
}