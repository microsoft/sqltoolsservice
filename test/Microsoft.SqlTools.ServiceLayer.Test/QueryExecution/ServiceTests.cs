using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class ServiceTests
    {

        [Fact]
        public void QueryExecuteValidNoResultsTest()
        {
            // If:
            // ... I request to execute a valid query with no results
            var queryService = GetPrimedExecutionService(Common.CreateMockFactory(null, false));
            var queryParams = new QueryExecuteParams
            {
                QueryText = "Doesn't Matter",
                OwnerUri = "testFile"
            };

            QueryExecuteResult result = null;
            QueryExecuteCompleteParams completeParams = null;
            var requestContext = GetQueryExecuteResultContextMock(qer => result = qer, (et, cp) => completeParams = cp, null);
            queryService.HandleExecuteRequest(queryParams, requestContext.Object).Wait();

            // Then:
            // ... No Errors should have been sent
            // ... A successful result should have been sent with no messages
            // ... A completion event should have been fired with empty results
            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Never());
            Assert.Null(result.Messages);
            Assert.Empty(completeParams.Messages);
            Assert.Empty(completeParams.ResultSetSummaries);
            Assert.False(completeParams.Error);
        }

        [Fact]
        public void QueryExecuteValidResultsTest()
        {
            // If:
            // ... I request to execute a valid query with results
            var queryService = GetPrimedExecutionService(Common.CreateMockFactory(new [] {Common.StandardTestData}, false));
            var queryParams = new QueryExecuteParams {OwnerUri = "testFile", QueryText = "Doesn't Matter"};

            QueryExecuteResult result = null;
            QueryExecuteCompleteParams completeParams = null;
            var requestContext = GetQueryExecuteResultContextMock(qer => result = qer, (et, cp) => completeParams = cp, null);
            queryService.HandleExecuteRequest(queryParams, requestContext.Object).Wait();

            // Then:
            // ... No errors should have been send
            // ... A successful result should have been sent with no messages
            // ... A completion event should hvae been fired with one result
            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Never());
            Assert.Null(result.Messages);
            Assert.Empty(completeParams.Messages);
            Assert.NotEmpty(completeParams.ResultSetSummaries);
            Assert.False(completeParams.Error);
        }



        private ConnectionDetails GetTestConnectionDetails()
        {
            return new ConnectionDetails
            {
                DatabaseName = "123",
                Password = "456",
                ServerName = "789",
                UserName = "012"
            };
        }

        private QueryExecutionService GetPrimedExecutionService(ISqlConnectionFactory factory)
        {
            var connectionService = new ConnectionService(factory);
            connectionService.Connect(new ConnectParams {Connection = GetTestConnectionDetails(), OwnerUri = "testFile"});
            return new QueryExecutionService(connectionService);
        }

        private Mock<RequestContext<QueryExecuteResult>> GetQueryExecuteResultContextMock(
            Action<QueryExecuteResult> resultCallback,
            Action<EventType<QueryExecuteCompleteParams>, QueryExecuteCompleteParams> eventCallback,
            Action<object> errorCallback)
        {
            var requestContext = new Mock<RequestContext<QueryExecuteResult>>();

            // Setup the mock for SendResult
            var sendResultFlow = requestContext
                .Setup(rc => rc.SendResult(It.IsAny<QueryExecuteResult>()))
                .Returns(Task.FromResult(0));
            if (resultCallback != null)
            {
                sendResultFlow.Callback(resultCallback);
            }

            // Setup the mock for SendEvent
            var sendEventFlow = requestContext.Setup(rc => rc.SendEvent(
                It.Is<EventType<QueryExecuteCompleteParams>>(m => m == QueryExecuteCompleteEvent.Type),
                It.IsAny<QueryExecuteCompleteParams>()))
                .Returns(Task.FromResult(0));
            if (eventCallback != null)
            {
                sendEventFlow.Callback(eventCallback);
            }

            // Setup the mock for SendError
            var sendErrorFlow = requestContext.Setup(rc => rc.SendError(It.IsAny<object>()))
                .Returns(Task.FromResult(0));
            if (errorCallback != null)
            {
                sendErrorFlow.Callback(errorCallback);
            }

            return requestContext;
        }

        private void VerifyQueryExecuteCallCount(Mock<RequestContext<QueryExecuteResult>> mock, Times sendResultCalls, Times sendEventCalls, Times sendErrorCalls)
        {
            mock.Verify(rc => rc.SendResult(It.IsAny<QueryExecuteResult>()), sendResultCalls);
            mock.Verify(rc => rc.SendEvent(
                It.Is<EventType<QueryExecuteCompleteParams>>(m => m == QueryExecuteCompleteEvent.Type), 
                It.IsAny<QueryExecuteCompleteParams>()), sendEventCalls);
            mock.Verify(rc => rc.SendError(It.IsAny<object>()), sendErrorCalls);
        }
    }
}
