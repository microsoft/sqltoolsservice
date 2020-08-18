using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Completion;
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
        /// Updates database and affected variables like GlobalState for given object.
        /// </summary>
        /// <param name="updateDatabase">Object metadata.</param>
        void UpdateDatabase(string databaseName);

        /// <summary>
        /// Gets autocomplete suggestions at given position.
        /// </summary>
        /// <param name="GetAutoCompleteSuggestions">Object metadata.</param>
        CompletionItem[] GetAutoCompleteSuggestions(ScriptDocumentInfo queryText, Position index, bool throwOnError = false);
        /// <summary>
        /// Gets quick info hover tooltips for the current position.
        /// </summary>
        /// <param name="GetHoverHelp">Object metadata.</param>
        Hover GetHoverHelp(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false);

        /// <summary>
        /// Gets definition for a selected query text.
        /// </summary>
        /// <param name="GetDefinition">Object metadata.</param>
        DefinitionResult GetDefinition(string queryText, int index, int startLine, int startColumn, bool throwOnError = false);
        
        /// <summary>
        /// Gets a list of semantic diagnostic marks for the provided script file
        /// </summary>
        /// <param name="GetSemanticMarkers">Object metadata.</param>
        ScriptFileMarker[] GetSemanticMarkers(ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText);

        /// <summary>
        /// Tells whether the data source exists.
        /// </summary>
        /// <returns>true if it exists; false otherwise.</returns>
        Task<bool> Exists();

        /// <summary>
        /// Gets FunctionInfo object for a function
        /// </summary>
        /// <param name="functionName"></param>
        /// <returns></returns>
        string GenerateAlterFunctionScript(string functionName);
    }
}