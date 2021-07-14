//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Linq;
using Kusto.Language;
using Kusto.Language.Editor;
using Microsoft.Kusto.ServiceLayer.DataSource.Intellisense;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Kusto
{
    /// <summary>
    /// Kusto specific class for intellisense helper functions.
    /// </summary>
    public class KustoIntellisenseHelper
    {
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
            }

            return markers.ToArray();
        }
    }
}
