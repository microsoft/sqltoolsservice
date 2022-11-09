//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.Admin.Contracts;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource.Intellisense;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;

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
        public async Task<T> ExecuteScalarQueryAsync<T>(string query, CancellationToken cancellationToken, string? databaseName = null)
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
        public abstract void UpdateDatabase(string databaseName);

        /// <inheritdoc/>
        public abstract Task<bool> Exists();

        /// <inheritdoc/>
        public abstract bool Exists(DataSourceObjectMetadata objectMetadata);

        public abstract string GenerateAlterFunctionScript(string functionName);

        public abstract string GenerateExecuteFunctionScript(string functionName);
        public abstract ScriptFileMarker[] GetSemanticMarkers(ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText);

        public abstract DefinitionResult GetDefinition(string queryText, int index, int startLine, int startColumn, bool throwOnError = false);

        public abstract Hover GetHoverHelp(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false);

        public abstract CompletionItem[] GetAutoCompleteSuggestions(ScriptDocumentInfo scriptDocumentInfo, Position textPosition,
            bool throwOnError = false);

        public abstract ListDatabasesResponse GetDatabases(string serverName, bool includeDetails);
        public abstract DatabaseInfo GetDatabaseInfo(string serverName, string databaseName);

        /// <inheritdoc/>
        public DataSourceType DataSourceType { get; protected set; }
        
        /// <inheritdoc/>
        public abstract string ClusterName { get; }

        public abstract string DatabaseName { get; set; }

        #endregion
    }
}