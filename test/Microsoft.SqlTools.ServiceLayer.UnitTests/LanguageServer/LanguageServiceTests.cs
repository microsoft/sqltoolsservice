//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    /// <summary>
    /// Tests for the ServiceHost Language Service tests
    /// </summary>
    public class LanguageServiceTests
    {
        /// <summary>
        /// Verify that the latest SqlParser (2016 as of this writing) is used by default
        /// </summary>
        [Test]
        public void LatestSqlParserIsUsedByDefault()
        {
            // This should only parse correctly on SQL server 2016 or newer
            const string sql2016Text =
                @"CREATE SECURITY POLICY [FederatedSecurityPolicy]" + "\r\n" +
                @"ADD FILTER PREDICATE [rls].[fn_securitypredicate]([CustomerId])" + "\r\n" +
                @"ON [dbo].[Customer];";

            LanguageService service = TestObjects.GetTestLanguageService();

            // parse
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents(sql2016Text);
            ScriptFileMarker[] fileMarkers = service.GetSemanticMarkers(scriptFile).GetAwaiter().GetResult();

            // verify that no errors are detected
            Assert.AreEqual(0, fileMarkers.Length);
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
            LanguageService service = TestObjects.GetTestLanguageService();

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
            LanguageService service = TestObjects.GetTestLanguageService();

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
            LanguageService service = TestObjects.GetTestLanguageService();

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

        /// <summary>
        /// Verify that GetSignatureHelp returns null when the provided TextDocumentPosition
        /// has no associated ScriptParseInfo.
        /// </summary>
        [Test]
        public void GetSignatureHelpReturnsNullIfParseInfoNotInitialized()
        {
            // Given service doesn't have parseinfo intialized for a document
            const string docContent = "SELECT * FROM sys.objects";
            LanguageService service = TestObjects.GetTestLanguageService();
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

            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = false };

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

            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = false };

            var scriptDocumentInfo = new TestScriptDocumentInfo(
                new TextDocumentPosition()
                {
                    TextDocument = new TextDocumentIdentifier() { Uri = TestObjects.ScriptUri },
                    Position = new Position() { Line = 0, Character = 0 }
                }, scriptFile, scriptInfo, "all");

            CompletionItem[] result = AutoCompleteHelper.GetDefaultCompletionItems(scriptDocumentInfo, false);
            Assert.AreEqual(1, result.Length);

        }
    }
}
