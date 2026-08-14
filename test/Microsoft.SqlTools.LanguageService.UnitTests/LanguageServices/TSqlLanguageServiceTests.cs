//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
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
        private static TSqlLanguageService CreateLanguageService()
        {
            ILanguageServiceSettings settings = Mock.Of<ILanguageServiceSettings>(s =>
                s.IsLargeScriptOptimizationEnabled == true && s.IsDiagnosticsEnabled == true);
            ILanguageWorkspaceService workspaceService = Mock.Of<ILanguageWorkspaceService>(w => w.CurrentSettings == settings);
            return new TSqlLanguageService
            {
                WorkspaceServiceInstance = workspaceService,
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
    }
}
