//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using NUnit.Framework;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Location = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Location;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    /// <summary>
    /// Tests for the language service autocomplete component
    /// </summary>
    public class AutocompleteTests : LanguageServiceTestBase<CompletionItem>
    {

        [Test]
        public void HandleCompletionRequestDisabled()
        {
            InitializeTestObjects();
            langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = false;
            Assert.NotNull(langService.HandleCompletionRequest(null, null));
        }

        [Test]
        public void HandleCompletionResolveRequestDisabled()
        {
            InitializeTestObjects();
            langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = false;
            Assert.NotNull(langService.HandleCompletionResolveRequest(null, null));
        }

        [Test]
        public void HandleSignatureHelpRequestDisabled()
        {
            InitializeTestObjects();
            langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = false;
            Assert.NotNull(langService.HandleSignatureHelpRequest(null, null));
        }

        [Test]
        public async Task HandleSignatureHelpRequestNonMssqlFile()
        {
            InitializeTestObjects();

            // setup the mock for SendResult
            var signatureRequestContext = new Mock<RequestContext<SignatureHelp>>();
            SignatureHelp result = null;
            signatureRequestContext.Setup(rc => rc.SendResult(It.IsAny<SignatureHelp>()))
            .Returns<SignatureHelp>((signature) =>
            {
                result = signature;
                return Task.FromResult(0);
            });
            signatureRequestContext.Setup(rc => rc.SendError(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.FromResult(0));


            langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = true;
            await langService.HandleDidChangeLanguageFlavorNotification(new LanguageFlavorChangeParams
            {
                Uri = textDocument.TextDocument.Uri,
                Language = LanguageService.SQL_LANG.ToLower(),
                Flavor = "NotMSSQL"
            }, null);
            await langService.HandleSignatureHelpRequest(textDocument, signatureRequestContext.Object);
            // verify that the response was sent with a null response value
            signatureRequestContext.Verify(m => m.SendResult(It.IsAny<SignatureHelp>()), Times.Once());
            Assert.Null(result);
            signatureRequestContext.Verify(m => m.SendError(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void AddOrUpdateScriptParseInfoNullUri()
        {
            InitializeTestObjects();
            langService.AddOrUpdateScriptParseInfo("abracadabra", scriptParseInfo);
            Assert.True(langService.ScriptParseInfoMap.ContainsKey("abracadabra"));
        }

        [Test]
        public async Task HandleDefinitionRequest_InvalidTextDocument_SendsEmptyListResponse()
        {
            InitializeTestObjects();
            textDocument.TextDocument.Uri = "invaliduri";

            // setup the mock for SendResult
            var definitionRequestContext = new Mock<RequestContext<Location[]>>();
            Location[] result = null;
            definitionRequestContext.Setup(rc => rc.SendResult(It.IsAny<Location[]>()))
            .Returns<Location[]>((resultDetails) =>
            {
                result = resultDetails;
                return Task.FromResult(0);
            });

            await langService.HandleDefinitionRequest(textDocument, definitionRequestContext.Object);
            // Should get an empty array when passed
            Assert.NotNull(result);
            Assert.True(result.Length == 0, $"Unexpected values passed to SendResult : [{string.Join(",", (object[])result)}]");
        }

        [Test]
        public void RemoveScriptParseInfoNullUri()
        {
            InitializeTestObjects();
            Assert.False(langService.RemoveScriptParseInfo("abc123"));
        }

        [Test]
        public void IsPreviewWindowNullScriptFileTest()
        {
            InitializeTestObjects();
            Assert.False(langService.IsPreviewWindow(null));
        }

        [Test]
        public async Task HandleCompletionRequest_InvalidTextDocument_SendsNullResult()
        {
            InitializeTestObjects();
            // setup the mock for SendResult to capture the items
            CompletionItem[] completionItems = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<CompletionItem[]>()))
                .Returns<CompletionItem[]>((resultDetails) =>
                {
                    completionItems = resultDetails;
                    return Task.FromResult(0);
                });

            textDocument.TextDocument.Uri = "somethinggoeshere";
            await langService.HandleCompletionRequest(textDocument, requestContext.Object);
            requestContext.Verify(m => m.SendResult(It.IsAny<CompletionItem[]>()), Times.Once());
            Assert.Null(completionItems);
        }

        [Test]
        public void GetDiagnosticFromMarkerTest()
        {
            var scriptFileMarker = new ScriptFileMarker()
            {
                Message = "Message",
                Level = ScriptFileMarkerLevel.Error,
                ScriptRegion = new ScriptRegion()
                {
                    File = "file://nofile.sql",
                    StartLineNumber = 1,
                    StartColumnNumber = 1,
                    StartOffset = 0,
                    EndLineNumber = 1,
                    EndColumnNumber = 1,
                    EndOffset = 0
                }
            };
            var diagnostic = DiagnosticsHelper.GetDiagnosticFromMarker(scriptFileMarker);
            Assert.AreEqual(diagnostic.Message, scriptFileMarker.Message);
        }

        [Test]
        public void MapDiagnosticSeverityTest()
        {
            var level = ScriptFileMarkerLevel.Error;
            Assert.AreEqual(DiagnosticSeverity.Error, DiagnosticsHelper.MapDiagnosticSeverity(level));
            level = ScriptFileMarkerLevel.Warning;
            Assert.AreEqual(DiagnosticSeverity.Warning, DiagnosticsHelper.MapDiagnosticSeverity(level));
            level = ScriptFileMarkerLevel.Information;
            Assert.AreEqual(DiagnosticSeverity.Information, DiagnosticsHelper.MapDiagnosticSeverity(level));
            level = (ScriptFileMarkerLevel)100;
            Assert.AreEqual(DiagnosticSeverity.Error, DiagnosticsHelper.MapDiagnosticSeverity(level));
        }

        /// <summary>
        /// Tests the primary completion list event handler
        /// </summary>
        [Test]
        public void GetCompletionsHandlerTest()
        {
            InitializeTestObjects();

            // request the completion list            
            Task handleCompletion = langService.HandleCompletionRequest(textDocument, requestContext.Object);
            handleCompletion.Wait(TaskTimeout);

            // verify that send result was called with a completion array
            requestContext.Verify(m => m.SendResult(It.IsAny<CompletionItem[]>()), Times.Once());
        }

        public ScriptDocumentInfo CreateSqlStarTestFile(string sqlText, int startLine, int startColumn)
        {
            var uri = "file://nofile.sql";

            var textDocumentPosition = new TextDocumentPosition()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = uri
                },
                Position = new Position()
                {
                    Line = startLine,
                    Character = startColumn
                }
            };

            var scriptFile = new ScriptFile()
            {
                ClientUri = uri,
                Contents = sqlText
            };


            ParseResult parseResult = langService.ParseAndBind(scriptFile, null);
            ScriptParseInfo scriptParseInfo = langService.GetScriptParseInfo(scriptFile.ClientUri, true);

            return new ScriptDocumentInfo(textDocumentPosition, scriptFile, scriptParseInfo);
        }

        [Test]
        //complete select query with the cursor at * should return a sqlselectstarexpression object.
        [TestCase("select * from sys.all_objects", 0, 8, "SelectStarExpression is not returned on complete select query with star")]
        //incomplete select query with the cursor at * should sqlselectstarexpression
        [TestCase("select * ", 0, 8, "SelectStarExpression is returned on an incomplete select query with star")]
        //method should return sqlselectstarexpression on *s with object identifiers.
        [TestCase("select a.* from sys.all_objects as a", 0, 10, "SelectStarExpression returned on star expression with object identifier")]
        public void TryGetSqlSelectStarStatementNotNullTests(string sqlQuery, int cursorLine, int cursorColumn, string errorValidationMessage)
        {
            InitializeTestObjects();
            var testFile = CreateSqlStarTestFile(sqlQuery, cursorLine, cursorColumn);
            Assert.NotNull(AutoCompleteHelper.TryGetSelectStarStatement(testFile.ScriptParseInfo.ParseResult.Script, testFile), errorValidationMessage);
        }


        [Test]
        //complete select query with the cursor not at * should return null.
        [TestCase("select * from sys.all_objects", 0, 0, "null is not returned when the cursor is not at a star expression")]
        //file with no text should return null
        [TestCase("", 0, 0, "null is not returned on file with empty sql text")]
        //file with out of bounds cursor position should return null
        [TestCase("select * from sys.all_objects", 0, 100, "null is not returned when the cursor is out of bounds.")]
        public void TryGetSqlSelectStarStatementNullTests(string sqlQuery, int cursorLine, int cursorColumn, string errorValidationMessage)
        {
            InitializeTestObjects();
            var testFile = CreateSqlStarTestFile(sqlQuery, cursorLine, cursorColumn);
            Assert.Null(AutoCompleteHelper.TryGetSelectStarStatement(testFile.ScriptParseInfo.ParseResult.Script, testFile), errorValidationMessage);
        }

        [Test]
        public void TryGetSqlSelectStarStatementNullFileTest()
        {
            Assert.Null(AutoCompleteHelper.TryGetSelectStarStatement(null, null), "null is not returned on null file");
        }

        [Test]
        [TestCase("select a.*, * from sys.all_objects as a CROSS JOIN sys.databases", 0, 10, "a.*")]
        [TestCase("select a.*, * from sys.all_objects as a CROSS JOIN sys.databases", 0, 13, "*")]
        public void TryGetSqlSelectStarStatmentMulitpleStarExpressionsTest(string sqlQuery, int cursorLine, int cursorColumn, string expectedStarExpressionSqlText)
        {
            InitializeTestObjects();
            var testFile = CreateSqlStarTestFile(sqlQuery, cursorLine, cursorColumn);
            var starExpressionTest = AutoCompleteHelper.TryGetSelectStarStatement(testFile.ScriptParseInfo.ParseResult.Script, testFile).Sql;
            Assert.AreEqual(expectedStarExpressionSqlText, expectedStarExpressionSqlText, string.Format("correct SelectStarExpression is not returned."));
        }
    }
}
