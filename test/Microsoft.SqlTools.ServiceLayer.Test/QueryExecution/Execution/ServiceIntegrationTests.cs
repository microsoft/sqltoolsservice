using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.Execution
{
    public class ServiceIntegrationTests
    {

        [Fact]
        public async void QueryExecuteValidNoResultsTest()
        {
            // Given:
            // ... Default settings are stored in the workspace service
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings = new SqlToolsSettings();

            // Set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns(Common.StandardQuery);
            // Set up workspace mock
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);
            // If:
            // ... I request to execute a valid query with no results
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true, workspaceService.Object);
            var queryParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };

            QueryExecuteResult result = null;
            QueryExecuteCompleteParams completeParams = null;
            var requestContext =
                RequestContextMocks.SetupRequestContextMock<QueryExecuteResult, QueryExecuteCompleteParams>(
                    resultCallback: qer => result = qer,
                    expectedEvent: QueryExecuteCompleteEvent.Type,
                    eventCallback: (et, cp) => completeParams = cp,
                    errorCallback: null);
            await AwaitExecution(queryService, queryParams, requestContext.Object);

            // Then:
            // ... No Errors should have been sent
            // ... A successful result should have been sent with messages on the first batch
            // ... A completion event should have been fired with empty results
            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Never());
            Assert.Null(result.Messages);
            Assert.Equal(1, completeParams.BatchSummaries.Length);
            Assert.Empty(completeParams.BatchSummaries[0].ResultSetSummaries);
            Assert.NotEmpty(completeParams.BatchSummaries[0].Messages);

            // ... There should be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }


        [Fact]
        public async void QueryExecuteValidResultsTest()
        {

            // Set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns(Common.StandardQuery);
            // Set up workspace mock
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);
            // If:
            // ... I request to execute a valid query with results
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(new[] { Common.StandardTestData }, false), true,
                workspaceService.Object);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            QueryExecuteResult result = null;
            QueryExecuteCompleteParams completeParams = null;
            var requestContext =
                RequestContextMocks.SetupRequestContextMock<QueryExecuteResult, QueryExecuteCompleteParams>(
                    resultCallback: qer => result = qer,
                    expectedEvent: QueryExecuteCompleteEvent.Type,
                    eventCallback: (et, cp) => completeParams = cp,
                    errorCallback: null);
            await AwaitExecution(queryService, queryParams, requestContext.Object);

            // Then:
            // ... No errors should have been sent
            // ... A successful result should have been sent with messages
            // ... A completion event should have been fired with one result
            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Never());
            Assert.Null(result.Messages);
            Assert.Equal(1, completeParams.BatchSummaries.Length);
            Assert.NotEmpty(completeParams.BatchSummaries[0].ResultSetSummaries);
            Assert.NotEmpty(completeParams.BatchSummaries[0].Messages);
            Assert.False(completeParams.BatchSummaries[0].HasError);

            // ... There should be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async void QueryExecuteUnconnectedUriTest()
        {

            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            // If:
            // ... I request to execute a query using a file URI that isn't connected
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), false, workspaceService.Object);
            var queryParams = new QueryExecuteParams { OwnerUri = "notConnected", QuerySelection = Common.WholeDocument };

            object error = null;
            var requestContext = RequestContextMocks.Create<QueryExecuteResult>(null)
                .AddErrorHandling(e => error = e);
            await queryService.HandleExecuteRequest(queryParams, requestContext.Object);

            // Then:
            // ... An error should have been returned
            // ... No result should have been returned
            // ... No completion event should have been fired
            // ... There should be no active queries
            VerifyQueryExecuteCallCount(requestContext, Times.Never(), Times.Never(), Times.Once());
            Assert.IsType<string>(error);
            Assert.NotEmpty((string)error);
            Assert.Empty(queryService.ActiveQueries);
        }

        [Fact]
        public async void QueryExecuteInProgressTest()
        {

            // Set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns(Common.StandardQuery);
            // Set up workspace mock
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);

            // If:
            // ... I request to execute a query
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true, workspaceService.Object);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            // Note, we don't care about the results of the first request
            var firstRequestContext = RequestContextMocks.Create<QueryExecuteResult>(null);
            await AwaitExecution(queryService, queryParams, firstRequestContext.Object);

            // ... And then I request another query without waiting for the first to complete
            queryService.ActiveQueries[Common.OwnerUri].HasExecuted = false;   // Simulate query hasn't finished
            object error = null;
            var secondRequestContext = RequestContextMocks.Create<QueryExecuteResult>(null)
                .AddErrorHandling(e => error = e);
            await AwaitExecution(queryService, queryParams, secondRequestContext.Object);

            // Then:
            // ... An error should have been sent
            // ... A result should have not have been sent
            // ... No completion event should have been fired
            // ... The original query should exist
            VerifyQueryExecuteCallCount(secondRequestContext, Times.Never(), Times.Never(), Times.Once());
            Assert.IsType<string>(error);
            Assert.NotEmpty((string)error);
            Assert.Contains(Common.OwnerUri, queryService.ActiveQueries.Keys);
        }


        [Fact]
        public async void QueryExecuteCompletedTest()
        {

            // Set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns(Common.StandardQuery);
            // Set up workspace mock
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);

            // If:
            // ... I request to execute a query
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true, workspaceService.Object);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            // Note, we don't care about the results of the first request
            var firstRequestContext = RequestContextMocks.SetupRequestContextMock<QueryExecuteResult, QueryExecuteCompleteParams>(null, QueryExecuteCompleteEvent.Type, null, null);
            await AwaitExecution(queryService, queryParams, firstRequestContext.Object);

            // ... And then I request another query after waiting for the first to complete
            QueryExecuteResult result = null;
            QueryExecuteCompleteParams complete = null;
            var secondRequestContext =
                RequestContextMocks.SetupRequestContextMock<QueryExecuteResult, QueryExecuteCompleteParams>(qer => result = qer, QueryExecuteCompleteEvent.Type, (et, qecp) => complete = qecp, null);
            await AwaitExecution(queryService, queryParams, secondRequestContext.Object);

            // Then:
            // ... No errors should have been sent
            // ... A result should have been sent with no errors
            // ... There should only be one active query
            VerifyQueryExecuteCallCount(secondRequestContext, Times.Once(), Times.Once(), Times.Never());
            Assert.Null(result.Messages);
            Assert.False(complete.BatchSummaries.Any(b => b.HasError));
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Theory]
        [InlineData(null)]
        public async Task QueryExecuteMissingSelectionTest(SelectionData selection)
        {

            // Set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns("");
            // Set up workspace mock
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);
            // If:
            // ... I request to execute a query with a missing query string
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true, workspaceService.Object);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = selection };

            object errorResult = null;
            var requestContext = RequestContextMocks.Create<QueryExecuteResult>(null)
                .AddErrorHandling(error => errorResult = error);
            await queryService.HandleExecuteRequest(queryParams, requestContext.Object);

            // Then:
            // ... Am error should have been sent
            // ... No result should have been sent
            // ... No completion event should have been fired
            // ... An active query should not have been added
            VerifyQueryExecuteCallCount(requestContext, Times.Never(), Times.Never(), Times.Once());
            Assert.NotNull(errorResult);
            Assert.IsType<string>(errorResult);
            Assert.DoesNotContain(Common.OwnerUri, queryService.ActiveQueries.Keys);

            // ... There should not be an active query
            Assert.Empty(queryService.ActiveQueries);
        }

        [Fact]
        public async void QueryExecuteInvalidQueryTest()
        {
            // Set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns(Common.StandardQuery);
            // Set up workspace mock
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);
            // If:
            // ... I request to execute a query that is invalid
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, true), true, workspaceService.Object);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            QueryExecuteResult result = null;
            QueryExecuteCompleteParams complete = null;
            var requestContext =
                RequestContextMocks.SetupRequestContextMock<QueryExecuteResult, QueryExecuteCompleteParams>(qer => result = qer, QueryExecuteCompleteEvent.Type, (et, qecp) => complete = qecp, null);
            await AwaitExecution(queryService, queryParams, requestContext.Object);

            // Then:
            // ... No errors should have been sent
            // ... A result should have been sent with success (we successfully started the query)
            // ... A completion event should have been sent with error
            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Never());
            Assert.Null(result.Messages);
            Assert.Equal(1, complete.BatchSummaries.Length);
            Assert.True(complete.BatchSummaries[0].HasError);
            Assert.NotEmpty(complete.BatchSummaries[0].Messages);
        }

        private static void VerifyQueryExecuteCallCount(Mock<RequestContext<QueryExecuteResult>> mock, Times sendResultCalls, Times sendEventCalls, Times sendErrorCalls)
        {
            mock.Verify(rc => rc.SendResult(It.IsAny<QueryExecuteResult>()), sendResultCalls);
            mock.Verify(rc => rc.SendEvent(
                It.Is<EventType<QueryExecuteCompleteParams>>(m => m == QueryExecuteCompleteEvent.Type),
                It.IsAny<QueryExecuteCompleteParams>()), sendEventCalls);
            mock.Verify(rc => rc.SendError(It.IsAny<object>()), sendErrorCalls);
        }

        private static async Task AwaitExecution(QueryExecutionService service, QueryExecuteParams qeParams,
            RequestContext<QueryExecuteResult> requestContext)
        {
            await service.HandleExecuteRequest(qeParams, requestContext);
            await service.ActiveQueries[qeParams.OwnerUri].ExecutionTask;
        }
    }
}
