//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using GlobalCommon = Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using Xunit;

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
        public void AddOrUpdateScriptParseInfoNullUri()
        {
            InitializeTestObjects();
            langService.AddOrUpdateScriptParseInfo("abracadabra", scriptParseInfo);
            Assert.True(langService.ScriptParseInfoMap.ContainsKey("abracadabra"));
        }

        [Fact]
        public void GetDefinitionInvalidTextDocument()
        {
            InitializeTestObjects();
            textDocument.TextDocument.Uri = "invaliduri";
            Assert.Null(langService.GetDefinition(textDocument, null, null));
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
        public void GetCompletionItemsInvalidTextDocument()
        {
            InitializeTestObjects();
            textDocument.TextDocument.Uri = "somethinggoeshere";
            Assert.True(langService.GetCompletionItems(textDocument, scriptFile.Object, null).Length > 0);
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
            Assert.Equal(DiagnosticsHelper.MapDiagnosticSeverity(level), DiagnosticSeverity.Error);
            level = ScriptFileMarkerLevel.Warning;
            Assert.Equal(DiagnosticsHelper.MapDiagnosticSeverity(level), DiagnosticSeverity.Warning);
            level = ScriptFileMarkerLevel.Information;
            Assert.Equal(DiagnosticsHelper.MapDiagnosticSeverity(level), DiagnosticSeverity.Information);
            level = (ScriptFileMarkerLevel)100;
            Assert.Equal(DiagnosticsHelper.MapDiagnosticSeverity(level), DiagnosticSeverity.Error);
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
