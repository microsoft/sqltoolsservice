//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Location = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Location;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    /// <summary>
    /// Tests for the language service autocomplete component
    /// </summary>
    public class AutocompleteTests : LanguageServiceTestBase<CompletionItem>
    {

        [Fact]
        public void HandleCompletionRequestDisabled()
        {
            InitializeTestObjects();
            langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = false;
            Assert.NotNull(langService.HandleCompletionRequest(null, null));
        }

        [Fact]
        public void HandleCompletionResolveRequestDisabled()
        {
            InitializeTestObjects();
            langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = false;
            Assert.NotNull(langService.HandleCompletionResolveRequest(null, null));
        }

        [Fact]
        public void HandleSignatureHelpRequestDisabled()
        {
            InitializeTestObjects();
            langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = false;
            Assert.NotNull(langService.HandleSignatureHelpRequest(null, null));
        }

        [Fact]
        public async Task HandleSignatureHelpRequestNonMssqlFile()
        {
            InitializeTestObjects();

            // setup the mock for SendResult
            var signatureRequestContext = new Mock<RequestContext<SignatureHelp>>();
            SignatureHelp result = null;
            signatureRequestContext.Setup(rc => rc.SendResult(It.IsAny<SignatureHelp>()))
            .Returns<SignatureHelp>((signature) => {
                result = signature;
                return Task.FromResult(0);
            });
            signatureRequestContext.Setup(rc => rc.SendError(It.IsAny<string>(), It.IsAny<int>())).Returns(Task.FromResult(0));


            langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = true;
            await langService.HandleDidChangeLanguageFlavorNotification(new LanguageFlavorChangeParams {
                Uri = textDocument.TextDocument.Uri,
                Language = LanguageService.SQL_LANG.ToLower(),
                Flavor = "NotMSSQL"
            }, null);
            await langService.HandleSignatureHelpRequest(textDocument, signatureRequestContext.Object);
            // verify that the response was sent with a null response value
            signatureRequestContext.Verify(m => m.SendResult(It.IsAny<SignatureHelp>()), Times.Once());
            Assert.Null(result);
            signatureRequestContext.Verify(m => m.SendError(It.IsAny<string>(), It.IsAny<int>()), Times.Never());
        }               

        [Fact]
        public void AddOrUpdateScriptParseInfoNullUri()
        {
            InitializeTestObjects();
            langService.AddOrUpdateScriptParseInfo("abracadabra", scriptParseInfo);
            Assert.True(langService.ScriptParseInfoMap.ContainsKey("abracadabra"));
        }

        [Fact]
        public async void HandleDefinitionRequest_InvalidTextDocument_SendsEmptyListResponse()
        {
            InitializeTestObjects();
            textDocument.TextDocument.Uri = "invaliduri";

            // setup the mock for SendResult
            var definitionRequestContext = new Mock<RequestContext<Location[]>>();
            Location[] result = null;
            definitionRequestContext.Setup(rc => rc.SendResult(It.IsAny<Location[]>()))
            .Returns<Location[]>((resultDetails) => {
                result = resultDetails;
                return Task.FromResult(0);
            });

            await langService.HandleDefinitionRequest(textDocument, definitionRequestContext.Object);
            // Should get an empty array when passed
            Assert.NotNull(result);
            Assert.True(result.Length == 0, $"Unexpected values passed to SendResult : [{ string.Join(",", (object[])result)}]");
        }

        [Fact]
        public void RemoveScriptParseInfoNullUri()
        {
            InitializeTestObjects();
            Assert.False(langService.RemoveScriptParseInfo("abc123"));
        }

        [Fact]
        public void IsPreviewWindowNullScriptFileTest()
        {
            InitializeTestObjects();
            Assert.False(langService.IsPreviewWindow(null));
        }

        [Fact]
        public async void HandleCompletionRequest_InvalidTextDocument_SendsNullResult()
        {
            InitializeTestObjects();
            // setup the mock for SendResult to capture the items
            CompletionItem[] completionItems = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<CompletionItem[]>()))
                .Returns<CompletionItem[]>((resultDetails) => {
                    completionItems = resultDetails;
                    return Task.FromResult(0);
                });

            textDocument.TextDocument.Uri = "somethinggoeshere";
            await langService.HandleCompletionRequest(textDocument, requestContext.Object);
            requestContext.Verify(m => m.SendResult(It.IsAny<CompletionItem[]>()), Times.Once());
            Assert.Null(completionItems);
        }

        [Fact]
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
            Assert.Equal(diagnostic.Message, scriptFileMarker.Message);
        }

        [Fact]
        public void MapDiagnosticSeverityTest()
        {
            var level = ScriptFileMarkerLevel.Error;
            Assert.Equal(DiagnosticSeverity.Error, DiagnosticsHelper.MapDiagnosticSeverity(level));
            level = ScriptFileMarkerLevel.Warning;
            Assert.Equal(DiagnosticSeverity.Warning, DiagnosticsHelper.MapDiagnosticSeverity(level));
            level = ScriptFileMarkerLevel.Information;
            Assert.Equal(DiagnosticSeverity.Information, DiagnosticsHelper.MapDiagnosticSeverity(level));
            level = (ScriptFileMarkerLevel)100;
            Assert.Equal(DiagnosticSeverity.Error, DiagnosticsHelper.MapDiagnosticSeverity(level));
        }

        /// <summary>
        /// Tests the primary completion list event handler
        /// </summary>
        [Fact]
        public void GetCompletionsHandlerTest()
        {
            InitializeTestObjects();

            // request the completion list            
            Task handleCompletion = langService.HandleCompletionRequest(textDocument, requestContext.Object);
            handleCompletion.Wait(TaskTimeout);

            // verify that send result was called with a completion array
            requestContext.Verify(m => m.SendResult(It.IsAny<CompletionItem[]>()), Times.Once());
        }
    }
}
