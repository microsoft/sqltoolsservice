//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.LanguageService.Connection.Contracts;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using Microsoft.SqlTools.LanguageService.LanguageServices.Completion;
using Microsoft.SqlTools.LanguageService.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.LanguageService.Workspace;
using Microsoft.SqlTools.LanguageService.Workspace.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    /// <summary>
    /// Tests for the ServiceHost Language Service tests
    /// </summary>
    public class LanguageServiceTests
    {
        private sealed class TestLanguageService : TSqlLanguageService
        {
            internal Func<string, ParseResult, ParseOptions, ParseResult> IncrementalParseOverride { get; set; }

            internal Func<ThreadStart, Thread> CreateParseThreadOverride { get; set; }

            internal override ParseResult IncrementalParse(string sqlText, ParseResult previousParseResult, ParseOptions parseOptions)
            {
                return this.IncrementalParseOverride != null
                    ? this.IncrementalParseOverride(sqlText, previousParseResult, parseOptions)
                    : base.IncrementalParse(sqlText, previousParseResult, parseOptions);
            }

            internal override Thread CreateParseThread(ThreadStart threadStart)
            {
                return this.CreateParseThreadOverride != null
                    ? this.CreateParseThreadOverride(threadStart)
                    : base.CreateParseThread(threadStart);
            }
        }

        /// <summary>
        /// Reproduces rapid typing while an older completion ignores cancellation. Only the
        /// latest request for that document runs, while another editor remains independent.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task CompletionCoordinatorRunsOnlyLatestRequestPerDocument()
        {
            var service = new TestLanguageService();
            var firstStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirst = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationToken firstToken = default;
            int secondOperationCount = 0;
            int thirdOperationCount = 0;

            Task<string> first = service.RunLatestCompletionByUriAsync(
                "file://query.sql",
                async cancellationToken =>
                {
                    firstToken = cancellationToken;
                    firstStarted.SetResult(true);
                    await releaseFirst.Task;
                    return "old";
                });
            await firstStarted.Task;

            Task<string> otherDocument = service.RunLatestCompletionByUriAsync(
                "file://other.sql",
                _ => Task.FromResult("other"));
            Assert.That(await otherDocument, Is.EqualTo("other"));
            Assert.That(firstToken.IsCancellationRequested, Is.False);

            Task<string> second = service.RunLatestCompletionByUriAsync(
                "file://query.sql",
                _ =>
                {
                    Interlocked.Increment(ref secondOperationCount);
                    return Task.FromResult("middle");
                });
            Task<string> third = service.RunLatestCompletionByUriAsync(
                "file://query.sql",
                _ =>
                {
                    Interlocked.Increment(ref thirdOperationCount);
                    return Task.FromResult("latest");
                });

            Assert.That(firstToken.IsCancellationRequested, Is.True);
            releaseFirst.SetResult(true);

            Assert.That(await first, Is.Null);
            Assert.That(await second, Is.Null);
            Assert.That(await third, Is.EqualTo("latest"));
            Assert.That(secondOperationCount, Is.Zero);
            Assert.That(thirdOperationCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Test]
        public void ParseSelectStatementWithoutErrors()
        {
            // sql statement with no errors
            const string sqlWithErrors = "SELECT * FROM sys.objects";

            // get the test service
            TSqlLanguageService service = TestObjects.GetTestLanguageService();

            // parse the sql statement
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents(sqlWithErrors);
            ScriptFileMarker[] fileMarkers = service.GetSemanticMarkers(scriptFile).GetAwaiter().GetResult();

            // verify there are no errors
            Assert.AreEqual(0, fileMarkers.Length);
        }

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Test]
        public void ParseSelectStatementWithError()
        {
            // sql statement with errors
            const string sqlWithErrors = "SELECT *** FROM sys.objects";

            // get test service
            TSqlLanguageService service = TestObjects.GetTestLanguageService();

            // parse sql statement
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents(sqlWithErrors);
            ScriptFileMarker[] fileMarkers = service.GetSemanticMarkers(scriptFile).GetAwaiter().GetResult();

            // verify there is one error
            Assert.AreEqual(1, fileMarkers.Length);

            // verify the position of the error
            Assert.AreEqual(9, fileMarkers[0].ScriptRegion.StartColumnNumber);
            Assert.AreEqual(1, fileMarkers[0].ScriptRegion.StartLineNumber);
            Assert.AreEqual(10, fileMarkers[0].ScriptRegion.EndColumnNumber);
            Assert.AreEqual(1, fileMarkers[0].ScriptRegion.EndLineNumber);
        }

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Test]
        public void ParseMultilineSqlWithErrors()
        {
            // multiline sql with errors
            const string sqlWithErrors =
                "SELECT *** FROM sys.objects;\n" +
                "GO\n" +
                "SELECT *** FROM sys.objects;\n";

            // get test service
            TSqlLanguageService service = TestObjects.GetTestLanguageService();

            // parse sql
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents(sqlWithErrors);
            ScriptFileMarker[] fileMarkers = service.GetSemanticMarkers(scriptFile).GetAwaiter().GetResult();

            // verify there are two errors
            Assert.AreEqual(2, fileMarkers.Length);

            // check position of first error
            Assert.AreEqual(9, fileMarkers[0].ScriptRegion.StartColumnNumber);
            Assert.AreEqual(1, fileMarkers[0].ScriptRegion.StartLineNumber);
            Assert.AreEqual(10, fileMarkers[0].ScriptRegion.EndColumnNumber);
            Assert.AreEqual(1, fileMarkers[0].ScriptRegion.EndLineNumber);

            // check position of second error
            Assert.AreEqual(9, fileMarkers[1].ScriptRegion.StartColumnNumber);
            Assert.AreEqual(3, fileMarkers[1].ScriptRegion.StartLineNumber);
            Assert.AreEqual(10, fileMarkers[1].ScriptRegion.EndColumnNumber);
            Assert.AreEqual(3, fileMarkers[1].ScriptRegion.EndLineNumber);
        }

        [Test]
        public async Task ParseAndBindBatchSeparatorChangeInvalidatesParseAndUsesNewSeparator()
        {
            const string customBatchSeparator = "RUN";
            const string sql = "SELECT 1;\r\nRUN\r\nSELECT 2;";
            var service = new TestLanguageService
            {
                WorkspaceServiceInstance = WorkspaceService<SqlToolsSettings>.Instance
            };
            var scriptFile = new ScriptFile(TestObjects.ScriptUri, TestObjects.ScriptUri, sql);
            var defaultParseOptions = new ParseOptions(
                batchSeparator: TSqlLanguageService.DefaultBatchSeperator,
                isQuotedIdentifierSet: true,
                compatibilityLevel: DatabaseCompatibilityLevel.Current,
                transactSqlVersion: TransactSqlVersion.Current);
            var parseInfo = new ScriptParseInfo
            {
                ParseResult = Parser.IncrementalParse(sql, null, defaultParseOptions)
            };
            service.AddOrUpdateScriptParseInfo(scriptFile.ClientUri, parseInfo);
            ParseOptions observedParseOptions = default!;
            ParseResult observedPreviousParseResult = default!;
            service.IncrementalParseOverride = (sqlText, previousParseResult, parseOptions) =>
            {
                observedPreviousParseResult = previousParseResult;
                observedParseOptions = parseOptions;
                return Parser.IncrementalParse(sqlText, previousParseResult, parseOptions);
            };

            Mock<EventContext> eventContext = new();
            await service.HandleDidChangeLanguageFlavorNotification(
                new LanguageFlavorChangeParams
                {
                    Uri = scriptFile.ClientUri,
                    Language = TSqlLanguageService.SQL_CMD_LANG,
                    Flavor = "MSSQL",
                    BatchSeparator = customBatchSeparator
                },
                eventContext.Object);
            var result = await service.ParseAndBind(scriptFile, TestObjects.GetTestConnectionInfo());

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(observedPreviousParseResult, Is.Null, "Changing the separator must force a full document parse.");
                Assert.That(observedParseOptions.BatchSeparator, Is.EqualTo(customBatchSeparator));
                Assert.That(result.Errors, Is.Empty);
            });

            observedParseOptions = default!;
            observedPreviousParseResult = default!;
            await service.HandleDidChangeLanguageFlavorNotification(
                new LanguageFlavorChangeParams
                {
                    Uri = scriptFile.ClientUri,
                    Language = TSqlLanguageService.SQL_CMD_LANG,
                    Flavor = "MSSQL"
                },
                eventContext.Object);
            result = await service.ParseAndBind(scriptFile, TestObjects.GetTestConnectionInfo());

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(observedPreviousParseResult, Is.Null, "Omitting the separator must invalidate a custom-separator parse.");
                Assert.That(observedParseOptions.BatchSeparator, Is.EqualTo(TSqlLanguageService.DefaultBatchSeperator));
            });
        }

        /// <summary>
        /// Verify that GetSignatureHelp returns null when the provided TextDocumentPosition
        /// has no associated ScriptParseInfo.
        /// </summary>
        [Test]
        public void GetSignatureHelpReturnsNullIfParseInfoNotInitialized()
        {
            // Given service doesn't have parseinfo intialized for a document
            const string docContent = "SELECT * FROM sys.objects";
            TSqlLanguageService service = TestObjects.GetTestLanguageService();
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents(docContent);

            // When requesting SignatureHelp
            SignatureHelp signatureHelp = service.GetSignatureHelp(TestObjects.GetTestDocPosition(), scriptFile).GetAwaiter().GetResult();

            // Then null is returned as no parse info can be used to find the signature
            Assert.Null(signatureHelp);
        }

        [Test]
        public void EmptyCompletionListTest()
        {
            Assert.AreEqual(0, AutoCompleteHelper.EmptyCompletionList.Length);
        }

        internal sealed class TestScriptDocumentInfo : ScriptDocumentInfo
        {
            public TestScriptDocumentInfo(TextDocumentPosition textDocumentPosition, ScriptFile scriptFile, ScriptParseInfo scriptParseInfo,
                string tokenText = null)
                : base(textDocumentPosition, scriptFile, scriptParseInfo)
            {
                this.tokenText = string.IsNullOrEmpty(tokenText) ? "doesntmatchanythingintheintellisensedefaultlist" : tokenText;
            }

            private string tokenText;

            public override string TokenText
            {
                get
                {
                    return this.tokenText;
                }
            }
        }

        [Test]
        public void GetDefaultCompletionListWithNoMatchesTest()
        {
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents("koko wants a bananas");

            ScriptParseInfo scriptInfo = new ScriptParseInfo { BindingContextKind = BindingContextKindEnum.None };

            var scriptDocumentInfo = new TestScriptDocumentInfo(
                new TextDocumentPosition()
                {
                    TextDocument = new TextDocumentIdentifier() { Uri = TestObjects.ScriptUri },
                    Position = new Position() { Line = 0, Character = 0 }
                }, scriptFile, scriptInfo);

            AutoCompleteHelper.GetDefaultCompletionItems(scriptDocumentInfo, false);
        }

        [Test]
        public void GetDefaultCompletionListWithMatchesTest()
        {
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents("koko wants a bananas");

            ScriptParseInfo scriptInfo = new ScriptParseInfo { BindingContextKind = BindingContextKindEnum.None };

            var scriptDocumentInfo = new TestScriptDocumentInfo(
                new TextDocumentPosition()
                {
                    TextDocument = new TextDocumentIdentifier() { Uri = TestObjects.ScriptUri },
                    Position = new Position() { Line = 0, Character = 0 }
                }, scriptFile, scriptInfo, "all");

            CompletionItem[] result = AutoCompleteHelper.GetDefaultCompletionItems(scriptDocumentInfo, false);
            Assert.AreEqual(1, result.Length);
        }

        [Test]
        public async Task ParseAndBindConnectedPathUsesDedicatedParseThreadAndClearsFailedParseState()
        {
            TestLanguageService service = new TestLanguageService();
            ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue(false);
            service.BindingQueue = bindingQueue;

            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents("SELECT 1");

            var parseOptions = new ParseOptions(
                batchSeparator: TSqlLanguageService.DefaultBatchSeperator,
                isQuotedIdentifierSet: true,
                compatibilityLevel: DatabaseCompatibilityLevel.Current,
                transactSqlVersion: TransactSqlVersion.Current);

            ScriptParseInfo scriptParseInfo = new ScriptParseInfo
            {
                BindingContextKind = BindingContextKindEnum.LiveConnection,
                ConnectionKey = "test-connection-key",
                ParseResult = Parser.IncrementalParse("SELECT 1", null, parseOptions)
            };

            service.AddOrUpdateScriptParseInfo(scriptFile.ClientUri, scriptParseInfo);

            ConnectedBindingContext bindingContext = new ConnectedBindingContext
            {
                IsConnected = false
            };

            bindingQueue.BindingContextMap.TryAdd(scriptParseInfo.ConnectionKey, bindingContext);
            bindingQueue.BindingContextTasks.TryAdd(bindingContext, Task.FromResult(0));

            int callingThreadId = Environment.CurrentManagedThreadId;
            int parserThreadId = callingThreadId;
            bool dedicatedThreadCreated = false;

            service.CreateParseThreadOverride = threadStart =>
            {
                dedicatedThreadCreated = true;
                return new Thread(threadStart);
            };

            service.IncrementalParseOverride = (sqlText, previousParseResult, options) =>
            {
                parserThreadId = Environment.CurrentManagedThreadId;
                throw new InvalidOperationException("parser fault");
            };

            try
            {
                ParseResult parseResult = await service.ParseAndBind(scriptFile, TestObjects.GetTestConnectionInfo());

                Assert.IsNull(parseResult);
                Assert.IsNull(scriptParseInfo.ParseResult);
                Assert.IsTrue(dedicatedThreadCreated);
                Assert.AreNotEqual(callingThreadId, parserThreadId);
            }
            finally
            {
                bindingQueue.StopQueueProcessor(1000);
                bindingQueue.Dispose();
            }
        }

        [Test]
        public async Task ParseAndBindConnectedPathClearsParseStateWhenParserReturnsNull()
        {
            TestLanguageService service = new TestLanguageService();
            ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue(false);
            service.BindingQueue = bindingQueue;

            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents("SELECT 1");

            var parseOptions = new ParseOptions(
                batchSeparator: TSqlLanguageService.DefaultBatchSeperator,
                isQuotedIdentifierSet: true,
                compatibilityLevel: DatabaseCompatibilityLevel.Current,
                transactSqlVersion: TransactSqlVersion.Current);

            ScriptParseInfo scriptParseInfo = new ScriptParseInfo
            {
                BindingContextKind = BindingContextKindEnum.LiveConnection,
                ConnectionKey = "test-connection-key",
                ParseResult = Parser.IncrementalParse("SELECT 1", null, parseOptions)
            };

            service.AddOrUpdateScriptParseInfo(scriptFile.ClientUri, scriptParseInfo);

            ConnectedBindingContext bindingContext = new ConnectedBindingContext
            {
                IsConnected = false
            };

            bindingQueue.BindingContextMap.TryAdd(scriptParseInfo.ConnectionKey, bindingContext);
            bindingQueue.BindingContextTasks.TryAdd(bindingContext, Task.FromResult(0));

            bool dedicatedThreadCreated = false;

            service.CreateParseThreadOverride = threadStart =>
            {
                dedicatedThreadCreated = true;
                return new Thread(threadStart);
            };

            service.IncrementalParseOverride = (sqlText, previousParseResult, options) => null;

            try
            {
                ParseResult parseResult = await service.ParseAndBind(scriptFile, TestObjects.GetTestConnectionInfo());

                Assert.IsNull(parseResult);
                Assert.IsNull(scriptParseInfo.ParseResult);
                Assert.IsTrue(dedicatedThreadCreated);
            }
            finally
            {
                bindingQueue.StopQueueProcessor(1000);
                bindingQueue.Dispose();
            }
        }

        /// <summary>
        /// A parser runs on its required large-stack thread, but waiting for that thread must not
        /// occupy the binding queue's only scheduler slot. The old Thread.Join implementation
        /// held that slot and prevented even an unrelated binding context from dispatching.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task ParseThreadWaitDoesNotStallUnrelatedBindingContext()
        {
            TestLanguageService service = new TestLanguageService();
            var schedulerPair = new ConcurrentExclusiveSchedulerPair(
                TaskScheduler.Default,
                maxConcurrencyLevel: 1);
            ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue(false)
            {
                DispatchScheduler = schedulerPair.ConcurrentScheduler,
            };
            service.BindingQueue = bindingQueue;

            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents("SELECT 1");
            var parseOptions = new ParseOptions(
                batchSeparator: TSqlLanguageService.DefaultBatchSeperator,
                isQuotedIdentifierSet: true,
                compatibilityLevel: DatabaseCompatibilityLevel.Current,
                transactSqlVersion: TransactSqlVersion.Current);
            var parseInfo = new ScriptParseInfo
            {
                BindingContextKind = BindingContextKindEnum.LiveConnection,
                ConnectionKey = "slow-parser-context",
            };
            service.AddOrUpdateScriptParseInfo(scriptFile.ClientUri, parseInfo);

            var parserContext = new ConnectedBindingContext { IsConnected = false };
            bindingQueue.BindingContextMap.TryAdd(parseInfo.ConnectionKey, parserContext);
            bindingQueue.BindingContextTasks.TryAdd(parserContext, Task.CompletedTask);

            using var parserStarted = new ManualResetEventSlim(false);
            using var releaseParser = new ManualResetEventSlim(false);
            service.IncrementalParseOverride = (sqlText, previousParseResult, options) =>
            {
                parserStarted.Set();
                releaseParser.Wait();
                return Parser.IncrementalParse(sqlText, previousParseResult, options);
            };

            Task<ParseResult> parseTask = service.ParseAndBind(
                scriptFile,
                TestObjects.GetTestConnectionInfo());

            try
            {
                Assert.That(parserStarted.Wait(TimeSpan.FromSeconds(2)), Is.True);

                QueueItem unrelated = bindingQueue.QueueBindingOperation(
                    "unrelated-parser-context",
                    (context, token) => null,
                    waitForLockTimeout: 1_000,
                    hardTimeout: 1_000);

                Assert.That(
                    await unrelated.WaitForCompletionAsync(queueWaitBudgetMs: 1_000),
                    Is.True,
                    "the parser wait must yield the queue's only scheduler slot");
                Assert.That(unrelated.WasExecuted, Is.True);
            }
            finally
            {
                releaseParser.Set();
                await parseTask.WaitAsync(TimeSpan.FromSeconds(2));
                bindingQueue.StopQueueProcessor(2_000);
                bindingQueue.Dispose();
                schedulerPair.Complete();
            }
        }

        [Test]
        [Timeout(10_000)]
        public async Task TimedOutParserDoesNotPublishLateResult()
        {
            var service = new TestLanguageService();
            var bindingQueue = new ConnectedBindingQueue(false);
            service.BindingQueue = bindingQueue;

            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents("SELECT 2");
            var parseOptions = new ParseOptions(
                batchSeparator: TSqlLanguageService.DefaultBatchSeperator,
                isQuotedIdentifierSet: true,
                compatibilityLevel: DatabaseCompatibilityLevel.Current,
                transactSqlVersion: TransactSqlVersion.Current);
            ParseResult oldResult = Parser.IncrementalParse("SELECT 1", null, parseOptions);
            var parseInfo = new ScriptParseInfo
            {
                BindingContextKind = BindingContextKindEnum.LiveConnection,
                ConnectionKey = "timed-out-parser-context",
                ParseResult = oldResult,
            };
            service.AddOrUpdateScriptParseInfo(scriptFile.ClientUri, parseInfo);

            var bindingContext = new ConnectedBindingContext { IsConnected = false };
            bindingQueue.BindingContextMap.TryAdd(parseInfo.ConnectionKey, bindingContext);
            bindingQueue.BindingContextTasks.TryAdd(bindingContext, Task.CompletedTask);

            using var parserStarted = new ManualResetEventSlim(false);
            using var releaseParser = new ManualResetEventSlim(false);
            service.IncrementalParseOverride = (sqlText, previousParseResult, options) =>
            {
                parserStarted.Set();
                releaseParser.Wait();
                return Parser.IncrementalParse(sqlText, previousParseResult, options);
            };

            try
            {
                Task<ParseResult> parseTask = service.ParseAndBind(
                    scriptFile,
                    TestObjects.GetTestConnectionInfo());
                Assert.That(parserStarted.Wait(TimeSpan.FromSeconds(2)), Is.True);
                Assert.That(await parseTask.WaitAsync(TimeSpan.FromSeconds(2)), Is.Null,
                    "the binding queue must return at its hard timeout");

                releaseParser.Set();
                Assert.That(bindingContext.BindingLock.WaitOne(TimeSpan.FromSeconds(2)), Is.True,
                    "the abandoned parser callback must finish and release its context");
                Assert.That(parseInfo.ParseResult, Is.SameAs(oldResult),
                    "a parser that finishes after cancellation must not overwrite newer document state");
            }
            finally
            {
                releaseParser.Set();
                bindingContext.BindingLock.Set();
                bindingQueue.StopQueueProcessor(2_000);
                bindingQueue.Dispose();
            }
        }

        [Test]
        [Timeout(10_000)]
        public async Task SyntaxOnlyParserWaitIsBounded()
        {
            var service = new TestLanguageService { ParserTimeout = 100 };
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents("SELECT 1");
            using var parserStarted = new ManualResetEventSlim(false);
            using var releaseParser = new ManualResetEventSlim(false);

            service.IncrementalParseOverride = (sqlText, previousParseResult, options) =>
            {
                parserStarted.Set();
                releaseParser.Wait();
                return Parser.IncrementalParse(sqlText, previousParseResult, options);
            };

            try
            {
                Task<ParseResult> parseTask = service.ParseAndBind(scriptFile, connInfo: null);
                Assert.That(parserStarted.Wait(TimeSpan.FromSeconds(2)), Is.True);
                Assert.That(await parseTask.WaitAsync(TimeSpan.FromSeconds(2)), Is.Null,
                    "a syntax-only parser thread must not hold its caller indefinitely");
            }
            finally
            {
                releaseParser.Set();
                service.Dispose();
            }
        }

        [Test]
        [Timeout(10_000)]
        public async Task ParserThreadConcurrencyIsBounded()
        {
            var service = new TestLanguageService
            {
                ParserTimeout = 150,
                ParserThreadThrottle = new SemaphoreSlim(1, 1),
            };
            var firstFile = new ScriptFile { ClientUri = "file:///first.sql" };
            firstFile.SetFileContents("SELECT 1");
            var secondFile = new ScriptFile { ClientUri = "file:///second.sql" };
            secondFile.SetFileContents("SELECT 2");
            using var parserStarted = new ManualResetEventSlim(false);
            using var releaseParser = new ManualResetEventSlim(false);
            int parserStarts = 0;

            service.IncrementalParseOverride = (sqlText, previousParseResult, options) =>
            {
                Interlocked.Increment(ref parserStarts);
                parserStarted.Set();
                releaseParser.Wait();
                return Parser.IncrementalParse(sqlText, previousParseResult, options);
            };

            try
            {
                Task<ParseResult> first = service.ParseAndBind(firstFile, connInfo: null);
                Assert.That(parserStarted.Wait(TimeSpan.FromSeconds(2)), Is.True);

                Task<ParseResult> second = service.ParseAndBind(secondFile, connInfo: null);
                Assert.That(await second.WaitAsync(TimeSpan.FromSeconds(2)), Is.Null);
                Assert.That(Volatile.Read(ref parserStarts), Is.EqualTo(1),
                    "a timed-out parser keeps its slot until its dedicated thread actually exits");

                releaseParser.Set();
                Assert.That(await first.WaitAsync(TimeSpan.FromSeconds(2)), Is.Null);
            }
            finally
            {
                releaseParser.Set();
                service.Dispose();
            }
        }

        /// <summary>
        /// Verifies that a completion request does not consume the previous document's parse
        /// result when its required incremental parse cannot acquire the binding lock. The busy
        /// request must stop without running the parser or replacing the stored parse state. Once
        /// the lock is available, the next request must detect the text mismatch, retry parsing,
        /// and update the stored result to the latest document text. This models the #21930
        /// overlap where a slow completion owned the binding lock while a newer parse arrived.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task CompletionStopsWhenRequiredReparseCannotGetBindingLock()
        {
            const string oldSql = "SELECT OldColumn";
            const string currentSql = "SELECT CurrentColumn";
            const string connectionKey = "stale-parse-test-connection";

            var service = new TestLanguageService();
            var bindingQueue = new ConnectedBindingQueue(false);
            service.BindingQueue = bindingQueue;
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings = new SqlToolsSettings();
            service.WorkspaceServiceInstance = WorkspaceService<SqlToolsSettings>.Instance;

            var scriptFile = new ScriptFile(TestObjects.ScriptUri, TestObjects.ScriptUri, currentSql);
            var parseOptions = new ParseOptions(
                batchSeparator: TSqlLanguageService.DefaultBatchSeperator,
                isQuotedIdentifierSet: true,
                compatibilityLevel: DatabaseCompatibilityLevel.Current,
                transactSqlVersion: TransactSqlVersion.Current);
            ParseResult oldParseResult = Parser.IncrementalParse(oldSql, null, parseOptions);
            var scriptParseInfo = new ScriptParseInfo
            {
                BindingContextKind = BindingContextKindEnum.LiveConnection,
                ConnectionKey = connectionKey,
                ParseResult = oldParseResult
            };
            service.AddOrUpdateScriptParseInfo(scriptFile.ClientUri, scriptParseInfo);

            var bindingContext = new ConnectedBindingContext { IsConnected = false };
            bindingContext.BindingLock.Reset();
            bindingQueue.BindingContextMap.TryAdd(connectionKey, bindingContext);
            bindingQueue.BindingContextTasks.TryAdd(bindingContext, Task.CompletedTask);

            int incrementalParseCount = 0;
            service.IncrementalParseOverride = (sqlText, previousParseResult, options) =>
            {
                Interlocked.Increment(ref incrementalParseCount);
                return Parser.IncrementalParse(sqlText, previousParseResult, options);
            };

            var position = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = scriptFile.ClientUri },
                Position = new Position { Line = 0, Character = currentSql.Length }
            };

            try
            {
                ParseResult publicParseResult = await service.ParseAndBind(
                    scriptFile,
                    TestObjects.GetTestConnectionInfo());

                Assert.That(publicParseResult, Is.Null, "NotExecuted must not be exposed as the previous parse result.");
                Assert.That(scriptParseInfo.ParseResult, Is.SameAs(oldParseResult));

                CompletionItem[] busyResult = await service.GetCompletionItems(
                    position,
                    scriptFile,
                    TestObjects.GetTestConnectionInfo());

                Assert.That(busyResult, Is.Null, "A busy reparse must stop the current completion request.");
                Assert.That(incrementalParseCount, Is.Zero, "The timed-out queue item must not run later.");
                Assert.That(scriptParseInfo.ParseResult, Is.SameAs(oldParseResult));
                Assert.That(scriptParseInfo.ParseResult.Script.Sql, Is.EqualTo(oldSql));

                bindingContext.BindingLock.Set();

                await service.GetCompletionItems(
                    position,
                    scriptFile,
                    TestObjects.GetTestConnectionInfo());

                Assert.That(incrementalParseCount, Is.EqualTo(1));
                Assert.That(scriptParseInfo.ParseResult, Is.Not.Null);
                Assert.That(scriptParseInfo.ParseResult.Script.Sql, Is.EqualTo(currentSql));
            }
            finally
            {
                bindingContext.BindingLock.Set();
                bindingQueue.StopQueueProcessor(2_000);
                service.Dispose();
            }
        }
    }
}
