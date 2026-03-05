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
        private const int QueueItemWaitTimeoutBufferMs = 1000;
        private ConnectedBindingQueue BindingQueue { get; set; }

        private const string CompletionRequestStallEnvVar = "SQLTOOLS_TEST_STALL_COMPLETION_REQUEST";
        private const int CompletionRequestStallThreshold = 1;
        // private static int completionRequestCount;
        private static int parserCompletionRequestCount;


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
            CancellationToken callerCancellation = default)
        {
            Logger.Verbose($"CompletionService.CreateCompletions: start (isConnected={scriptDocumentInfo.ScriptParseInfo.IsConnected}, callerCancelled={callerCancellation.IsCancellationRequested}).");

            if (callerCancellation.IsCancellationRequested)
            {
                Logger.Verbose("CompletionService.CreateCompletions: caller already cancelled before queueing; returning default completions.");
                return CreateDefaultCompletionItems(scriptDocumentInfo.ScriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions);
            }

            AutoCompletionResult result = new AutoCompletionResult();
            // check if the file is connected and the file lock is available
            if (scriptDocumentInfo.ScriptParseInfo.IsConnected && Monitor.TryEnter(scriptDocumentInfo.ScriptParseInfo.BuildingMetadataLock))
            {
                try
                {
                    QueueItem queueItem = AddToQueue(connInfo, scriptDocumentInfo.ScriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions, callerCancellation);

                    // wait for the queue item with a bounded timeout
                    if (!WaitForQueueItem(queueItem))
                    {
                        Logger.Verbose("CompletionService.CreateCompletions: queue wait timed out; returning default completions.");
                        return CreateDefaultCompletionItems(scriptDocumentInfo.ScriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions);
                    }

                    if (callerCancellation.IsCancellationRequested)
                    {
                        Logger.Verbose("CompletionService.CreateCompletions: caller cancelled after queue completion; returning default completions.");
                        return CreateDefaultCompletionItems(scriptDocumentInfo.ScriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions);
                    }

                    var completionResult = queueItem.GetResultAsT<AutoCompletionResult>();
                    if (completionResult != null && completionResult.CompletionItems != null && completionResult.CompletionItems.Length > 0)
                    {
                        Logger.Verbose($"CompletionService.CreateCompletions: queue returned {completionResult.CompletionItems.Length} completion items.");
                        result = completionResult;
                    }
                    else if (!ShouldShowCompletionList(scriptDocumentInfo.Token))
                    {
                        Logger.Verbose("CompletionService.CreateCompletions: token context suppresses completion list.");
                        result.CompleteResult(AutoCompleteHelper.EmptyCompletionList);
                    }
                }
                finally
                {
                    Monitor.Exit(scriptDocumentInfo.ScriptParseInfo.BuildingMetadataLock);
                }
            }
            else
            {
                Logger.Verbose("CompletionService.CreateCompletions: metadata lock unavailable or script is disconnected; returning default result.");
            }

            return result;
        }

        private QueueItem AddToQueue(
            ConnectionInfo connInfo,
            ScriptParseInfo scriptParseInfo,
            ScriptDocumentInfo scriptDocumentInfo,
            bool useLowerCaseSuggestions,
            CancellationToken callerCancellation = default)
        {
            Logger.Verbose($"CompletionService.AddToQueue: queueing completion bind operation (bindingTimeoutMs={LanguageService.BindingTimeout}, callerCancelled={callerCancellation.IsCancellationRequested}).");

            // queue the completion task with the binding queue    
            QueueItem queueItem = this.BindingQueue.QueueBindingOperation(
                key: scriptParseInfo.ConnectionKey,
                bindingTimeout: LanguageService.BindingTimeout,
                callerCancellation: callerCancellation,
                bindOperation: (bindingContext, cancelToken) =>
                {
                    if (callerCancellation.IsCancellationRequested || cancelToken.IsCancellationRequested)
                    {
                        Logger.Verbose("CompletionService.AddToQueue: bind operation observed cancellation; returning default completions.");
                        return CreateDefaultCompletionItems(scriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions);
                    }

                    return CreateCompletionsFromSqlParser(connInfo, scriptParseInfo, scriptDocumentInfo, bindingContext.MetadataDisplayInfoProvider);
                },
                timeoutOperation: (bindingContext) =>
                {
                    // return the default list if the connected bind fails
                    return CreateDefaultCompletionItems(scriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions);
                },
                errorHandler: ex =>
                {
                    // return the default list if an unexpected exception occurs
                    return CreateDefaultCompletionItems(scriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions);
                });
            return queueItem;
        }

        private static bool WaitForQueueItem(QueueItem queueItem)
        {
            int waitTimeoutMs = LanguageService.BindingTimeout + QueueItemWaitTimeoutBufferMs;
            if (queueItem.Completed.Wait(waitTimeoutMs))
            {
                Logger.Verbose($"CompletionService.WaitForQueueItem: queue item completed within {waitTimeoutMs} ms.");
                return true;
            }

            Logger.Warning($"CreateCompletions timed out waiting for binding queue item completion after {waitTimeoutMs} ms.");
            return false;
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

        private AutoCompletionResult CreateDefaultCompletionItems(ScriptParseInfo scriptParseInfo, ScriptDocumentInfo scriptDocumentInfo, bool useLowerCaseSuggestions)
        {
            AutoCompletionResult result = new AutoCompletionResult();
            CompletionItem[] completionList = AutoCompleteHelper.GetDefaultCompletionItems(scriptDocumentInfo, useLowerCaseSuggestions);
            result.CompleteResult(completionList);
            return result;
        }

        private AutoCompletionResult CreateCompletionsFromSqlParser(
            ConnectionInfo connInfo,
            ScriptParseInfo scriptParseInfo,
            ScriptDocumentInfo scriptDocumentInfo,
            MetadataDisplayInfoProvider metadataDisplayInfoProvider)
        {
            Logger.Verbose($"CompletionService.CreateCompletionsFromSqlParser: computing parser completions at parser line={scriptDocumentInfo.ParserLine}, column={scriptDocumentInfo.ParserColumn}.");;
            AutoCompletionResult result = new AutoCompletionResult();

            IEnumerable<Declaration> suggestions = SqlParserWrapper.FindCompletions(
                scriptParseInfo.ParseResult,
                scriptDocumentInfo.ParserLine,
                scriptDocumentInfo.ParserColumn,
                metadataDisplayInfoProvider);

            // get the completion list from SQL Parser
            scriptParseInfo.CurrentSuggestions = suggestions;

            // convert the suggestion list to the VS Code format
            CompletionItem[] completionList = AutoCompleteHelper.ConvertDeclarationsToCompletionItems(
                scriptParseInfo.CurrentSuggestions,
                scriptDocumentInfo.StartLine,
                scriptDocumentInfo.StartColumn,
                scriptDocumentInfo.EndColumn,
                scriptDocumentInfo.TokenText);

            result.CompleteResult(completionList);

            Logger.Verbose($"CompletionService.CreateCompletionsFromSqlParser: parser produced {completionList?.Length ?? 0} completion items.");

            //The bucket for number of milliseconds will take to send back auto complete list
            connInfo.IntellisenseMetrics.UpdateMetrics(result.Duration, 1, (k2, v2) => v2 + 1);
            return result;
        }
    }
}
