//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class CancelTests
    {
        [Fact]
        public async void CancelInProgressQueryTest()
        {
            // Set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.Setup(file => file.GetLinesInRange(It.IsAny<BufferRange>()))
                .Returns(new[] { Common.StandardQuery });
            // Set up workspace mock
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);

            // If:
            // ... I request a query (doesn't matter what kind) and execute it
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService.Object);
            var executeParams = new QueryExecuteParams { QuerySelection = Common.SubsectionDocument, OwnerUri = Common.OwnerUri };
            var executeRequest = 
                RequestContextMocks.SetupRequestContextMock<QueryExecuteResult, QueryExecuteCompleteParams>(null, QueryExecuteCompleteEvent.Type, null, null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;
            queryService.ActiveQueries[Common.OwnerUri].HasExecuted = false;    // Fake that it hasn't completed execution

            // ... And then I request to cancel the query
            var cancelParams = new QueryCancelParams {OwnerUri = Common.OwnerUri};
            QueryCancelResult result = null;
            var cancelRequest = GetQueryCancelResultContextMock(qcr => result = qcr, null);
            await queryService.HandleCancelRequest(cancelParams, cancelRequest.Object);

            // Then:
            // ... I should have seen a successful event (no messages)
            VerifyQueryCancelCallCount(cancelRequest, Times.Once(), Times.Never());
            Assert.Null(result.Messages);
            
            // ... The query should not have been disposed
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async void CancelExecutedQueryTest()
        {
            
            // Set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns(Common.StandardQuery);
            // Set up workspace mock
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);
            // If:
            // ... I request a query (doesn't matter what kind) and wait for execution
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService.Object);
            var executeParams = new QueryExecuteParams {QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri};
            var executeRequest =
                RequestContextMocks.SetupRequestContextMock<QueryExecuteResult, QueryExecuteCompleteParams>(null, QueryExecuteCompleteEvent.Type, null, null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // ... And then I request to cancel the query
            var cancelParams = new QueryCancelParams {OwnerUri = Common.OwnerUri};
            QueryCancelResult result = null;
            var cancelRequest = GetQueryCancelResultContextMock(qcr => result = qcr, null);
            await queryService.HandleCancelRequest(cancelParams, cancelRequest.Object);

            // Then:
            // ... I should have seen a result event with an error message
            VerifyQueryCancelCallCount(cancelRequest, Times.Once(), Times.Never());
            Assert.NotNull(result.Messages);

            // ... The query should not have been disposed
            Assert.NotEmpty(queryService.ActiveQueries);
        }

        [Fact]
        public async Task CancelNonExistantTest()
        {

            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            // If:
            // ... I request to cancel a query that doesn't exist
            var queryService = Common.GetPrimedExecutionService(null, false, false, workspaceService.Object);
            var cancelParams = new QueryCancelParams {OwnerUri = "Doesn't Exist"};
            QueryCancelResult result = null;
            var cancelRequest = GetQueryCancelResultContextMock(qcr => result = qcr, null);
            await queryService.HandleCancelRequest(cancelParams, cancelRequest.Object);

            // Then:
            // ... I should have seen a result event with an error message
            VerifyQueryCancelCallCount(cancelRequest, Times.Once(), Times.Never());
            Assert.NotNull(result.Messages);
        }

        #region Mocking

        private static Mock<RequestContext<QueryCancelResult>> GetQueryCancelResultContextMock(
            Action<QueryCancelResult> resultCallback,
            Action<object> errorCallback)
        {
            var requestContext = new Mock<RequestContext<QueryCancelResult>>();

            // Setup the mock for SendResult
            var sendResultFlow = requestContext
                .Setup(rc => rc.SendResult(It.IsAny<QueryCancelResult>()))
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

        private static void VerifyQueryCancelCallCount(Mock<RequestContext<QueryCancelResult>> mock,
            Times sendResultCalls, Times sendErrorCalls)
        {
            mock.Verify(rc => rc.SendResult(It.IsAny<QueryCancelResult>()), sendResultCalls);
            mock.Verify(rc => rc.SendError(It.IsAny<object>()), sendErrorCalls);
        }

        #endregion

    }
}
