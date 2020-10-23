// <copyright file="DataSourceUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Threading.Tasks;
using Kusto.Language;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    /// <inheritdoc cref="IDataSource"/>
    public abstract class DataSourceBase : IDataSource
    {
        #region IDisposable

        /// <summary>
        /// Finalizes an instance of the <see cref="DataSourceBase"/> class.
        /// </summary>
        ~DataSourceBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        /// <param name="disposing">true if disposing; false if finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        #endregion

        #region IDataSource

        /// <inheritdoc/>
        public abstract Task<IDataReader> ExecuteQueryAsync(string query, CancellationToken cancellationToken, string databaseName = null);

        /// <inheritdoc/>
        public async Task<T> ExecuteScalarQueryAsync<T>(string query, CancellationToken cancellationToken, string databaseName = null)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(query, nameof(query));

            using (var records = await ExecuteQueryAsync(query, cancellationToken, databaseName))
            {
                return records.ToScalar<T>();
            }
        }

        public abstract Task<IEnumerable<T>> ExecuteControlCommandAsync<T>(string command, bool throwOnError, CancellationToken cancellationToken);

        /// <inheritdoc/>
        public abstract DiagnosticsInfo GetDiagnostics(DataSourceObjectMetadata parentMetadata);

        /// <inheritdoc/>
        public abstract IEnumerable<DataSourceObjectMetadata> GetChildObjects(DataSourceObjectMetadata parentMetadata, bool includeSizeDetails = false);

        /// <param name="includeDatabase"></param>
        /// <inheritdoc/>
        public abstract void Refresh(bool includeDatabase);

        /// <inheritdoc/>
        public abstract void Refresh(DataSourceObjectMetadata objectMetadata);

        /// <inheritdoc/>
        public abstract void UpdateDatabase(string databaseName, RetryPolicy commandRetryPolicy);

        /// <inheritdoc/>
        public abstract Task<bool> Exists();

        /// <inheritdoc/>
        public abstract bool Exists(DataSourceObjectMetadata objectMetadata);

        public abstract string GenerateAlterFunctionScript(string functionName);

        public abstract string GenerateExecuteFunctionScript(string functionName);

        /// <inheritdoc/>
        public DataSourceType DataSourceType { get; protected set; }
        
        /// <inheritdoc/>
        public abstract string ClusterName { get; }

        public abstract string DatabaseName { get; }
        public abstract GlobalState SchemaState { get; }

        #endregion
    }
}