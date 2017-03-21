//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Moq;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Scripting
{
    /// <summary>
    /// Tests for the scripting service component
    /// </summary>
    public class ScriptingServiceTests
    {
        private const string SchemaName = "dbo";
        private const string TableName = "spt_monitor";

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


        private async Task<Mock<RequestContext<ScriptingScriptAsResult>>> SendAndValidateScriptRequest(ScriptOperation operation)
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<ScriptingScriptAsResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<ScriptingScriptAsResult>())).Returns(Task.FromResult(new object()));

            var scriptingParams = new ScriptingScriptAsParams
            {
                OwnerUri = result.ConnectionInfo.OwnerUri,
                Operation = operation,
                Metadata = new ObjectMetadata()
                {
                    MetadataType = MetadataType.Table,
                    MetadataTypeName = "Table",
                    Schema = SchemaName,
                    Name = TableName
                }
            };

            await ScriptingService.HandleScriptingScriptAsRequest(scriptingParams, requestContext.Object);

            return requestContext;
        }

        /// <summary>
        /// Verify the script as select request
        /// </summary>
        [Fact]
        public async void ScriptingScriptAsSelect()
        {
            await SendAndValidateScriptRequest(ScriptOperation.Select);
        }

        /// <summary>
        /// Verify the script as create request
        /// </summary>
        [Fact]
        public async void ScriptingScriptAsCreate()
        {
            await SendAndValidateScriptRequest(ScriptOperation.Create);
        }

        /// <summary>
        /// Verify the script as insert request
        /// </summary>
        [Fact]
        public async void ScriptingScriptAsInsert()
        {
            await SendAndValidateScriptRequest(ScriptOperation.Insert);
        }

        /// <summary>
        /// Verify the script as update request
        /// </summary>
        [Fact]
        public async void ScriptingScriptAsUpdate()
        {
            await SendAndValidateScriptRequest(ScriptOperation.Update);
        }

        /// <summary>
        /// Verify the script as delete request
        /// </summary>
        [Fact]
        public async void ScriptingScriptAsDelete()
        {
            await SendAndValidateScriptRequest(ScriptOperation.Delete);
        }
    }
}
