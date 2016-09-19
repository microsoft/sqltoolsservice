//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class DisposeTests
    {
        [Fact]
        public void DisposeExecutedQuery()
        {
            // If:
            // ... I request a query (doesn't matter what kind)
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var executeParams = new QueryExecuteParams {QuerySelection = null, OwnerUri = Common.OwnerUri};
            var executeRequest = RequestContextMocks.SetupRequestContextMock<QueryExecuteResult, QueryExecuteCompleteParams>(null, QueryExecuteCompleteEvent.Type, null, null);
            queryService.HandleExecuteRequest(executeParams, executeRequest.Object).Wait();

            // ... And then I dispose of the query
            var disposeParams = new QueryDisposeParams {OwnerUri = Common.OwnerUri};
            QueryDisposeResult result = null;
            var disposeRequest = GetQueryDisposeResultContextMock(qdr => result = qdr, null);
            queryService.HandleDisposeRequest(disposeParams, disposeRequest.Object).Wait();

            // Then:
            // ... I should have seen a successful result
            // ... And the active queries should be empty
            VerifyQueryDisposeCallCount(disposeRequest, Times.Once(), Times.Never());
            Assert.Null(result.Messages);
            Assert.Empty(queryService.ActiveQueries);
        }

        [Fact]
        public void QueryDisposeMissingQuery()
        {
            // If:
            // ... I attempt to dispose a query that doesn't exist
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), false);
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
