// <copyright file="DataSourceUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Completion;

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

        public abstract Task<IEnumerable<T>> ExecuteControlCommandAsync<T>(string command, bool throwOnError, CancellationToken cancellationToken);

        /// <inheritdoc/>
        public abstract DiagnosticsInfo GetDiagnostics(DataSourceObjectMetadata parentMetadata);

        /// <inheritdoc/>
        public abstract IEnumerable<DataSourceObjectMetadata> GetChildObjects(DataSourceObjectMetadata parentMetadata, bool includeSizeDetails = false);

        /// <inheritdoc/>
        public abstract void Refresh();

        /// <inheritdoc/>
        public abstract void Refresh(DataSourceObjectMetadata objectMetadata);

        /// <inheritdoc/>
        public abstract void UpdateDatabase(string databaseName);

        /// <inheritdoc/>
        public abstract CompletionItem[] GetAutoCompleteSuggestions(ScriptDocumentInfo queryText, Position index, bool throwOnError = false);
        /// <inheritdoc/>
        public abstract Hover GetHoverHelp(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false);
        
        /// <inheritdoc/>
        public abstract DefinitionResult GetDefinition(string queryText, int index, int startLine, int startColumn, bool throwOnError = false);

        /// <inheritdoc/>
        public abstract ScriptFileMarker[] GetSemanticMarkers(ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText);

        /// <inheritdoc/>
        public abstract Task<bool> Exists();

        /// <inheritdoc/>
        public abstract bool Exists(DataSourceObjectMetadata objectMetadata);

        public abstract string GenerateAlterFunctionScript(string functionName);

        public abstract string GenerateExecuteFunctionScript(string functionName);

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