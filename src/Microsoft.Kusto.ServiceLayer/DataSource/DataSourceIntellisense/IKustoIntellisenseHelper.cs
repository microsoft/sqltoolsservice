using System.Threading.Tasks;
using Kusto.Language;
using Kusto.Language.Editor;
using Kusto.Language.Symbols;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Completion;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;

namespace Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense
{
    public interface IKustoIntellisenseHelper
    {
        /// <summary>
        /// Loads the schema for the specified databasea into a a <see cref="DatabaseSymbol"/>.
        /// </summary>
        Task<DatabaseSymbol> LoadDatabaseAsync(IDataSource dataSource, string databaseName, bool throwOnError = false);

        CompletionItemKind CreateCompletionItemKind(CompletionKind kustoKind);

        /// <summary>
        /// Gets default keyword when user if not connected to any Kusto cluster.
        /// </summary>
        LanguageServices.Contracts.CompletionItem[] GetDefaultKeywords(ScriptDocumentInfo scriptDocumentInfo, Position textDocumentPosition);

        /// <summary>
        /// Gets default diagnostics when user if not connected to any Kusto cluster.
        /// </summary>
        ScriptFileMarker[] GetDefaultDiagnostics(ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText);

        /// <summary>
        /// Loads the schema for the specified database and returns a new <see cref="GlobalState"/> with the database added or updated.
        /// </summary>
        Task<GlobalState> AddOrUpdateDatabaseAsync(IDataSource dataSource, GlobalState globals, string databaseName, string clusterName, bool throwOnError);
    }
}