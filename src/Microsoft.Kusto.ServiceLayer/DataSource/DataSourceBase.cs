// <copyright file="DataSourceUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
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
    /// Metadata type enumeration
    /// </summary>
    public enum DataSourceMetadataType
    {
        Table = 0,
        View = 1,
        SProc = 2,
        Function = 3,
        Schema = 4,
        Database = 5
    }

    /// <summary>
    /// Object metadata information
    /// </summary>
    public class DataSourceObjectMetadata 
    {
        public DataSourceMetadataType MetadataType { get; set; }
    
        public string MetadataTypeName { get; set; }

        public string Name { get; set; }

        public string PrettyName { get; set; }
        
        public string Urn { get; set; }
    }

    /// <summary>
    /// Database metadata information
    /// </summary>
    public class DatabaseMetadata : DataSourceObjectMetadata
    {
        public string ClusterName { get; set; }
    }

    /// <summary>
    /// Database metadata information
    /// </summary>
    public class TableMetadata : DatabaseMetadata
    {
        public string DatabaseName { get; set; }
    }

    /// <summary>
    /// Column metadata information
    /// </summary>
    public class ColumnMetadata : TableMetadata
    {
        public string TableName { get; set; }
        public string DataType { get; set; }
    }
    
    /// <summary>
    /// Represents data source utilities.
    /// </summary>
    public interface IDataSource : IDisposable
    {
        /// <summary>
        /// The data source type.
        /// </summary>
        DataSourceType DataSourceType { get; }

        /// <summary>
        /// The cluster/server name.
        /// </summary>
        string Cluster { get; }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The results.</returns>
        Task<IDataReader> ExecuteQueryAsync(string query, string database);

        /// <summary>
        /// Executes a Kusto query that returns a scalar value.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>The result.</returns>
        Task<T> ExecuteScalarQueryAsync<T>(string query, string database);

        /// <summary>
        /// Get children of the  given parent
        /// </summary>
        /// <param name="parentMetadata">Parent object metadata.</param>
        /// <returns>Metadata for all children.</returns>
        Task<IEnumerable<DataSourceObjectMetadata>> GetChildObjects(DataSourceObjectMetadata parentMetadata);

        /// <summary>
        /// Get folders of the  given parent
        /// </summary>
        /// <param name="parentMetadata">Parent object metadata.</param>
        /// <returns>List of all children.</returns>
        Task<IEnumerable<DataSourceObjectMetadata>> GetChildFolders(DataSourceObjectMetadata parentMetadata);

        /// <summary>
        /// Refresh object list for entire cluster.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Refresh object list for given object.
        /// </summary>
        /// <param name="objectMetadata">Object metadata.</param>
        void Refresh(DataSourceObjectMetadata objectMetadata);

        /// <summary>
        /// Tells whether the data source exists.
        /// </summary>
        /// <returns>true if it exists; false otherwise.</returns>
        Task<bool> Exists();

        /// <summary>
        /// Tells whether the object exists.
        /// </summary>
        /// <returns>true if it exists; false otherwise.</returns>
        Task<bool> Exists(DataSourceObjectMetadata objectMetadata);
    }

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
        public abstract Task<IDataReader> ExecuteQueryAsync(string query, string databaseName);

        /// <inheritdoc/>
        public async Task<T> ExecuteScalarQueryAsync<T>(string query, string databaseName)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(query, nameof(query));

            using (var records = await ExecuteQueryAsync(query))
            {
                return records.ToScalar<T>();
            }
        }

        /// <inheritdoc/>
        public abstract Task<bool> Exists();

        /// <inheritdoc/>
        public DataSourceType DataSourceType { get; private set; }

        /// <inheritdoc/>
        public string Cluster { get; private set; }

        #endregion
    }

    /// <summary>
    /// Data source factory.
    /// </summary>
    public static class DataSourceFactory
    {
        public static async Task<IDataSource> Create(DataSourceType dataSourceType, string connectionString, string azureAccountToken)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(cluster, nameof(cluster));
            ValidationUtils.IsArgumentNotNull(azureAccountToken, nameof(azureAccountToken));

            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                    {
                        return new KustoDataSource(connectionString, azureAccountToken);
                    }

                default:
                    throw new ArgumentException($"Unsupported data source type \"{dataSourceType}\"", nameof(dataSourceType));
            }
        }
    }
}