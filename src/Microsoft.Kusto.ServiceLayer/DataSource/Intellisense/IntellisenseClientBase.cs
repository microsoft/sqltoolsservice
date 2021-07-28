using System;
using System.Collections.Generic;
using System.Linq;
using Kusto.Language;
using Kusto.Language.Editor;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using CompletionItem = Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts.CompletionItem;
using Diagnostic = Kusto.Language.Diagnostic;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Intellisense
{
    public abstract class IntellisenseClientBase
    {
        protected GlobalState schemaState;

        public abstract void UpdateDatabase(string databaseName);

        public ScriptFileMarker[] GetSemanticMarkers(ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText)
        {
            var kustoCodeService = new KustoCodeService(queryText, schemaState);
            var script = CodeScript.From(queryText, schemaState);
            var parseResult = new List<Diagnostic>();

            foreach (var codeBlock in script.Blocks)
            {
                parseResult.AddRange(codeBlock.Service.GetDiagnostics());
            }

            parseInfo.ParseResult = parseResult;

            if (!parseResult.Any())
            {
                return Array.Empty<ScriptFileMarker>();
            }

            // build a list of Kusto script file markers from the errors.
            var markers = new List<ScriptFileMarker>();

            foreach (var error in parseResult)
            {
                script.TryGetLineAndOffset(error.Start, out var startLine, out var startOffset);
                script.TryGetLineAndOffset(error.End, out var endLine, out var endOffset);

                // vscode specific format for error markers.
                markers.Add(new ScriptFileMarker
                {
                    Message = error.Message,
                    Level = ScriptFileMarkerLevel.Error,
                    ScriptRegion = new ScriptRegion
                    {
                        File = scriptFile.FilePath,
                        StartLineNumber = startLine,
                        StartColumnNumber = startOffset,
                        StartOffset = 0,
                        EndLineNumber = endLine,
                        EndColumnNumber = endOffset,
                        EndOffset = 0
                    }
                });
            }

            return markers.ToArray();
        }

        public DefinitionResult GetDefinition(string queryText, int index, int startLine, int startColumn,
            bool throwOnError = false)
        {
            //TODOKusto: API wasnt working properly, need to check that part.
            return null;
        }

        public Hover GetHoverHelp(ScriptDocumentInfo scriptDocumentInfo, Position textPosition, bool throwOnError = false)
        {
            var script = CodeScript.From(scriptDocumentInfo.Contents, schemaState);
            script.TryGetTextPosition(textPosition.Line + 1, textPosition.Character + 1, out int position);

            var codeBlock = script.GetBlockAtPosition(position);
            var quickInfo = codeBlock.Service.GetQuickInfo(position);

            return AutoCompleteHelper.ConvertQuickInfoToHover(
                quickInfo.Text,
                "kusto",
                scriptDocumentInfo.StartLine,
                scriptDocumentInfo.StartColumn,
                textPosition.Character);
        }

        public CompletionItem[] GetAutoCompleteSuggestions(ScriptDocumentInfo scriptDocumentInfo, Position textPosition,
            bool throwOnError = false)
        {
            var script = CodeScript.From(scriptDocumentInfo.Contents, schemaState);
            script.TryGetTextPosition(textPosition.Line + 1, textPosition.Character + 1, out int position); // Gets the actual offset based on line and local offset

            var codeBlock = script.GetBlockAtPosition(position);
            var completion = codeBlock.Service.GetCompletionItems(position);
            scriptDocumentInfo.ScriptParseInfo.CurrentSuggestions = completion.Items; // this is declaration item so removed for now, but keep the info when api gets updated

            var completions = new List<CompletionItem>();
            foreach (var autoCompleteItem in completion.Items)
            {
                var label = autoCompleteItem.DisplayText;
                var insertText = autoCompleteItem.Kind == CompletionKind.Table || autoCompleteItem.Kind == CompletionKind.Database
                    ? KustoQueryUtils.EscapeName(label)
                    : label;
                
                var completionKind = CreateCompletionItemKind(autoCompleteItem.Kind);
                completions.Add(AutoCompleteHelper.CreateCompletionItem(label, autoCompleteItem.Kind.ToString(),
                    insertText, completionKind, scriptDocumentInfo.StartLine, scriptDocumentInfo.StartColumn,
                    textPosition.Character));
            }

            return completions.ToArray();
        }
        
        private CompletionItemKind CreateCompletionItemKind(CompletionKind kustoKind)
        {
            switch (kustoKind)
            {
                case CompletionKind.Syntax:
                    return CompletionItemKind.Module;
                case CompletionKind.Column:
                    return CompletionItemKind.Field;
                case CompletionKind.Variable:
                    return CompletionItemKind.Variable;
                case CompletionKind.Table:
                    return CompletionItemKind.File;
                case CompletionKind.Database:
                    return CompletionItemKind.Method;
                case CompletionKind.LocalFunction:
                case CompletionKind.DatabaseFunction:
                case CompletionKind.BuiltInFunction:
                case CompletionKind.AggregateFunction:
                    return CompletionItemKind.Function;
                default:
                    return CompletionItemKind.Keyword;
            }
        }
    }
}