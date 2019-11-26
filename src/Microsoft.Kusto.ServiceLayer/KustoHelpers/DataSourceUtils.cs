// <copyright file="DataSourceUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.Kusto.ServiceLayer.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using LEWeb.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents data source utilities.
    /// </summary>
    public interface IDataSourceUtils : IDisposable
    {
        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The results.</returns>
        Task<IDataReader> ExecuteQueryAsync(string query);

        /// <summary>
        /// Executes a Kusto query that returns a scalar value.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>The result.</returns>
        Task<T> ExecuteScalarQueryAsync<T>(string query);

        /// <summary>
        /// Tells whether the data source exists.
        /// </summary>
        /// <returns>true if it exists; false otherwise.</returns>
        Task<bool> Exists();
    }

    /// <summary>
    /// Represents the type of a data source.
    /// </summary>
    public enum DataSourceType
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        None,

        /// <summary>
        /// A Kusto cluster.
        /// </summary>
        Kusto,

        /// <summary>
        /// An Application Insights subscription.
        /// </summary>
        ApplicationInsights,

        /// <summary>
        /// An Operations Management Suite (OMS) Log Analytics workspace.
        /// </summary>
        OmsLogAnalytics
    }

    /// <summary>
    /// Base class for kusto metadata.
    /// </summary>
    public class KustoMetadata
    {
        /// <summary>
        /// The data source type.
        /// </summary>
        public string DataSourceType => DataSourceTypeEnum.ToString();

        /// <summary>
        /// The data source type enum.
        /// </summary>
        internal DataSourceType DataSourceTypeEnum { get; set; }
    }

    /// <summary>
    /// Represents database metadata.
    /// </summary>
    public class DatabaseMetadata : KustoMetadata
    {
        /// <summary>
        /// The cluster hostname.
        /// </summary>
        public string Cluster { get; set; }

        /// <summary>
        /// The database name.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// The table name.
        /// </summary>
        public string PrettyName { get; set; }
    }

    /// <summary>
    /// Source for schema metadata synchronization.  Follows a strategy pattern to get schema metadata from various metadata sources.
    /// </summary>
    public interface IDataSourceSchema
    {
        /// <summary>
        /// The data source type to sync.
        /// </summary>
        DataSourceType DataSourceType { get; }

        /// <summary>
        /// The cluster to sync.
        /// </summary>
        string Cluster { get; }

        /// <summary>
        /// Gets the databases.
        /// </summary>
        /// <returns>The databases.</returns>
        Task<IEnumerable<DatabaseMetadata>> GetDatabaseMetadata();
    }

    /// <inheritdoc cref="IDataSourceUtils"/>
    public abstract class DataSourceUtils : IDataSourceUtils, IDataSourceSchema
    {
        #region IDisposable

        /// <summary>
        /// Finalizes an instance of the <see cref="DataSourceUtils"/> class.
        /// </summary>
        ~DataSourceUtils()
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

        #region IDataSourceUtils

        /// <inheritdoc/>
        public abstract Task<IDataReader> ExecuteQueryAsync(string query);

        /// <inheritdoc/>
        public async Task<T> ExecuteScalarQueryAsync<T>(string query)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(query, nameof(query));

            using (var records = await ExecuteQueryAsync(query))
            {
                return records.ToScalar<T>();
            }
        }

        /// <inheritdoc/>
        public abstract Task<bool> Exists();

        #endregion

        #region IDataSourceSchema

        /// <inheritdoc/>
        public abstract DataSourceType DataSourceType { get; }

        /// <inheritdoc/>
        public abstract string Cluster { get; }
        
        #endregion
    }
}