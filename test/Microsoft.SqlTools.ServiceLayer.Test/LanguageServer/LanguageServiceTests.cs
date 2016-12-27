//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.LanguageServer
{
    /// <summary>
    /// Tests for the ServiceHost Language Service tests
    /// </summary>
    public class LanguageServiceTests
    {
        #region "Diagnostics tests"


        /// <summary>
        /// Verify that the latest SqlParser (2016 as of this writing) is used by default
        /// </summary>
        [Fact]
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
            ScriptFileMarker[] fileMarkers = service.GetSemanticMarkers(scriptFile);

            // verify that no errors are detected
            Assert.Equal(0, fileMarkers.Length);
        }

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Fact]
        public void ParseSelectStatementWithoutErrors()
        {
            // sql statement with no errors
            const string sqlWithErrors = "SELECT * FROM sys.objects";

            // get the test service 
            LanguageService service = TestObjects.GetTestLanguageService();

            // parse the sql statement
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents(sqlWithErrors);
            ScriptFileMarker[] fileMarkers = service.GetSemanticMarkers(scriptFile);

            // verify there are no errors
            Assert.Equal(0, fileMarkers.Length);
        }

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Fact]
        public void ParseSelectStatementWithError()
        {
            // sql statement with errors
            const string sqlWithErrors = "SELECT *** FROM sys.objects";

            // get test service
            LanguageService service = TestObjects.GetTestLanguageService();

            // parse sql statement
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents(sqlWithErrors);
            ScriptFileMarker[] fileMarkers = service.GetSemanticMarkers(scriptFile);

            // verify there is one error
            Assert.Equal(1, fileMarkers.Length);

            // verify the position of the error
            Assert.Equal(9, fileMarkers[0].ScriptRegion.StartColumnNumber);
            Assert.Equal(1, fileMarkers[0].ScriptRegion.StartLineNumber);
            Assert.Equal(10, fileMarkers[0].ScriptRegion.EndColumnNumber);
            Assert.Equal(1, fileMarkers[0].ScriptRegion.EndLineNumber);
        }

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Fact]
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
            ScriptFileMarker[] fileMarkers = service.GetSemanticMarkers(scriptFile);

            // verify there are two errors
            Assert.Equal(2, fileMarkers.Length);

            // check position of first error
            Assert.Equal(9, fileMarkers[0].ScriptRegion.StartColumnNumber);
            Assert.Equal(1, fileMarkers[0].ScriptRegion.StartLineNumber);
            Assert.Equal(10, fileMarkers[0].ScriptRegion.EndColumnNumber);
            Assert.Equal(1, fileMarkers[0].ScriptRegion.EndLineNumber);

            // check position of second error
            Assert.Equal(9, fileMarkers[1].ScriptRegion.StartColumnNumber);
            Assert.Equal(3, fileMarkers[1].ScriptRegion.StartLineNumber);
            Assert.Equal(10, fileMarkers[1].ScriptRegion.EndColumnNumber);
            Assert.Equal(3, fileMarkers[1].ScriptRegion.EndLineNumber);
        }

        /// <summary>
        /// Verify that GetSignatureHelp returns null when the provided TextDocumentPosition
        /// has no associated ScriptParseInfo.
        /// </summary>
        [Fact]
        public void GetSignatureHelpReturnsNullIfParseInfoNotInitialized()
        {
            // Given service doesn't have parseinfo intialized for a document
            const string docContent = "SELECT * FROM sys.objects";
            LanguageService service = TestObjects.GetTestLanguageService();
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents(docContent);

            // When requesting SignatureHelp
            SignatureHelp signatureHelp = service.GetSignatureHelp(TestObjects.GetTestDocPosition(), scriptFile);
            
            // Then null is returned as no parse info can be used to find the signature
            Assert.Null(signatureHelp);
        }

        [Fact]
        public void EmptyCompletionListTest()
        {           
            Assert.Equal(AutoCompleteHelper.EmptyCompletionList.Length, 0);
        }

        [Fact]
        public void SetWorkspaceServiceInstanceTest()
        {           
            AutoCompleteHelper.WorkspaceServiceInstance = null;
            // workspace will be recreated if it's set to null
            Assert.NotNull(AutoCompleteHelper.WorkspaceServiceInstance);
        }

        internal class TestScriptDocumentInfo : ScriptDocumentInfo
        {
            public TestScriptDocumentInfo(TextDocumentPosition textDocumentPosition, ScriptFile scriptFile, ScriptParseInfo scriptParseInfo, 
                string tokenText = null)
                :base(textDocumentPosition, scriptFile, scriptParseInfo)
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

        [Fact]
        public void GetDefaultCompletionListWithNoMatchesTest()
        {           
            var scriptFile = new ScriptFile();
            scriptFile.SetFileContents("koko wants a bananas");

            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = false };

            var scriptDocumentInfo = new TestScriptDocumentInfo(
                new TextDocumentPosition()
                {
                    TextDocument = new TextDocumentIdentifier() {  Uri = TestObjects.ScriptUri  },
                    Position = new Position() { Line = 0, Character = 0 }
                }, scriptFile, scriptInfo);
      
            AutoCompleteHelper.GetDefaultCompletionItems(scriptDocumentInfo, false);

        }

        [Fact]
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
            Assert.Equal(result.Length, 1);

        }

        #endregion

        #region "General Language Service tests"

#if LIVE_CONNECTION_TESTS

        private static void GetLiveAutoCompleteTestObjects(
            out TextDocumentPosition textDocument,
            out ScriptFile scriptFile,
            out ConnectionInfo connInfo)
        {
            textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier {Uri = TestObjects.ScriptUri},
                Position = new Position
                {
                    Line = 0,
                    Character = 0
                }
            };

            connInfo = TestObjects.InitLiveConnectionInfo(out scriptFile);
        }
        
        /// <summary>
        /// Test the service initialization code path and verify nothing throws
        /// </summary>
        [Fact]
        public void ServiceInitialization()
        {
            try
            {
                TestObjects.InitializeTestServices();
            }
            catch (System.ArgumentException)
            {

            }
            Assert.True(LanguageService.Instance.Context != null);
            Assert.True(LanguageService.ConnectionServiceInstance != null);
            Assert.True(LanguageService.Instance.CurrentSettings != null);
            Assert.True(LanguageService.Instance.CurrentWorkspace != null);
        }  

        /// <summary>
        /// Test the service initialization code path and verify nothing throws
        /// </summary>
        [Fact]
        public void PrepopulateCommonMetadata()
        {
            ScriptFile scriptFile;
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfo(out scriptFile);

            ScriptParseInfo scriptInfo = new ScriptParseInfo {IsConnected = true};

            AutoCompleteHelper.PrepopulateCommonMetadata(connInfo, scriptInfo, null);
        }

        // This test currently requires a live database connection to initialize 
        // SMO connected metadata provider.  Since we don't want a live DB dependency
        // in the CI unit tests this scenario is currently disabled.
        [Fact]
        public void AutoCompleteFindCompletions()
        {
            TextDocumentPosition textDocument;
            ConnectionInfo connInfo;
            ScriptFile scriptFile;
            GetLiveAutoCompleteTestObjects(out textDocument, out scriptFile, out connInfo);

            textDocument.Position.Character = 7;
            scriptFile.Contents = "select ";

            var autoCompleteService = LanguageService.Instance;
            var completions = autoCompleteService.GetCompletionItems(
                textDocument, 
                scriptFile,
                connInfo);

            Assert.True(completions.Length > 0);
        }

        /// <summary>
        /// Verify that GetSignatureHelp returns not null when the provided TextDocumentPosition
        /// has an associated ScriptParseInfo and the provided query has a function that should
        /// provide signature help.
        /// </summary>
        [Fact]
        public async void GetSignatureHelpReturnsNotNullIfParseInfoInitialized()
        {
            // When we make a connection to a live database
            ScriptFile scriptFile;
            Hosting.ServiceHost.SendEventIgnoreExceptions = true;
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfo(out scriptFile);

            // And we place the cursor after a function that should prompt for signature help
            string queryWithFunction = "EXEC sys.fn_isrolemember ";
            scriptFile.Contents = queryWithFunction;
            TextDocumentPosition textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = scriptFile.ClientFilePath
                },
                Position = new Position
                {
                    Line = 0,
                    Character = queryWithFunction.Length
                }
            };

            // If the SQL has already been parsed
            var service = LanguageService.Instance;
            await service.UpdateLanguageServiceOnConnection(connInfo);

            // We should get back a non-null ScriptParseInfo
            ScriptParseInfo parseInfo = service.GetScriptParseInfo(scriptFile.ClientFilePath);
            Assert.NotNull(parseInfo);

            // And we should get back a non-null SignatureHelp
            SignatureHelp signatureHelp = service.GetSignatureHelp(textDocument, scriptFile);
            Assert.NotNull(signatureHelp);
        }

#endif

        #endregion
    }
}
