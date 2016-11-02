//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class DisposeTests
    {
        [Fact]
        public void DisposeResultSet()
        {
            // Setup: Mock file stream factory, mock db reader
            var mockFileStreamFactory = new Mock<IFileStreamFactory>();
            var mockDataReader = Common.CreateTestConnection(null, false).CreateCommand().ExecuteReaderAsync().Result;
            
            // If: I setup a single resultset and then dispose it
            ResultSet rs = new ResultSet(mockDataReader, Common.Ordinal, mockFileStreamFactory.Object);
            rs.Dispose();

            // Then: The file that was created should have been deleted
            mockFileStreamFactory.Verify(fsf => fsf.DisposeFile(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async void DisposeExecutedQuery()
        {
            // Set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns("doesn't matter");
            // Set up workspace mock
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);
            // If:
            // ... I request a query (doesn't matter what kind)
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true, workspaceService.Object);
            var executeParams = new QueryExecuteParams {QuerySelection = null, OwnerUri = Common.OwnerUri};
            var executeRequest = RequestContextMocks.SetupRequestContextMock<QueryExecuteResult, QueryExecuteCompleteParams>(null, QueryExecuteCompleteEvent.Type, null, null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // ... And then I dispose of the query
            var disposeParams = new QueryDisposeParams {OwnerUri = Common.OwnerUri};
            QueryDisposeResult result = null;
            var disposeRequest = GetQueryDisposeResultContextMock(qdr => {
                result = qdr;
            }, null);
            queryService.HandleDisposeRequest(disposeParams, disposeRequest.Object).Wait();

            // Then:
            // ... I should have seen a successful result
            // ... And the active queries should be empty
            VerifyQueryDisposeCallCount(disposeRequest, Times.Once(), Times.Never());
            Assert.Null(result.Messages);
            Assert.Empty(queryService.ActiveQueries);
        }

        [Fact]
        public async void QueryDisposeMissingQuery()
        {
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            // If:
            // ... I attempt to dispose a query that doesn't exist
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), false, workspaceService.Object);
            var disposeParams = new QueryDisposeParams {OwnerUri = Common.OwnerUri};
            QueryDisposeResult result = null;
            var disposeRequest = GetQueryDisposeResultContextMock(qdr => result = qdr, null);
            queryService.HandleDisposeRequest(disposeParams, disposeRequest.Object).Wait();

            // Then:
            // ... I should have gotten an error result
            VerifyQueryDisposeCallCount(disposeRequest, Times.Once(), Times.Never());
            Assert.NotNull(result.Messages);
            Assert.NotEmpty(result.Messages);
        }

        [Fact]
        public async Task ServiceDispose()
        {
            // Setup:
            // ... We need a workspace service that returns a file
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns(Common.StandardQuery);
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);
            // ... We need a query service
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true,
                workspaceService.Object);

            // If:
            // ... I execute some bogus query
            var queryParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };
            var requestContext = RequestContextMocks.Create<QueryExecuteResult>(null);
            await queryService.HandleExecuteRequest(queryParams, requestContext.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // ... And it sticks around as an active query
            Assert.Equal(1, queryService.ActiveQueries.Count);

            // ... The query execution service is disposed, like when the service is shutdown
            queryService.Dispose();

            // Then:
            // ... There should no longer be an active query
            Assert.Empty(queryService.ActiveQueries);
        }

        #region Mocking

        private Mock<RequestContext<QueryDisposeResult>> GetQueryDisposeResultContextMock(
            Action<QueryDisposeResult> resultCallback,
            Action<object> errorCallback)
        {
            var requestContext = new Mock<RequestContext<QueryDisposeResult>>();

            // Setup the mock for SendResult
            var sendResultFlow = requestContext
                .Setup(rc => rc.SendResult(It.IsAny<QueryDisposeResult>()))
                .Returns(Task.FromResult(0));
            if (resultCallback != null)
            {
                sendResultFlow.Callback(resultCallback);
            }

            // Setup the mock for SendError
            var sendErrorFlow = requestContext
                .Setup(rc => rc.SendError(It.IsAny<object>()))
                .Returns(Task.FromResult(0));
            if (errorCallback != null)
            {
                sendErrorFlow.Callback(errorCallback);
            }

            return requestContext;
        }

        private void VerifyQueryDisposeCallCount(Mock<RequestContext<QueryDisposeResult>> mock, Times sendResultCalls,
            Times sendErrorCalls)
        {
            mock.Verify(rc => rc.SendResult(It.IsAny<QueryDisposeResult>()), sendResultCalls);
            mock.Verify(rc => rc.SendError(It.IsAny<object>()), sendErrorCalls);
        }

        #endregion

    }
}
