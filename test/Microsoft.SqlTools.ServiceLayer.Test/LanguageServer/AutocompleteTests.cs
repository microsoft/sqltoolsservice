//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.LanguageServices
{
    /// <summary>
    /// Tests for the language service autocomplete component
    /// </summary>
    public class AutocompleteTests
    {
        private const int TaskTimeout = 60000;

        private readonly string testScriptUri = TestObjects.ScriptUri;

        private readonly string testConnectionKey = "testdbcontextkey";

        private Mock<ConnectedBindingQueue> bindingQueue;

        private Mock<WorkspaceService<SqlToolsSettings>> workspaceService;

        private Mock<RequestContext<CompletionItem[]>> requestContext;

        private Mock<ScriptFile> scriptFile;

        private Mock<IBinder> binder; 

        private ScriptParseInfo scriptParseInfo;  

        private TextDocumentPosition textDocument;

        private void InitializeTestObjects()
        {
            // initial cursor position in the script file
            textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier {Uri = this.testScriptUri},
                Position = new Position
                {
                    Line = 0,
                    Character = 0
                }
            };                    

            // default settings are stored in the workspace service
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings = new SqlToolsSettings();

            // set up file for returning the query
            scriptFile = new Mock<ScriptFile>();
            scriptFile.SetupGet(file => file.Contents).Returns(Common.StandardQuery);
            scriptFile.SetupGet(file => file.ClientFilePath).Returns(this.testScriptUri);

            // set up workspace mock
            workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(scriptFile.Object);
        
            // setup binding queue mock
            bindingQueue = new Mock<ConnectedBindingQueue>();
            bindingQueue.Setup(q => q.AddConnectionContext(It.IsAny<ConnectionInfo>()))
                .Returns(this.testConnectionKey);

            // inject mock instances into the Language Service
            LanguageService.WorkspaceServiceInstance = workspaceService.Object;         
            LanguageService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();     
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo(); 
            LanguageService.ConnectionServiceInstance.OwnerToConnectionMap.Add(this.testScriptUri, connectionInfo); 
            LanguageService.Instance.BindingQueue = bindingQueue.Object;

            // setup the mock for SendResult
            requestContext = new Mock<RequestContext<CompletionItem[]>>();            
            requestContext.Setup(rc => rc.SendResult(It.IsAny<CompletionItem[]>()))
                .Returns(Task.FromResult(0));     

            // setup the IBinder mock
            binder = new Mock<IBinder>();
            binder.Setup(b => b.Bind(
                It.IsAny<IEnumerable<ParseResult>>(),
                It.IsAny<string>(),
                It.IsAny<BindMode>()));
            
            scriptParseInfo = new ScriptParseInfo();        
            LanguageService.Instance.AddOrUpdateScriptParseInfo(this.testScriptUri, scriptParseInfo);      
            scriptParseInfo.IsConnected = true;            
            scriptParseInfo.ConnectionKey = LanguageService.Instance.BindingQueue.AddConnectionContext(connectionInfo);

            // setup the binding context object
            ConnectedBindingContext bindingContext = new ConnectedBindingContext();
            bindingContext.Binder = binder.Object;
            bindingContext.MetadataDisplayInfoProvider = new MetadataDisplayInfoProvider();
            LanguageService.Instance.BindingQueue.BindingContextMap.Add(scriptParseInfo.ConnectionKey, bindingContext);                
        }

        [Fact]
        public void HandleCompletionRequestDisabled()
        {
            InitializeTestObjects();
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings.SqlTools.IntelliSense.EnableIntellisense = false;            
            Assert.NotNull(LanguageService.HandleCompletionRequest(null, null));
        }

        [Fact]
        public void HandleCompletionResolveRequestDisabled()
        {
            InitializeTestObjects();
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings.SqlTools.IntelliSense.EnableIntellisense = false;            
            Assert.NotNull(LanguageService.HandleCompletionResolveRequest(null, null));
        }

        [Fact]
        public void HandleSignatureHelpRequestDisabled()
        {
            InitializeTestObjects();
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings.SqlTools.IntelliSense.EnableIntellisense = false;            
            Assert.NotNull(LanguageService.HandleSignatureHelpRequest(null, null));
        }               

        [Fact]
        public void AddOrUpdateScriptParseInfoNullUri()
        {
            InitializeTestObjects();
            LanguageService.Instance.AddOrUpdateScriptParseInfo("abracadabra", scriptParseInfo);
            Assert.True(LanguageService.Instance.ScriptParseInfoMap.ContainsKey("abracadabra"));
        }

        [Fact]
        public void GetDefinitionInvalidTextDocument()
        {
            InitializeTestObjects();
            textDocument.TextDocument.Uri = "abracadabra";
            Assert.Null(LanguageService.Instance.GetDefinition(textDocument, null, null));
        }

        [Fact]
        public void RemoveScriptParseInfoNullUri()
        {
            InitializeTestObjects();
            Assert.False(LanguageService.Instance.RemoveScriptParseInfo("abracadabra"));
        }

        [Fact]
        public void IsPreviewWindowNullScriptFileTest()
        {
            InitializeTestObjects();
            Assert.False(LanguageService.Instance.IsPreviewWindow(null));
        }

        [Fact]
        public void GetCompletionItemsInvalidTextDocument()
        {
            InitializeTestObjects();
            textDocument.TextDocument.Uri = "abracadabra";
            Assert.True(LanguageService.Instance.GetCompletionItems(textDocument, scriptFile.Object, null).Length > 0);
        }        

        /// <summary>
        /// Tests the primary completion list event handler
        /// </summary>
        [Fact]
        public void GetCompletionsHandlerTest()
        {
            InitializeTestObjects();

            // request the completion list            
            Task handleCompletion = LanguageService.HandleCompletionRequest(textDocument, requestContext.Object);
            handleCompletion.Wait(TaskTimeout);

            // verify that send result was called with a completion array
            requestContext.Verify(m => m.SendResult(It.IsAny<CompletionItem[]>()), Times.Once());
        }
    }
}
