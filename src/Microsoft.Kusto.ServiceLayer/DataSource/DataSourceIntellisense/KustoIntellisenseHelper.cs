//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Linq;
using Kusto.Language;
using KustoDiagnostic = Kusto.Language.Diagnostic;
using Kusto.Language.Editor;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Completion;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;

namespace Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense
{
    /// <summary>
    /// Kusto specific class for intellisense helper functions.
    /// </summary>
    public class KustoIntellisenseHelper
    {
        public static CompletionItemKind CreateCompletionItemKind(CompletionKind kustoKind)
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

        /// <summary>
        /// Gets default keyword when user if not connected to any Kusto cluster.
        /// </summary>
        public static LanguageServices.Contracts.CompletionItem[] GetDefaultKeywords(
            ScriptDocumentInfo scriptDocumentInfo, Position textDocumentPosition)
        {
            var kustoCodeService = new KustoCodeService(scriptDocumentInfo.Contents, GlobalState.Default);
            var script = CodeScript.From(scriptDocumentInfo.Contents, GlobalState.Default);
            script.TryGetTextPosition(textDocumentPosition.Line + 1, textDocumentPosition.Character,
                out int position); // Gets the actual offset based on line and local offset      
            var completion = kustoCodeService.GetCompletionItems(position);

            List<LanguageServices.Contracts.CompletionItem> completions =
                new List<LanguageServices.Contracts.CompletionItem>();
            foreach (var autoCompleteItem in completion.Items)
            {
                var label = autoCompleteItem.DisplayText;
                // convert the completion item candidates into vscode format CompletionItems
                completions.Add(AutoCompleteHelper.CreateCompletionItem(label, label + " keyword", label,
                    CompletionItemKind.Keyword, scriptDocumentInfo.StartLine, scriptDocumentInfo.StartColumn,
                    textDocumentPosition.Character));
            }

            return completions.ToArray();
        }

        /// <summary>
        /// Gets default diagnostics when user if not connected to any Kusto cluster.
        /// </summary>
        public static ScriptFileMarker[] GetDefaultDiagnostics(ScriptParseInfo parseInfo, ScriptFile scriptFile,
            string queryText)
        {
            var kustoCodeService = new KustoCodeService(queryText, GlobalState.Default);
            var script = CodeScript.From(queryText, GlobalState.Default);
            var parseResult = kustoCodeService.GetDiagnostics();

            parseInfo.ParseResult = parseResult;

            // build a list of Kusto script file markers from the errors.
            List<ScriptFileMarker> markers = new List<ScriptFileMarker>();
            if (parseResult != null && parseResult.Count() > 0)
            {
                foreach (var error in parseResult)
                {
                    script.TryGetLineAndOffset(error.Start, out var startLine, out var startOffset);
                    script.TryGetLineAndOffset(error.End, out var endLine, out var endOffset);

                    // vscode specific format for error markers.
                    markers.Add(new ScriptFileMarker()
                    {
                        Message = error.Message,
                        Level = ScriptFileMarkerLevel.Error,
                        ScriptRegion = new ScriptRegion()
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
            }

            return markers.ToArray();
        }

        /// <inheritdoc/>
        public static LanguageServices.Contracts.CompletionItem[] GetAutoCompleteSuggestions(
            ScriptDocumentInfo scriptDocumentInfo, Position textPosition, GlobalState schemaState,
            bool throwOnError = false)
        {
            var script = CodeScript.From(scriptDocumentInfo.Contents, schemaState);
            script.TryGetTextPosition(textPosition.Line + 1, textPosition.Character + 1,
                out int position); // Gets the actual offset based on line and local offset

            var codeBlock = script.GetBlockAtPosition(position);
            var completion = codeBlock.Service.GetCompletionItems(position);
            scriptDocumentInfo.ScriptParseInfo.CurrentSuggestions =
                completion.Items; // this is declaration item so removed for now, but keep the info when api gets updated

            var completions = new List<LanguageServices.Contracts.CompletionItem>();
            foreach (var autoCompleteItem in completion.Items)
            {
                var label = autoCompleteItem.DisplayText;
                var insertText = autoCompleteItem.Kind == CompletionKind.Table
                    ? KustoQueryUtils.EscapeName(label)
                    : label;
                
                var completionKind = CreateCompletionItemKind(autoCompleteItem.Kind);
                completions.Add(AutoCompleteHelper.CreateCompletionItem(label, autoCompleteItem.Kind.ToString(),
                    insertText, completionKind, scriptDocumentInfo.StartLine, scriptDocumentInfo.StartColumn,
                    textPosition.Character));
            }

            return completions.ToArray();
        }

        /// <inheritdoc/>
        public static Hover GetHoverHelp(ScriptDocumentInfo scriptDocumentInfo, Position textPosition,
            GlobalState schemaState, bool throwOnError = false)
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

        /// <inheritdoc/>
        public static DefinitionResult GetDefinition(string queryText, int index, int startLine, int startColumn,
            GlobalState schemaState, bool throwOnError = false)
        {
            var abc = KustoCode.ParseAndAnalyze(queryText,
                schemaState); //TODOKusto: API wasnt working properly, need to check that part.
            var kustoCodeService = new KustoCodeService(abc);
            //var kustoCodeService = new KustoCodeService(queryText, globals);
            var relatedInfo = kustoCodeService.GetRelatedElements(index);

            if (relatedInfo != null && relatedInfo.Elements.Count > 1)
            {
            }

            return null;
        }

        /// <inheritdoc/>
        public static ScriptFileMarker[] GetSemanticMarkers(ScriptParseInfo parseInfo, ScriptFile scriptFile,
            string queryText, GlobalState schemaState)
        {
            var kustoCodeService = new KustoCodeService(queryText, schemaState);
            var script = CodeScript.From(queryText, schemaState);
            var parseResult = new List<KustoDiagnostic>();

            foreach (var codeBlock in script.Blocks)
            {
                parseResult.AddRange(codeBlock.Service.GetDiagnostics());
            }

            parseInfo.ParseResult = parseResult;

            // build a list of Kusto script file markers from the errors.
            List<ScriptFileMarker> markers = new List<ScriptFileMarker>();
            if (parseResult != null && parseResult.Any())
            {
                foreach (var error in parseResult)
                {
                    script.TryGetLineAndOffset(error.Start, out var startLine, out var startOffset);
                    script.TryGetLineAndOffset(error.End, out var endLine, out var endOffset);

                    // vscode specific format for error markers.
                    markers.Add(new ScriptFileMarker()
                    {
                        Message = error.Message,
                        Level = ScriptFileMarkerLevel.Error,
                        ScriptRegion = new ScriptRegion()
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
            }

            return markers.ToArray();
        }
    }
}
