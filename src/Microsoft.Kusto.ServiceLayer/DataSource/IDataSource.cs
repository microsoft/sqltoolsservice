using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
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
        string ClusterName { get; }

        /// <summary>
        /// The current database name, if there is one.
        /// </summary>
        string DatabaseName { get; set; }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The results.</returns>
        Task<IDataReader> ExecuteQueryAsync(string query, CancellationToken cancellationToken, string databaseName = null);

        /// <summary>
        /// Executes a Kusto query that returns a scalar value.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>The result.</returns>
        Task<T> ExecuteScalarQueryAsync<T>(string query, CancellationToken cancellationToken, string databaseName = null);

        /// <summary>
        /// Get children of the  given parent
        /// </summary>
        /// <param name="parentMetadata">Parent object metadata.</param>
        /// <returns>Metadata for all children.</returns>
        IEnumerable<DataSourceObjectMetadata> GetChildObjects(DataSourceObjectMetadata parentMetadata);

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
        bool Exists(DataSourceObjectMetadata objectMetadata);
    }
}