using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.Hosting.Contracts.Language;
using Microsoft.SqlTools.Hosting.Contracts.Workspace;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Intellisense
{
    public interface IIntellisenseClient
    {
        void UpdateDatabase(string databaseName);
        ScriptFileMarker[] GetSemanticMarkers(ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText);
        DefinitionResult GetDefinition(string queryText, int index, int startLine, int startColumn, bool throwOnError = false);
        Hover GetHoverHelp(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false);
        CompletionItem[] GetAutoCompleteSuggestions(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false);
    }
}