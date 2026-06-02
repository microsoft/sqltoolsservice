//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using System.Threading;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion
{
    /// <summary>
    /// A service to create auto complete list for given script document 
    /// </summary>
    internal class CompletionService
    {
        private ConnectedBindingQueue BindingQueue { get; set; }

        /// <summary>
        /// Created new instance given binding queue
        /// </summary>
        public CompletionService(ConnectedBindingQueue bindingQueue)
        {
            BindingQueue = bindingQueue;
        }

        private ISqlParserWrapper sqlParserWrapper;

        /// <summary>
        /// SQL parser wrapper to create the completion list
        /// </summary>
        public ISqlParserWrapper SqlParserWrapper
        {
            get
            {
                this.sqlParserWrapper ??= new SqlParserWrapper();
                return this.sqlParserWrapper;
            }
            set
            {
                this.sqlParserWrapper = value;
            }
        }

        /// <summary>
        /// Creates a completion list given connection and document info
        /// </summary>
        public AutoCompletionResult CreateCompletions(
            ConnectionInfo connInfo,
            ScriptDocumentInfo scriptDocumentInfo,
            bool useLowerCaseSuggestions,
            int bindingTimeout = LanguageService.BindingTimeout,
            CancellationToken cancellationToken = default)
        {
            AutoCompletionResult result = new AutoCompletionResult();
            ScriptParseInfo scriptParseInfo = scriptDocumentInfo.ScriptParseInfo;
            bool hasBindingContext = (scriptParseInfo.IsConnected || scriptParseInfo.IsProject) && scriptParseInfo.ConnectionKey != null;
            Logger.Verbose(
                $"CreateCompletions start ownerUri={connInfo?.OwnerUri ?? "<null>"} bindingTimeout={bindingTimeout} " +
                $"bindingContextKind={scriptParseInfo.BindingContextKind} hasConnectionKey={scriptParseInfo.ConnectionKey != null} " +
                $"hasParseResult={scriptParseInfo.ParseResult != null} parserLine={scriptDocumentInfo.ParserLine} " +
                $"parserColumn={scriptDocumentInfo.ParserColumn} tokenText='{scriptDocumentInfo.TokenText ?? "<null>"}'");

            // check if the file has a binding context ready and the file lock is available
            if (hasBindingContext && Monitor.TryEnter(scriptParseInfo.BuildingMetadataLock, bindingTimeout))
            {
                try
                {
                    QueueItem queueItem = AddToQueue(connInfo, scriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions, bindingTimeout, cancellationToken);

                    // wait for the queue item
                    if (!LanguageService.WaitForQueueItem(queueItem, bindingTimeout, "completion"))
                    {
                        Logger.Warning($"Completion queue wait timed out after {bindingTimeout} ms; returning default completions");
                        result = CreateDefaultCompletionItems(scriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions, "completion queue wait timeout");
                    }
                    else
                    {
                        Logger.Verbose($"Finished processing completion request for {connInfo?.OwnerUri ?? "<null>"} in CompletionService.CreateCompletions");
                        var completionResult = queueItem.GetResultAsT<AutoCompletionResult>();
                        int parserCompletionCount = GetCompletionCount(completionResult?.CompletionItems);
                        Logger.Verbose($"Completion queue returned {parserCompletionCount} parser items");

                        if (completionResult != null && completionResult.CompletionItems != null && completionResult.CompletionItems.Length > 0)
                        {
                            result = completionResult;
                        }
                        else if (!ShouldShowCompletionList(scriptDocumentInfo.Token))
                        {
                            Logger.Verbose("Completion list suppressed because the cursor is inside a comment token");
                            result.CompleteResult(AutoCompleteHelper.EmptyCompletionList);
                        }
                        else
                        {
                            Logger.Verbose("Parser returned no completion items; returning default completions");
                            result = CreateDefaultCompletionItems(scriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions, "parser returned null or empty completions");
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(scriptParseInfo.BuildingMetadataLock);
                }
            }
            else if (!cancellationToken.IsCancellationRequested)
            {
                string reason = hasBindingContext
                    ? $"metadata lock unavailable after {bindingTimeout} ms"
                    : $"no binding context available; bindingContextKind={scriptParseInfo.BindingContextKind}; hasConnectionKey={scriptParseInfo.ConnectionKey != null}";
                Logger.Verbose($"CreateCompletions returning defaults because {reason}");
                result = CreateDefaultCompletionItems(scriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions, reason);
            }
            else
            {
                Logger.Verbose("CreateCompletions canceled before completion list was created");
            }

            Logger.Verbose($"Sending {GetCompletionCount(result.CompletionItems)} completion items for {connInfo?.OwnerUri ?? "<null>"} in CompletionService.CreateCompletions");
            return result;
        }

        private QueueItem AddToQueue(
            ConnectionInfo connInfo,
            ScriptParseInfo scriptParseInfo,
            ScriptDocumentInfo scriptDocumentInfo,
            bool useLowerCaseSuggestions,
            int bindingTimeout,
            CancellationToken cancellationToken)
        {
            // queue the completion task with the binding queue    
            QueueItem queueItem = this.BindingQueue.QueueBindingOperation(
                key: scriptParseInfo.ConnectionKey,
                bindingTimeout: bindingTimeout,
                bindOperation: (bindingContext, cancelToken) =>
                {
                    if (cancelToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                    {
                        Logger.Verbose("Completion bind operation canceled before SQL parser execution; returning default completions");
                        return CreateDefaultCompletionItems(scriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions, "completion bind operation canceled");
                    }

                    Logger.Verbose($"Completion bind operation running SQL parser for key={scriptParseInfo.ConnectionKey}");
                    return CreateCompletionsFromSqlParser(connInfo, scriptParseInfo, scriptDocumentInfo, bindingContext.MetadataDisplayInfoProvider, cancelToken, cancellationToken);
                },
                timeoutOperation: (bindingContext) =>
                {
                    // return the default list if the connected bind fails
                    Logger.Warning($"Completion bind operation timed out after {bindingTimeout} ms for key={scriptParseInfo.ConnectionKey}; returning default completions");
                    return CreateDefaultCompletionItems(scriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions, "completion bind operation timeout");
                },
                errorHandler: ex =>
                {
                    // return the default list if an unexpected exception occurs
                    Logger.Error($"Completion bind operation failed for key={scriptParseInfo.ConnectionKey}; returning default completions. Exception: {ex}");
                    return CreateDefaultCompletionItems(scriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions, "completion bind operation exception");
                });
            return queueItem;
        }

        private static bool ShouldShowCompletionList(Token token)
        {
            bool result = true;
            if (token != null)
            {
                switch (token.Id)
                {
                    case (int)Tokens.LEX_MULTILINE_COMMENT:
                    case (int)Tokens.LEX_END_OF_LINE_COMMENT:
                        result = false;
                        break;
                }
            }
            return result;
        }

        private AutoCompletionResult CreateDefaultCompletionItems(
            ScriptParseInfo scriptParseInfo,
            ScriptDocumentInfo scriptDocumentInfo,
            bool useLowerCaseSuggestions,
            string reason = null)
        {
            AutoCompletionResult result = new AutoCompletionResult();
            CompletionItem[] completionList = AutoCompleteHelper.GetDefaultCompletionItems(scriptDocumentInfo, useLowerCaseSuggestions);
            result.CompleteResult(completionList);
            Logger.Verbose(
                $"Created {GetCompletionCount(completionList)} default completion items; reason={reason ?? "unspecified"}; " +
                $"bindingContextKind={scriptParseInfo?.BindingContextKind.ToString() ?? "<null>"} tokenText='{scriptDocumentInfo.TokenText ?? "<null>"}'");
            return result;
        }

        private AutoCompletionResult CreateCompletionsFromSqlParser(
            ConnectionInfo connInfo,
            ScriptParseInfo scriptParseInfo,
            ScriptDocumentInfo scriptDocumentInfo,
            MetadataDisplayInfoProvider metadataDisplayInfoProvider,
            CancellationToken queueCancellationToken,
            CancellationToken requestCancellationToken)
        {
            AutoCompletionResult result = new AutoCompletionResult();
            IEnumerable<Declaration> suggestions = SqlParserWrapper.FindCompletions(
                scriptParseInfo.ParseResult,
                scriptDocumentInfo.ParserLine,
                scriptDocumentInfo.ParserColumn,
                metadataDisplayInfoProvider);

            if (queueCancellationToken.IsCancellationRequested || requestCancellationToken.IsCancellationRequested)
            {
                return result;
            }

            // get the completion list from SQL Parser
            scriptParseInfo.CurrentSuggestions = suggestions;

            // convert the suggestion list to the VS Code format
            CompletionItem[] completionList = AutoCompleteHelper.ConvertDeclarationsToCompletionItems(
                scriptParseInfo.CurrentSuggestions,
                scriptDocumentInfo.StartLine,
                scriptDocumentInfo.StartColumn,
                scriptDocumentInfo.EndColumn,
                scriptDocumentInfo.TokenText);

            Logger.Verbose($"SQL parser produced {GetCompletionCount(completionList)} completion items before star expansion");

            // Star expansion uses the binder's BoundTables — works for both live and project contexts.
            // Returns null if not a star expression, so safe to always call.
            CompletionItem[] starExpansionSuggestion = AutoCompleteHelper.ExpandSqlStarExpression(scriptDocumentInfo);
            if (starExpansionSuggestion != null)
            {
                completionList = [.. starExpansionSuggestion, .. completionList];
                Logger.Verbose(
                    $"Added {GetCompletionCount(starExpansionSuggestion)} star expansion completion items; " +
                    $"combined count={GetCompletionCount(completionList)}");
            }

            result.CompleteResult(completionList);
            Logger.Verbose($"SQL parser completion result completed with {GetCompletionCount(result.CompletionItems)} items");
            if (!scriptParseInfo.IsProject)
            {
                connInfo.IntellisenseMetrics.UpdateMetrics(result.Duration, 1, (k2, v2) => v2 + 1);
            }
            return result;
        }

        private static int GetCompletionCount(CompletionItem[] completionItems)
        {
            return completionItems?.Length ?? 0;
        }
    }
}
