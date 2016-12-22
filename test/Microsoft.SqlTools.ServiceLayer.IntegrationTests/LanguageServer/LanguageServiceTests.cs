//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.LanguageServer
{
    /// <summary>
    /// Tests for the ServiceHost Language Service tests
    /// </summary>
    public class LanguageServiceTests
    {
        private static void GetLiveAutoCompleteTestObjects(
            out TextDocumentPosition textDocument,
            out ScriptFile scriptFile,
            out ConnectionInfo connInfo)
        {
            textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = TestObjects.ScriptUri },
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

            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };

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
    }
}
