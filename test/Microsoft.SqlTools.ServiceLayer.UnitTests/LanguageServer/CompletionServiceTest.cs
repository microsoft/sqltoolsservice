//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using Microsoft.SqlTools.LanguageService.LanguageServices.Completion;
using Microsoft.SqlTools.LanguageService.LanguageServices.Contracts;
using Microsoft.SqlTools.LanguageService.Workspace.Contracts;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    public class CompletionServiceTest
    {
        // Disable flaky test (mairvine - 3/15/2018)
        // [Test]
        public void CompletionItemsShouldCreatedUsingSqlParserIfTheProcessDoesNotTimeout()
        {
            ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue();
            ScriptDocumentInfo docInfo = CreateScriptDocumentInfo();
            CompletionService completionService = new CompletionService(bindingQueue);
            ConnectionInfo connectionInfo = new ConnectionInfo(null, null, null);
            bool useLowerCaseSuggestions = true;
            CompletionItem[] defaultCompletionList = AutoCompleteHelper.GetDefaultCompletionItems(docInfo, useLowerCaseSuggestions);

            List<Declaration> declarations = new List<Declaration>();

            var sqlParserWrapper = new Mock<ISqlParserWrapper>();
            sqlParserWrapper.Setup(x => x.FindCompletions(docInfo.ScriptParseInfo.ParseResult, docInfo.ParserLine, docInfo.ParserColumn, 
                It.IsAny<IMetadataDisplayInfoProvider>())).Returns(declarations);
            completionService.SqlParserWrapper = sqlParserWrapper.Object;

            AutoCompletionResult result = completionService.CreateCompletions(connectionInfo, docInfo, useLowerCaseSuggestions);
            Assert.NotNull(result);
            var count = result.CompletionItems == null ? 0 : result.CompletionItems.Length;

            Assert.That(count, Is.Not.EqualTo(defaultCompletionList.Length));
        }

        /// <summary>
        /// Protects the #21930 large-dbo case: valid semantic completion can take longer than the
        /// 500 ms slow threshold and must remain eligible to win before the 2-second hard timeout.
        /// </summary>
        [Test]
        public void CompletionSlowOperationUsesParserResultBeforeHardTimeout()
        {
            using ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue();
            ScriptDocumentInfo docInfo = CreateScriptDocumentInfo();
            CompletionService completionService = new CompletionService(bindingQueue);
            ConnectionInfo connectionInfo = new ConnectionInfo(null, null, null);
            bool useLowerCaseSuggestions = true;
            List<Declaration> declarations = new List<Declaration>();
            CompletionItem[] defaultCompletionList = AutoCompleteHelper.GetDefaultCompletionItems(docInfo, useLowerCaseSuggestions);

            var sqlParserWrapper = new Mock<ISqlParserWrapper>();
            sqlParserWrapper.Setup(x => x.FindCompletions(docInfo.ScriptParseInfo.ParseResult, docInfo.ParserLine, docInfo.ParserColumn,
                It.IsAny<IMetadataDisplayInfoProvider>())).Callback(() => Thread.Sleep(ConnectedBindingQueue.BindingTimeout + 100)).Returns(declarations);
            completionService.SqlParserWrapper = sqlParserWrapper.Object;

            try
            {
                AutoCompletionResult result = completionService.CreateCompletions(connectionInfo, docInfo, useLowerCaseSuggestions);

                Assert.That(completionService.HardTimeout, Is.EqualTo(2_000));
                Assert.That(defaultCompletionList, Is.Not.Empty);
                Assert.NotNull(result);
                Assert.That(result.CompletionItems, Is.Null,
                    "Crossing the slow threshold must not select the default timeout result.");
                Assert.True(connectionInfo.IntellisenseMetrics.Quantile.Any());
            }
            finally
            {
                bindingQueue.StopQueueProcessor(2_000);
            }
        }

        /// <summary>
        /// Protects the real #22236 SMO blocking shape: a hard timeout is an infrastructure
        /// failure, not a successful default-keyword result, even if the parser is still blocked.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public void CompletionHardTimeoutReturnsFailureWhileParserIsStillBlocked()
        {
            using var operationStarted = new ManualResetEvent(false);
            using var operationFinished = new ManualResetEvent(false);
            using var releaseOperation = new ManualResetEvent(false);
            using ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue();
            ScriptDocumentInfo docInfo = CreateScriptDocumentInfo();
            CompletionService completionService = new CompletionService(bindingQueue)
            {
                HardTimeout = 150
            };
            ConnectionInfo connectionInfo = new ConnectionInfo(null, null, null);

            var sqlParserWrapper = new Mock<ISqlParserWrapper>();
            sqlParserWrapper.Setup(x => x.FindCompletions(
                docInfo.ScriptParseInfo.ParseResult,
                docInfo.ParserLine,
                docInfo.ParserColumn,
                It.IsAny<IMetadataDisplayInfoProvider>()))
                .Callback(() =>
                {
                    operationStarted.Set();
                    releaseOperation.WaitOne();
                    operationFinished.Set();
                })
                .Returns(new List<Declaration>());
            completionService.SqlParserWrapper = sqlParserWrapper.Object;

            try
            {
                AutoCompletionResult result = completionService.CreateCompletions(
                    connectionInfo,
                    docInfo,
                    useLowerCaseSuggestions: true);

                Assert.That(operationStarted.WaitOne(0), Is.True);
                Assert.That(result, Is.Null, "A hard timeout must be returned as a failure, not a fallback success.");
                Assert.That(operationFinished.WaitOne(0), Is.False,
                    "The caller should return before the non-cooperative parser operation finishes.");
            }
            finally
            {
                releaseOperation.Set();
                Assert.That(operationFinished.WaitOne(TimeSpan.FromSeconds(1)), Is.True);
                bindingQueue.StopQueueProcessor(2_000);
            }
        }

        /// <summary>
        /// Reproduces the PR5 contention path: a lazy parser/SMO lookup may block, but it must
        /// not hold BuildingMetadataLock and prevent the editor's current document from changing.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task CompletionDoesNotHoldDocumentLockWhileParserIsBlocked()
        {
            using var operationStarted = new ManualResetEvent(false);
            using var releaseOperation = new ManualResetEvent(false);
            using ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue();
            ScriptDocumentInfo docInfo = CreateScriptDocumentInfo();
            CompletionService completionService = new CompletionService(bindingQueue);
            ConnectionInfo connectionInfo = new ConnectionInfo(null, null, null);

            var sqlParserWrapper = new Mock<ISqlParserWrapper>();
            sqlParserWrapper.Setup(x => x.FindCompletions(
                docInfo.ScriptParseInfo.ParseResult,
                docInfo.ParserLine,
                docInfo.ParserColumn,
                It.IsAny<IMetadataDisplayInfoProvider>()))
                .Callback(() =>
                {
                    operationStarted.Set();
                    releaseOperation.WaitOne();
                })
                .Returns(new List<Declaration>());
            completionService.SqlParserWrapper = sqlParserWrapper.Object;

            Task<AutoCompletionResult> completionTask = Task.Run(() => completionService.CreateCompletions(
                connectionInfo,
                docInfo,
                useLowerCaseSuggestions: true));

            try
            {
                Assert.That(operationStarted.WaitOne(TimeSpan.FromSeconds(1)), Is.True);

                bool documentLockTaken = Monitor.TryEnter(docInfo.ScriptParseInfo.BuildingMetadataLock, 500);
                try
                {
                    Assert.That(documentLockTaken, Is.True,
                        "Completion must release the document lock before parser or SMO work begins.");
                }
                finally
                {
                    if (documentLockTaken)
                    {
                        Monitor.Exit(docInfo.ScriptParseInfo.BuildingMetadataLock);
                    }
                }
            }
            finally
            {
                releaseOperation.Set();
                await completionTask;
                bindingQueue.StopQueueProcessor(2_000);
            }
        }

        private ScriptDocumentInfo CreateScriptDocumentInfo()
        {
            TextDocumentPosition doc = new TextDocumentPosition()
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = "script file"
                },
                Position = new Position()
                {
                    Line = 1,
                    Character = 14
                }
            };
            ScriptFile scriptFile = new ScriptFile()
            {
                ClientUri = "script file",
                Contents = "Select * from sys.all_objects"
            };

            ScriptParseInfo scriptParseInfo = new ScriptParseInfo()
            {
                BindingContextKind = BindingContextKindEnum.LiveConnection,
                ParseResult = Parser.IncrementalParse(
                    scriptFile.Contents,
                    null,
                    new ParseOptions(
                        batchSeparator: "GO",
                        isQuotedIdentifierSet: true,
                        compatibilityLevel: DatabaseCompatibilityLevel.Current,
                        transactSqlVersion: TransactSqlVersion.Current))
            };
            ScriptDocumentInfo docInfo = new ScriptDocumentInfo(doc, scriptFile, scriptParseInfo);

            return docInfo;
        }
    }
}
