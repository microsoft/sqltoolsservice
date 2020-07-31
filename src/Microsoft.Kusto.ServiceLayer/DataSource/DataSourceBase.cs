// <copyright file="DataSourceUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    /// <inheritdoc cref="IDataSource"/>
    public abstract class DataSourceBase : IDataSource
    {
        protected Object dataSourceLock = new Object();

        private string _database;

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

        /// <inheritdoc/>
        public abstract IEnumerable<DataSourceObjectMetadata> GetChildObjects(DataSourceObjectMetadata parentMetadata);

        /// <inheritdoc/>
        public abstract void Refresh();

        /// <inheritdoc/>
        public abstract void Refresh(DataSourceObjectMetadata objectMetadata);

        /// <inheritdoc/>
        public abstract Task<bool> Exists();

        /// <inheritdoc/>
        public abstract bool Exists(DataSourceObjectMetadata objectMetadata);

        /// <inheritdoc/>
        public DataSourceType DataSourceType { get; protected set; }

        /// <inheritdoc/>
        public string ClusterName { get; protected set; }

        /// <inheritdoc/>
        public string DatabaseName { 
            get
            {
                return _database;
            }
            
            set
            {
                lock(dataSourceLock)
                {
                    _database = value;
                }
            }
        }

        #endregion
    }
}