//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
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
    /// Tests for the ServiceHost Language Service tests
    /// </summary>
    public class Autocomplete
    {
        private string testScriptUri = "testfile.sql";


        /// <summary>
        /// Tests the primary completion list event handler
        /// </summary>
        [Fact]
        public void GetCompletionsHandlerTest()
        {
            // initial cursor position in the script file
            TextDocumentPosition textDocument = new TextDocumentPosition
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
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns(Common.StandardQuery);
            
            // set up workspace mock
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);
        
            // inject mock instances into the Language Service
            LanguageService.WorkspaceServiceInstance = workspaceService.Object;         
            LanguageService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();       

            // setup the mock for SendResult
            var requestContext = new Mock<RequestContext<CompletionItem[]>>();            
            var sendResultFlow = requestContext
                .Setup(rc => rc.SendResult(It.IsAny<CompletionItem[]>()))
                .Returns(Task.FromResult(0));     

            var testScriptParseInfo = new ScriptParseInfo();
        
            LanguageService.Instance.AddOrUpdateScriptParseInfo(this.testScriptUri, testScriptParseInfo);       

            // request the completion list            
            Task task = LanguageService.HandleCompletionRequest(textDocument, requestContext.Object);            

            // wait with a timeout
            task.Wait(60000);

            // verify that send result was called with a completion array
            requestContext.Verify(m => m.SendResult(It.IsAny<CompletionItem[]>()), Times.Once());
        }        
    }
}
