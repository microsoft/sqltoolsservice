//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Moq;
using Xunit;


namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Scripting
{
    /// <summary>
    /// Tests for the scripting service component
    /// </summary>
    public class ScriptingServiceTests
    {
        private const string SchemaName = "dbo";
        private const string TableName = "spt_monitor";
        private const string ViewName = "test";
        private const string DatabaseName = "test-db";
        private const string StoredProcName = "test-sp";
        private string[] objects = new string[5] {"Table", "View", "Schema", "Database", "SProc"};
        private string[] selectObjects = new string[2] { "Table", "View" };

        private LiveConnectionHelper.TestConnectionResult GetLiveAutoCompleteTestObjects()
        {
            var textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = Test.Common.Constants.OwnerUri },
                Position = new Position
                {
                    Line = 0,
                    Character = 0
                }
            };

            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            result.TextDocumentPosition = textDocument;
            return result;
        }

        private async Task<Mock<RequestContext<ScriptingResult>>> SendAndValidateScriptRequest()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<ScriptingResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<ScriptingResult>())).Returns(Task.FromResult(new object()));

            var scriptingParams = new ScriptingParams
            {
                ConnectionString = ConnectionService.BuildConnectionString(result.ConnectionInfo.ConnectionDetails)
            };

            ScriptingService service = new ScriptingService();
            await service.HandleScriptExecuteRequest(scriptingParams, requestContext.Object);

            return requestContext;
        }

        /// <summary>
        /// Verify the script object request
        /// </summary>
        [Fact]
        public async void ScriptingScript()
        {
            foreach (string obj in objects)
            {
                Assert.NotNull(await SendAndValidateScriptRequest());
            }
        }
    }
}
