//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Kusto.Language;
using KustoDiagnostic = Kusto.Language.Diagnostic;
using Kusto.Language.Editor;
using Kusto.Language.Syntax;
using Kusto.Language.Symbols;
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
        /// <summary>
        /// Convert CLR type name into a Kusto scalar type.
        /// </summary>
        private static ScalarSymbol GetKustoType(string clrTypeName)
        {
            switch (clrTypeName)
            {
                case "System.Byte":
                case "Byte":
                case "byte":
                case "System.SByte":
                case "SByte":
                case "sbyte":
                case "System.Int16":
                case "Int16":
                case "short":
                case "System.UInt16":
                case "UInt16":
                case "ushort":
                case "System.Int32":
                case "System.Single":
                case "Int32":
                case "int":
                    return ScalarTypes.Int;
                case "System.UInt32": // unsigned ints don't fit into int, use long
                case "UInt32":
                case "uint":
                case "System.Int64":
                case "Int64":
                case "long":
                    return ScalarTypes.Long;
                case "System.Double":
                case "Double":
                case "double":
                case "float":
                    return ScalarTypes.Real;
                case "System.UInt64": // unsigned longs do not fit into long, use decimal
                case "UInt64":
                case "ulong":
                case "System.Decimal":
                case "Decimal":
                case "decimal":
                case "System.Data.SqlTypes.SqlDecimal":
                case "SqlDecimal":
                    return ScalarTypes.Decimal;
                case "System.Guid":
                case "Guid":
                    return ScalarTypes.Guid;
                case "System.DateTime":
                case "DateTime":
                    return ScalarTypes.DateTime;
                case "System.TimeSpan":
                case "TimeSpan":
                    return ScalarTypes.TimeSpan;
                case "System.String":
                case "String":
                case "string":
                    return ScalarTypes.String;
                case "System.Boolean":
                case "Boolean":
                case "bool":
                    return ScalarTypes.Bool;
                case "System.Object":
                case "Object":
                case "object":
                    return ScalarTypes.Dynamic;
                case "System.Type":
                case "Type":
                    return ScalarTypes.Type;
                default:
                    throw new InvalidOperationException($"Unhandled clr type: {clrTypeName}");
            }
        }

        private static IReadOnlyList<Parameter> NoParameters = new Parameter[0];

        /// <summary>
        /// Translate Kusto parameter list declaration into into list of <see cref="Parameter"/> instances.
        /// </summary>
        private static IReadOnlyList<Parameter> TranslateParameters(string parameters)
        {
            parameters = parameters.Trim();

            if (string.IsNullOrEmpty(parameters) || parameters == "()")
                return NoParameters;

            if (parameters[0] != '(')
                parameters = "(" + parameters;
            if (parameters[parameters.Length - 1] != ')')
                parameters = parameters + ")";

            var query = "let fn = " + parameters + " { };";
            var code = KustoCode.ParseAndAnalyze(query);
            var let = code.Syntax.GetFirstDescendant<LetStatement>();
            
             FunctionSymbol function = let.Name.ReferencedSymbol is VariableSymbol variable
                ? variable.Type as FunctionSymbol
                : let.Name.ReferencedSymbol as FunctionSymbol;

            return function.Signatures[0].Parameters;
        }

        /// <summary>
        /// Loads the schema for the specified databasea into a a <see cref="DatabaseSymbol"/>.
        /// </summary>
        private static DatabaseSymbol LoadDatabaseAsync(IEnumerable<ShowDatabaseSchemaResult> tableSchemas,
            IEnumerable<ShowFunctionsResult> functionSchemas,
            string databaseName)
        {
            if (tableSchemas == null)
            {
                return null;
            }

            tableSchemas = tableSchemas
                .Where(r => !string.IsNullOrEmpty(r.TableName) && !string.IsNullOrEmpty(r.ColumnName))
                .ToArray();

            var members = new List<Symbol>();
            foreach (var table in tableSchemas.GroupBy(s => s.TableName))
            {
                var columns = table.Select(s => new ColumnSymbol(s.ColumnName, GetKustoType(s.ColumnType))).ToList();
                var tableSymbol = new TableSymbol(table.Key, columns);
                members.Add(tableSymbol);
            }

            if (functionSchemas == null)
            {
                return null;
            }

            foreach (var fun in functionSchemas)
            {
                var parameters = TranslateParameters(fun.Parameters);
                var functionSymbol = new FunctionSymbol(fun.Name, fun.Body, parameters);
                members.Add(functionSymbol);
            }

            return new DatabaseSymbol(databaseName, members);
        }

        public static CompletionItemKind CreateCompletionItemKind(CompletionKind kustoKind)
        {
            CompletionItemKind kind = CompletionItemKind.Variable;
            switch (kustoKind)
            {
                case CompletionKind.Syntax:
                    kind = CompletionItemKind.Module;
                    break;
                case CompletionKind.Column:
                    kind = CompletionItemKind.Field;
                    break;
                case CompletionKind.Variable:
                    kind = CompletionItemKind.Variable;
                    break;
                case CompletionKind.Table:
                    kind = CompletionItemKind.File;
                    break;
                case CompletionKind.Database:
                    kind = CompletionItemKind.Method;
                    break;
                case CompletionKind.LocalFunction:
                case CompletionKind.DatabaseFunction:
                case CompletionKind.BuiltInFunction:
                case CompletionKind.AggregateFunction:
                    kind = CompletionItemKind.Function;
                    break;
                default:
                    kind = CompletionItemKind.Keyword;
                    break;
            }

            return kind;
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

        /// <summary>
        /// Loads the schema for the specified database and returns a new <see cref="GlobalState"/> with the database added or updated.
        /// </summary>
        public static GlobalState AddOrUpdateDatabase(IEnumerable<ShowDatabaseSchemaResult> tableSchemas,
            IEnumerable<ShowFunctionsResult> functionSchemas, GlobalState globals,
            string databaseName, string clusterName)
        {
            // try and show error from here.
            DatabaseSymbol databaseSymbol = null;

            if (databaseName != null)
            {
                databaseSymbol = LoadDatabaseAsync(tableSchemas, functionSchemas, databaseName);
            }

            if (databaseSymbol == null)
            {
                return globals;
            }

            var cluster = globals.GetCluster(clusterName);
            if (cluster == null)
            {
                cluster = new ClusterSymbol(clusterName, new[] {databaseSymbol}, isOpen: true);
                globals = globals.AddOrUpdateCluster(cluster);
            }
            else
            {
                cluster = cluster.AddOrUpdateDatabase(databaseSymbol);
                globals = globals.AddOrUpdateCluster(cluster);
            }

            globals = globals.WithCluster(cluster).WithDatabase(databaseSymbol);

            return globals;
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
