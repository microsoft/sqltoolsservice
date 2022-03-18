﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.Admin.Contracts;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource.Intellisense;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;

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
        string DatabaseName { get; }

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
        /// Executes a Kusto query that returns a scalar value.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>The result.</returns>
        Task<IEnumerable<T>> ExecuteControlCommandAsync<T>(string command, bool throwOnError, CancellationToken cancellationToken);

        /// <summary>
        /// Get children of the  given parent
        /// </summary>
        /// <param name="parentMetadata">Parent object metadata.</param>
        /// <returns>Metadata for all children.</returns>
        DiagnosticsInfo GetDiagnostics(DataSourceObjectMetadata parentMetadata);

        /// <summary>
        /// Get children of the  given parent
        /// </summary>
        /// <param name="parentMetadata">Parent object metadata.</param>
        /// <param name="includeSizeDetails"></param>
        /// <returns>Metadata for all children.</returns>
        IEnumerable<DataSourceObjectMetadata> GetChildObjects(DataSourceObjectMetadata parentMetadata, bool includeSizeDetails = false);

        /// <summary>
        /// Refresh object list for entire cluster.
        /// </summary>
        /// <param name="includeDatabase"></param>
        void Refresh(bool includeDatabase);

        /// <summary>
        /// Refresh object list for given object.
        /// </summary>
        /// <param name="objectMetadata">Object metadata.</param>
        void Refresh(DataSourceObjectMetadata objectMetadata);

        /// <summary>
        /// Updates database and affected variables like GlobalState for given object.
        /// </summary>
        /// <param name="updateDatabase">Object metadata.</param>
        void UpdateDatabase(string databaseName);

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

        /// <summary>
        /// Generates an alter script for a function
        /// </summary>
        /// <param name="functionName"></param>
        /// <returns></returns>
        string GenerateAlterFunctionScript(string functionName);

        /// <summary>
        /// Generates an execute script for a function
        /// </summary>
        /// <param name="functionName"></param>
        /// <returns></returns>
        string GenerateExecuteFunctionScript(string functionName);
        
        ScriptFileMarker[] GetSemanticMarkers(ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText);
        DefinitionResult GetDefinition(string queryText, int index, int startLine, int startColumn, bool throwOnError = false);
        Hover GetHoverHelp(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false);
        CompletionItem[] GetAutoCompleteSuggestions(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false);
        ListDatabasesResponse GetDatabases(string serverName, bool includeDetails);
        DatabaseInfo GetDatabaseInfo(string serverName, string databaseName);
    }
}