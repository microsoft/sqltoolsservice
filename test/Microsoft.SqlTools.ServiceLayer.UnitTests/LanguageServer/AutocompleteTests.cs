//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
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
            Assert.NotNull(langService.HandleCompletionRequest(textDocument));
        }

        [Test]
        public void HandleCompletionResolveRequestDisabled()
        {
            InitializeTestObjects();
            langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = false;
            Assert.NotNull(langService.HandleCompletionResolveRequest(null));
        }

        [Test]
        public void HandleSignatureHelpRequestDisabled()
        {
            InitializeTestObjects();
            langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = false;
            Assert.NotNull(langService.HandleSignatureHelpRequest(textDocument));
        }

        [Test]
        public async Task HandleSignatureHelpRequestNonMssqlFile()
        {
            InitializeTestObjects();

            langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = true;
            await langService.HandleDidChangeLanguageFlavorNotification(new LanguageFlavorChangeParams
            {
                Uri = textDocument.TextDocument.Uri,
                Language = LanguageService.SQL_LANG.ToLower(System.Globalization.CultureInfo.InvariantCulture),
                Flavor = "NotMSSQL"
            });
            SignatureHelp result = await langService.HandleSignatureHelpRequest(textDocument);
            // verify that the response was sent with a null response value
            Assert.Null(result);
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

            Location[] result = await langService.HandleDefinitionRequest(textDocument);
            // Should get an empty array when passed
            Assert.NotNull(result);
            Assert.True(result.Length == 0, $"Unexpected handler result values: [{string.Join(",", (object[])result)}]");
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

            textDocument.TextDocument.Uri = "somethinggoeshere";
            CompletionItem[] completionItems = await langService.HandleCompletionRequest(textDocument);
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
            Task<CompletionItem[]> handleCompletion = langService.HandleCompletionRequest(textDocument);
            handleCompletion.Wait(TaskTimeout);

            Assert.NotNull(handleCompletion.Result);
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


            ParseResult parseResult = langService.ParseAndBind(scriptFile, null).GetAwaiter().GetResult();
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
        public void CreateStarExpansionCompletionItem_UsesSnippetPresentation()
        {
            CompletionItem completionItem = AutoCompleteHelper.CreateStarExpansionCompletionItem(
                "*",
                "[BusinessEntityID], [PersonType], [NameStyle], [Title]",
                new List<string>
                {
                    "[BusinessEntityID]",
                    "[PersonType]",
                    "[NameStyle]",
                    "[Title]"
                },
                line: 0,
                startCharacter: 7,
                endCharacter: 8);

            Assert.AreEqual(SR.StarExpansionLabel("*"), completionItem.Label);
            Assert.AreEqual(SR.StarExpansionDescription("*", "4", "[BusinessEntityID], [PersonType], [NameStyle], ..."), completionItem.Detail);
            Assert.AreEqual(CompletionItemKind.Snippet, completionItem.Kind);
            Assert.AreEqual("*", completionItem.FilterText);
            Assert.True(completionItem.Preselect);
            Assert.AreEqual("[BusinessEntityID], [PersonType], [NameStyle], [Title]", completionItem.InsertText);
            Assert.AreEqual(completionItem.InsertText, completionItem.TextEdit.NewText);
            StringAssert.StartsWith("Expands * into:" + Environment.NewLine, completionItem.Documentation);
        }

        [Test]
        public void CreateStarExpansionInsertText_WithTrailingSql_UsesMultilineFormat()
        {
            string insertText = AutoCompleteHelper.CreateStarExpansionInsertText(
                    "select * from sys.all_objects",
                    line: 0,
                    startCharacter: 7,
                    endCharacter: 9,
                    columnNames: new List<string>
                    {
                        "[BusinessEntityID]",
                        "[PersonType]",
                        "[NameStyle]"
                    });

            Assert.AreEqual(
                "[BusinessEntityID]," + Environment.NewLine +
                "       [PersonType]," + Environment.NewLine +
                "       [NameStyle]" + Environment.NewLine,
                insertText);
        }

        [Test]
        public void CreateStarExpansionInsertText_WithoutTrailingSql_UsesIndentedMultilineFormat()
        {
            string insertText = AutoCompleteHelper.CreateStarExpansionInsertText(
                    "select *",
                    line: 0,
                    startCharacter: 7,
                    endCharacter: 8,
                    columnNames: new List<string>
                    {
                        "[BusinessEntityID]",
                        "[PersonType]",
                        "[NameStyle]"
                    });

            Assert.AreEqual(
                "[BusinessEntityID]," + Environment.NewLine +
                "       [PersonType]," + Environment.NewLine +
                "       [NameStyle]",
                insertText);
        }

        [Test]
        public void GetStarExpansionReplacementEndCharacter_WithTrailingSql_ConsumesFollowingWhitespace()
        {
            int replacementEndCharacter = AutoCompleteHelper.GetStarExpansionReplacementEndCharacter(
                    "select * from sys.all_objects",
                    line: 0,
                    endCharacter: 8);

            Assert.AreEqual(9, replacementEndCharacter);
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
