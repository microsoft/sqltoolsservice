//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Threading;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;

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
                if(this.sqlParserWrapper == null)
                {
                    this.sqlParserWrapper = new SqlParserWrapper();
                }
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
            bool useLowerCaseSuggestions)
        {
            AutoCompletionResult result = new AutoCompletionResult();
            // check if the file is connected and the file lock is available
            if (scriptDocumentInfo.ScriptParseInfo.IsConnected && Monitor.TryEnter(scriptDocumentInfo.ScriptParseInfo.BuildingMetadataLock))
            {
                try
                {
                    QueueItem queueItem = AddToQueue(connInfo, scriptDocumentInfo.ScriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions);

                    // wait for the queue item
                    queueItem.ItemProcessed.WaitOne();
                    var completionResult = queueItem.GetResultAsT<AutoCompletionResult>();
                    if (completionResult != null && completionResult.CompletionItems != null && completionResult.CompletionItems.Length > 0)
                    {
                        result = completionResult;
                    }
                    else if (!ShouldShowCompletionList(scriptDocumentInfo.Token))
                    {
                        result.CompleteResult(AutoCompleteHelper.EmptyCompletionList);
                    }
                }
                finally
                {
                    Monitor.Exit(scriptDocumentInfo.ScriptParseInfo.BuildingMetadataLock);
                }
            }

            return result;
        }

        private QueueItem AddToQueue(
            ConnectionInfo connInfo,
            ScriptParseInfo scriptParseInfo,
            ScriptDocumentInfo scriptDocumentInfo,
            bool useLowerCaseSuggestions)
        {
            // queue the completion task with the binding queue    
            QueueItem queueItem = this.BindingQueue.QueueBindingOperation(
                key: scriptParseInfo.ConnectionKey,
                bindingTimeout: LanguageService.BindingTimeout,
                bindOperation: (bindingContext, cancelToken) =>
                {
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

            //The bucket for number of milliseconds will take to send back auto complete list
            connInfo.IntellisenseMetrics.UpdateMetrics(result.Duration, 1, (k2, v2) => v2 + 1);
            return result;
        }
    }
}
