//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.LanguageServer
{
    /// <summary>
    /// Tests for the ServiceHost Language Service tests
    /// </summary>
    public class CreateDatabaseTests
    {
        private LiveConnectionHelper.TestConnectionResult GetLiveAutoCompleteTestObjects()
        {
            var textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = Constants.OwnerUri },
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

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public void ServiceInitialization()
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
   
    }
}
