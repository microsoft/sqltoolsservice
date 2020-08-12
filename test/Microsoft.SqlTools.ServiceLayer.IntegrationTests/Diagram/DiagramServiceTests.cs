//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.Diagram.Contracts;
using Microsoft.SqlTools.ServiceLayer.Diagram;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Metadata
{
    /// <summary>
    /// Tests for the Metadata service component
    /// </summary>
    public class DiagramServiceTests
    {
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

        /// <summary>
        /// Verify that the metadata service correctly returns details for user tables
        /// </summary>
        [Fact]
        public async void DiagramServiceHandlerTest1()
        {
            var result = GetLiveAutoCompleteTestObjects();         
            var requestContext = new Mock<RequestContext<DiagramRequestResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<DiagramRequestResult>())).Returns(Task.FromResult(new object()));

            var schemaParams = new DiagramRequestParams
            {
                OwnerUri = result.ConnectionInfo.OwnerUri
            };

            await DiagramService.HandleDiagramModelRequest(schemaParams, requestContext.Object);

            await DiagramService.DiagramModelTask;

            requestContext.VerifyAll();
        }
    }
}
