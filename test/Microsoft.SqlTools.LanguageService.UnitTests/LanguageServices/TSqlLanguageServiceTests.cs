//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.LanguageService.Connection.Contracts;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using Microsoft.SqlTools.LanguageService.LanguageServices.Contracts;
using Microsoft.SqlTools.LanguageService.Workspace;
using Microsoft.SqlTools.LanguageService.Workspace.Contracts;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.LanguageService.UnitTests.LanguageServices
{
    [TestFixture]
    public class TSqlLanguageServiceTests
    {
        private static TSqlLanguageService CreateLanguageService(bool diagnosticsEnabled = true)
        {
            ILanguageServiceSettings settings = Mock.Of<ILanguageServiceSettings>(s =>
                s.IsLargeScriptOptimizationEnabled == true && s.IsDiagnosticsEnabled == diagnosticsEnabled);
            global::Microsoft.SqlTools.LanguageService.Workspace.Workspace workspace = new();
            ILanguageWorkspaceService workspaceService = Mock.Of<ILanguageWorkspaceService>(w =>
                w.CurrentSettings == settings && w.Workspace == workspace);
            return new TSqlLanguageService
            {
                WorkspaceServiceInstance = workspaceService,
                ServiceHostInstance = Mock.Of<ILanguageServiceHost>(host => host.ProviderName == "MSSQL"),
            };
        }

        private static TextDocumentPosition PositionAtStart(string uri) =>
            new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = new Position { Line = 0, Character = 0 },
            };

        /// <summary>
        /// Tests for the large-script fast path in <see cref="TSqlLanguageService.GetCompletionItems"/>.
        /// </summary>
        [Test]
        public async Task GetCompletionItems_LargeScriptNeedingReparse_ReturnsDefaultItemsWithoutParsing()
        {
            using TSqlLanguageService service = CreateLanguageService();

            const string uri = "large-script.sql";

            // Exceed the large-script threshold so the completion fast path is taken.
            string largeContents = new string('a', TSqlLanguageService.LargeScriptCompletionThresholdChars + 1);
            ScriptFile scriptFile = new ScriptFile { ClientUri = uri, Contents = largeContents };

            // A ScriptParseInfo with a null ParseResult makes RequiresReparse return true, so the method reaches the
            // large-script branch.
            ScriptParseInfo scriptParseInfo = new ScriptParseInfo();
            service.ScriptParseInfoMap[uri] = scriptParseInfo;

            CompletionItem[] items = await service.GetCompletionItems(PositionAtStart(uri), scriptFile, connInfo: null);

            Assert.That(items, Is.Not.Null,
                "The large-script fast path should return the default completion list, not null.");
            Assert.That(items.Length, Is.GreaterThan(0),
                "The default completion list should contain keyword suggestions.");
            Assert.That(scriptParseInfo.ParseResult, Is.Null,
                "The large-script fast path must not parse inline, so the cached ParseResult should remain null.");
        }

        [TestCase("SELECT (1)", 0, 7, 0, 7, 0, 8, 0, 9, 0, 10, TestName = "GetMatchingPairAsync_Parenthesis_ReturnsTokenRanges")]
        [TestCase("BEGIN\r\nSELECT 1\r\nEND", 0, 0, 0, 0, 0, 5, 2, 0, 2, 3, TestName = "GetMatchingPairAsync_BeginEnd_ReturnsTokenRanges")]
        public async Task GetMatchingPairAsync_MatchedToken_ReturnsTokenRanges(
            string contents,
            int requestLine,
            int requestCharacter,
            int expectedLeftStartLine,
            int expectedLeftStartCharacter,
            int expectedLeftEndLine,
            int expectedLeftEndCharacter,
            int expectedRightStartLine,
            int expectedRightStartCharacter,
            int expectedRightEndLine,
            int expectedRightEndCharacter)
        {
            using TSqlLanguageService service = CreateLanguageService();
            const string uri = "untitled:matching-pair.sql";
            service.CurrentWorkspace.GetFileBuffer(uri, contents);

            MatchingPairResult result = await service.GetMatchingPairAsync(
                uri,
                new Position { Line = requestLine, Character = requestCharacter });

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "A parser-recognized pair should be returned.");
                Assert.That(result.LeftRange.Start, Is.EqualTo(new Position { Line = expectedLeftStartLine, Character = expectedLeftStartCharacter }), "The left start should match.");
                Assert.That(result.LeftRange.End, Is.EqualTo(new Position { Line = expectedLeftEndLine, Character = expectedLeftEndCharacter }), "The left end should match.");
                Assert.That(result.RightRange.Start, Is.EqualTo(new Position { Line = expectedRightStartLine, Character = expectedRightStartCharacter }), "The right start should match.");
                Assert.That(result.RightRange.End, Is.EqualTo(new Position { Line = expectedRightEndLine, Character = expectedRightEndCharacter }), "The right end should match.");
            });
        }

        [Test]
        public async Task GetMatchingPairAsync_UnchangedDocument_ReusesSharedParseResult()
        {
            using TSqlLanguageService service = CreateLanguageService();
            const string uri = "untitled:cached-matching-pair.sql";
            service.CurrentWorkspace.GetFileBuffer(uri, "SELECT (1)");

            _ = await service.GetMatchingPairAsync(uri, new Position { Line = 0, Character = 7 });
            ScriptParseInfo parseInfo = service.ScriptParseInfoMap[uri];
            ParseResult firstParseResult = parseInfo.ParseResult;
            _ = await service.GetMatchingPairAsync(uri, new Position { Line = 0, Character = 9 });

            Assert.That(parseInfo.ParseResult, Is.SameAs(firstParseResult), "Repeated pair requests should reuse the shared parse result.");
        }

        [Test]
        public async Task GetMatchingPairAsync_CurrentParseResult_ReusesSharedParseResult()
        {
            using TSqlLanguageService service = CreateLanguageService();
            const string uri = "untitled:bound-matching-pair.sql";
            const string contents = "SELECT (1)";
            service.CurrentWorkspace.GetFileBuffer(uri, contents);
            ParseResult currentParseResult = Parser.Parse(contents, new ParseOptions("GO"));
            ScriptParseInfo parseInfo = new ScriptParseInfo
            {
                ParseResult = currentParseResult,
            };
            service.ScriptParseInfoMap[uri] = parseInfo;

            MatchingPairResult result = await service.GetMatchingPairAsync(
                uri,
                new Position { Line = 0, Character = 7 });

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null, "The current parse should provide the matching pair.");
                Assert.That(parseInfo.ParseResult, Is.SameAs(currentParseResult), "Brace matching should reuse the shared parse result.");
            });
        }

        [Test]
        public async Task GetMatchingPairAsync_CustomBatchSeparator_DoesNotMatchAcrossBatches()
        {
            using TSqlLanguageService service = CreateLanguageService(diagnosticsEnabled: false);
            const string uri = "untitled:custom-separator-matching-pair.sql";
            service.CurrentWorkspace.GetFileBuffer(uri, "BEGIN\r\nSELECT 1\r\nRUN\r\nEND");

            MatchingPairResult initialResult = await service.GetMatchingPairAsync(
                uri,
                new Position { Line = 0, Character = 0 });
            ScriptParseInfo parseInfo = service.ScriptParseInfoMap[uri];
            ParseResult initialParseResult = parseInfo.ParseResult;

            await SetBatchSeparatorAsync(service, uri, "RUN");

            MatchingPairResult result = await service.GetMatchingPairAsync(
                uri,
                new Position { Line = 0, Character = 0 });
            ParseResult customSeparatorParseResult = parseInfo.ParseResult;

            await SetBatchSeparatorAsync(service, uri, "GO");
            MatchingPairResult defaultResult = await service.GetMatchingPairAsync(
                uri,
                new Position { Line = 0, Character = 0 });

            Assert.Multiple(() =>
            {
                Assert.That(initialResult, Is.Not.Null, "RUN should not split batches while GO is configured.");
                Assert.That(result, Is.Null, "Tokens in separate caller-configured batches should not be matched.");
                Assert.That(
                    customSeparatorParseResult,
                    Is.Not.SameAs(initialParseResult),
                    "Changing the document batch separator should invalidate and replace the shared parse result.");
                Assert.That(defaultResult, Is.Not.Null, "RUN should stop splitting batches after restoring GO.");
                Assert.That(parseInfo.ParseResult, Is.Not.SameAs(customSeparatorParseResult), "Restoring GO should invalidate and replace the RUN parse result.");
            });
        }

        private static Task SetBatchSeparatorAsync(TSqlLanguageService service, string uri, string batchSeparator) =>
            service.HandleDidChangeLanguageFlavorNotification(
                new LanguageFlavorChangeParams
                {
                    Uri = uri,
                    Language = TSqlLanguageService.SQL_LANG,
                    Flavor = "MSSQL",
                    BatchSeparator = batchSeparator,
                },
                eventContext: null);

    }
}
