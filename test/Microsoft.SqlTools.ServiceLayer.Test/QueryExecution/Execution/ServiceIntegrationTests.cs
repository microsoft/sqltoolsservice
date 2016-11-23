//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.Execution
{
    public class ServiceIntegrationTests
    {

        [Fact]
        public async void QueryExecuteSingleBatchNoResultsTest()
        {
            // Given:
            // ... Default settings are stored in the workspace service
            // ... A workspace with a standard query is configured
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings = new SqlToolsSettings();
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);

            // If:
            // ... I request to execute a valid query with no results
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var queryParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };

            QueryExecuteResult result = null;
            QueryExecuteCompleteParams completeParams = null;
            QueryExecuteBatchCompleteParams batchCompleteParams = null;
            var requestContext = RequestContextMocks.Create<QueryExecuteResult>(qer => result = qer)
                .AddEventHandling(QueryExecuteCompleteEvent.Type, (et, p) => completeParams = p)
                .AddEventHandling(QueryExecuteBatchCompleteEvent.Type, (et, p) => batchCompleteParams = p)
                .AddEventHandling(QueryExecuteResultSetCompleteEvent.Type, null);
            await Common.AwaitExecution(queryService, queryParams, requestContext.Object);

            // Then:
            // ... No Errors should have been sent
            // ... A successful result should have been sent with messages on the first batch
            // ... A completion event should have been fired with empty results
            // ... A batch completion event should have been fired with empty results
            // ... A result set completion event should not have been fired
            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Once(), Times.Never(), Times.Never());
            Assert.Null(result.Messages);

            Assert.Equal(1, completeParams.BatchSummaries.Length);
            Assert.Empty(completeParams.BatchSummaries[0].ResultSetSummaries);
            Assert.NotEmpty(completeParams.BatchSummaries[0].Messages);

            Assert.NotNull(batchCompleteParams);
            Assert.Empty(batchCompleteParams.BatchSummary.ResultSetSummaries);
            Assert.NotEmpty(batchCompleteParams.BatchSummary.Messages);
            Assert.Equal(Common.OwnerUri, batchCompleteParams.OwnerUri);

            // ... There should be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }


        [Fact]
        public async void QueryExecuteSingleBatchSingleResultTest()
        {
            // Given:
            // ... A workspace with a standard query is configured
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);

            // If:
            // ... I request to execute a valid query with results
            var queryService = Common.GetPrimedExecutionService(new[] { Common.StandardTestData }, true, false, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            QueryExecuteResult result = null;
            QueryExecuteCompleteParams completeParams = null;
            QueryExecuteBatchCompleteParams batchCompleteParams = null;
            QueryExecuteResultSetCompleteParams resultCompleteParams = null;
            var requestContext = RequestContextMocks.Create<QueryExecuteResult>(qer => result = qer)
                .AddEventHandling(QueryExecuteCompleteEvent.Type, (et, p) => completeParams = p)
                .AddEventHandling(QueryExecuteBatchCompleteEvent.Type, (et, p) => batchCompleteParams = p)
                .AddEventHandling(QueryExecuteResultSetCompleteEvent.Type, (et, p) => resultCompleteParams = p);
            await Common.AwaitExecution(queryService, queryParams, requestContext.Object);

            // Then:
            // ... No errors should have been sent
            // ... A successful result should have been sent without messages
            // ... A completion event should have been fired with one result
            // ... A batch completion event should have been fired
            // ... A resultset completion event should have been fired
            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Once(), Times.Once(), Times.Never());
            Assert.Null(result.Messages);

            Assert.Equal(1, completeParams.BatchSummaries.Length);
            Assert.NotEmpty(completeParams.BatchSummaries[0].ResultSetSummaries);
            Assert.NotEmpty(completeParams.BatchSummaries[0].Messages);
            Assert.False(completeParams.BatchSummaries[0].HasError);

            Assert.NotNull(batchCompleteParams);
            Assert.NotEmpty(batchCompleteParams.BatchSummary.ResultSetSummaries);
            Assert.NotEmpty(batchCompleteParams.BatchSummary.Messages);
            Assert.Equal(Common.OwnerUri, batchCompleteParams.OwnerUri);

            Assert.NotNull(resultCompleteParams);
            Assert.Equal(Common.StandardColumns, resultCompleteParams.ResultSetSummary.ColumnInfo.Length);
            Assert.Equal(Common.StandardRows, resultCompleteParams.ResultSetSummary.RowCount);
            Assert.Equal(Common.OwnerUri, resultCompleteParams.OwnerUri);

            // ... There should be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async Task QueryExecuteSingleBatchMultipleResultTest()
        {
            // Given:
            // ... A workspace with a standard query is configured
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);

            // If:
            // ... I request to execute a valid query with one batch and multiple result sets
            var dataset = new[] { Common.StandardTestData, Common.StandardTestData };
            var queryService = Common.GetPrimedExecutionService(dataset, true, false, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            QueryExecuteResult result = null;
            QueryExecuteCompleteParams completeParams = null;
            QueryExecuteBatchCompleteParams batchCompleteParams = null;
            List<QueryExecuteResultSetCompleteParams> resultCompleteParams = new List<QueryExecuteResultSetCompleteParams>();
            var requestContext = RequestContextMocks.Create<QueryExecuteResult>(qer => result = qer)
                .AddEventHandling(QueryExecuteCompleteEvent.Type, (et, p) => completeParams = p)
                .AddEventHandling(QueryExecuteBatchCompleteEvent.Type, (et, p) => batchCompleteParams = p)
                .AddEventHandling(QueryExecuteResultSetCompleteEvent.Type, (et, p) => resultCompleteParams.Add(p));
            await Common.AwaitExecution(queryService, queryParams, requestContext.Object);

            // Then:
            // ... No errors should have been sent
            // ... A successful result should have been sent without messages
            // ... A completion event should have been fired with one result
            // ... A batch completion event should have been fired
            // ... Two resultset completion events should have been fired
            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Once(), Times.Exactly(2), Times.Never());
            Assert.Null(result.Messages);

            Assert.Equal(1, completeParams.BatchSummaries.Length);
            Assert.NotEmpty(completeParams.BatchSummaries[0].ResultSetSummaries);
            Assert.NotEmpty(completeParams.BatchSummaries[0].Messages);
            Assert.False(completeParams.BatchSummaries[0].HasError);

            Assert.NotNull(batchCompleteParams);
            Assert.NotEmpty(batchCompleteParams.BatchSummary.ResultSetSummaries);
            Assert.NotEmpty(batchCompleteParams.BatchSummary.Messages);
            Assert.Equal(Common.OwnerUri, batchCompleteParams.OwnerUri);

            Assert.Equal(2, resultCompleteParams.Count);
            foreach (var resultParam in resultCompleteParams)
            {
                Assert.NotNull(resultCompleteParams);
                Assert.Equal(Common.StandardColumns, resultParam.ResultSetSummary.ColumnInfo.Length);
                Assert.Equal(Common.StandardRows, resultParam.ResultSetSummary.RowCount);
                Assert.Equal(Common.OwnerUri, resultParam.OwnerUri);
            }
        }

        [Fact]
        public async Task QueryExecuteMultipleBatchSingleResultTest()
        {
            // Given:
            // ... A workspace with a standard query is configured
            var workspaceService = Common.GetPrimedWorkspaceService(string.Format("{0}\r\nGO\r\n{0}", Common.StandardQuery));

            // If:
            // ... I request a to execute a valid query with multiple batches
            var dataSet = new[] { Common.StandardTestData };
            var queryService = Common.GetPrimedExecutionService(dataSet, true, false, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            QueryExecuteResult result = null;
            QueryExecuteCompleteParams completeParams = null;
            List<QueryExecuteBatchCompleteParams> batchCompleteParams = new List<QueryExecuteBatchCompleteParams>();
            List<QueryExecuteResultSetCompleteParams> resultCompleteParams = new List<QueryExecuteResultSetCompleteParams>();
            var requestContext = RequestContextMocks.Create<QueryExecuteResult>(qer => result = qer)
                .AddEventHandling(QueryExecuteCompleteEvent.Type, (et, p) => completeParams = p)
                .AddEventHandling(QueryExecuteBatchCompleteEvent.Type, (et, p) => batchCompleteParams.Add(p))
                .AddEventHandling(QueryExecuteResultSetCompleteEvent.Type, (et, p) => resultCompleteParams.Add(p));
            await Common.AwaitExecution(queryService, queryParams, requestContext.Object);

            // Then:
            // ... No errors should have been sent
            // ... A successful result should have been sent without messages

            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Exactly(2), Times.Exactly(2), Times.Never());
            Assert.Null(result.Messages);

            // ... A completion event should have been fired with one two batch summaries, one result each
            Assert.Equal(2, completeParams.BatchSummaries.Length);
            Assert.Equal(1, completeParams.BatchSummaries[0].ResultSetSummaries.Length);
            Assert.Equal(1, completeParams.BatchSummaries[1].ResultSetSummaries.Length);
            Assert.NotEmpty(completeParams.BatchSummaries[0].Messages);
            Assert.NotEmpty(completeParams.BatchSummaries[1].Messages);

            // ... Two batch completion events should have been fired
            Assert.Equal(2, batchCompleteParams.Count);
            foreach (var batch in batchCompleteParams)
            {
                Assert.NotEmpty(batch.BatchSummary.ResultSetSummaries);
                Assert.NotEmpty(batch.BatchSummary.Messages);
                Assert.Equal(Common.OwnerUri, batch.OwnerUri);
            }

            // ... Two resultset completion events should have been fired
            Assert.Equal(2, resultCompleteParams.Count);
            foreach (var resultParam in resultCompleteParams)
            {
                Assert.NotNull(resultParam.ResultSetSummary);
                Assert.Equal(Common.StandardColumns, resultParam.ResultSetSummary.ColumnInfo.Length);
                Assert.Equal(Common.StandardRows, resultParam.ResultSetSummary.RowCount);
                Assert.Equal(Common.OwnerUri, resultParam.OwnerUri);
            }

            // ... There should be one active query
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public async void QueryExecuteUnconnectedUriTest()
        {
            // Given:
            // ... A workspace with a standard query is configured
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);

            // If:
            // ... I request to execute a query using a file URI that isn't connected
            var queryService = Common.GetPrimedExecutionService(null, false, false, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = "notConnected", QuerySelection = Common.WholeDocument };

            object error = null;
            var requestContext = RequestContextMocks.Create<QueryExecuteResult>(null)
                .AddErrorHandling(e => error = e);
            await Common.AwaitExecution(queryService, queryParams, requestContext.Object);

            // Then:
            // ... An error should have been returned
            // ... No result should have been returned
            // ... No completion event should have been fired
            // ... There should be no active queries
            VerifyQueryExecuteCallCount(requestContext, Times.Never(), Times.Never(), Times.Never(), Times.Never(), Times.Once());
            Assert.IsType<string>(error);
            Assert.NotEmpty((string)error);
            Assert.Empty(queryService.ActiveQueries);
        }

        [Fact]
        public async void QueryExecuteInProgressTest()
        {
            // Given:
            // ... A workspace with a standard query is configured
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);

            // If:
            // ... I request to execute a query
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            // Note, we don't care about the results of the first request
            var firstRequestContext = RequestContextMocks.Create<QueryExecuteResult>(null);
            await Common.AwaitExecution(queryService, queryParams, firstRequestContext.Object);

            // ... And then I request another query without waiting for the first to complete
            queryService.ActiveQueries[Common.OwnerUri].HasExecuted = false;   // Simulate query hasn't finished
            object error = null;
            var secondRequestContext = RequestContextMocks.Create<QueryExecuteResult>(null)
                .AddErrorHandling(e => error = e);
            await Common.AwaitExecution(queryService, queryParams, secondRequestContext.Object);

            // Then:
            // ... An error should have been sent
            // ... A result should have not have been sent
            // ... No completion event should have been fired
            // ... A batch completion event should have fired, but not a resultset event
            // ... There should only be one active query
            VerifyQueryExecuteCallCount(secondRequestContext, Times.Never(), Times.AtMostOnce(), Times.AtMostOnce(), Times.Never(), Times.Once());
            Assert.IsType<string>(error);
            Assert.NotEmpty((string)error);
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }


        [Fact]
        public async void QueryExecuteCompletedTest()
        {
            // Given:
            // ... A workspace with a standard query is configured
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);

            // If:
            // ... I request to execute a query
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            // Note, we don't care about the results of the first request
            var firstRequestContext = RequestContextMocks.Create<QueryExecuteResult>(null);
            await Common.AwaitExecution(queryService, queryParams, firstRequestContext.Object);

            // ... And then I request another query after waiting for the first to complete
            QueryExecuteResult result = null;
            QueryExecuteCompleteParams complete = null;
            QueryExecuteBatchCompleteParams batchComplete = null;
            var secondRequestContext = RequestContextMocks.Create<QueryExecuteResult>(qer => result = qer)
                .AddEventHandling(QueryExecuteCompleteEvent.Type, (et, qecp) => complete = qecp)
                .AddEventHandling(QueryExecuteBatchCompleteEvent.Type, (et, p) => batchComplete = p);
            await Common.AwaitExecution(queryService, queryParams, secondRequestContext.Object);

            // Then:
            // ... No errors should have been sent
            // ... A result should have been sent with no errors
            // ... There should only be one active query
            // ... A batch completion event should have fired, but not a result set completion event
            VerifyQueryExecuteCallCount(secondRequestContext, Times.Once(), Times.Once(), Times.Once(), Times.Never(), Times.Never());
            Assert.Null(result.Messages);

            Assert.False(complete.BatchSummaries.Any(b => b.HasError));
            Assert.Equal(1, queryService.ActiveQueries.Count);

            Assert.NotNull(batchComplete);
            Assert.False(batchComplete.BatchSummary.HasError);
            Assert.Equal(complete.OwnerUri, batchComplete.OwnerUri);
        }

        [Theory]
        [InlineData(null)]
        public async Task QueryExecuteMissingSelectionTest(SelectionData selection)
        {
            // Given:
            // ... A workspace with a standard query is configured
            var workspaceService = Common.GetPrimedWorkspaceService(string.Empty);

            // If:
            // ... I request to execute a query with a missing query string
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = null };

            object errorResult = null;
            var requestContext = RequestContextMocks.Create<QueryExecuteResult>(null)
                .AddErrorHandling(error => errorResult = error);
            await queryService.HandleExecuteRequest(queryParams, requestContext.Object);


            // Then:
            // ... Am error should have been sent
            // ... No result should have been sent
            // ... No completion events should have been fired
            // ... An active query should not have been added
            VerifyQueryExecuteCallCount(requestContext, Times.Never(), Times.Never(), Times.Never(), Times.Never(), Times.Once());
            Assert.NotNull(errorResult);
            Assert.IsType<string>(errorResult);
            Assert.DoesNotContain(Common.OwnerUri, queryService.ActiveQueries.Keys);

            // ... There should not be an active query
            Assert.Empty(queryService.ActiveQueries);
        }

        [Fact]
        public async void QueryExecuteInvalidQueryTest()
        {
            // Given:
            // ... A workspace with a standard query is configured
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);

            // If:
            // ... I request to execute a query that is invalid
            var queryService = Common.GetPrimedExecutionService(null, true, true, workspaceService);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QuerySelection = Common.WholeDocument };

            QueryExecuteResult result = null;
            QueryExecuteCompleteParams complete = null;
            QueryExecuteBatchCompleteParams batchComplete = null;
            var requestContext = RequestContextMocks.Create<QueryExecuteResult>(qer => result = qer)
                .AddEventHandling(QueryExecuteCompleteEvent.Type, (et, qecp) => complete = qecp)
                .AddEventHandling(QueryExecuteBatchCompleteEvent.Type, (et, p) => batchComplete = p);
            await Common.AwaitExecution(queryService, queryParams, requestContext.Object);

            // Then:
            // ... No errors should have been sent
            // ... A result should have been sent with success (we successfully started the query)
            // ... A completion event (query, batch, not resultset) should have been sent with error
            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Once(), Times.Never(), Times.Never());
            Assert.Null(result.Messages);

            Assert.Equal(1, complete.BatchSummaries.Length);
            Assert.True(complete.BatchSummaries[0].HasError);
            Assert.NotEmpty(complete.BatchSummaries[0].Messages);

            Assert.NotNull(batchComplete);
            Assert.True(batchComplete.BatchSummary.HasError);
            Assert.NotEmpty(batchComplete.BatchSummary.Messages);
            Assert.Equal(complete.OwnerUri, batchComplete.OwnerUri);
        }

        private static void VerifyQueryExecuteCallCount(Mock<RequestContext<QueryExecuteResult>> mock, Times sendResultCalls,
            Times sendCompletionEventCalls, Times sendBatchCompletionEvent, Times sendResultCompleteEvent, Times sendErrorCalls)
        {
            mock.Verify(rc => rc.SendResult(It.IsAny<QueryExecuteResult>()), sendResultCalls);
            mock.Verify(rc => rc.SendEvent(
                It.Is<EventType<QueryExecuteCompleteParams>>(m => m == QueryExecuteCompleteEvent.Type),
                It.IsAny<QueryExecuteCompleteParams>()), sendCompletionEventCalls);
            mock.Verify(rc => rc.SendEvent(
                It.Is<EventType<QueryExecuteBatchCompleteParams>>(m => m == QueryExecuteBatchCompleteEvent.Type),
                It.IsAny<QueryExecuteBatchCompleteParams>()), sendBatchCompletionEvent);
            mock.Verify(rc => rc.SendEvent(
                It.Is<EventType<QueryExecuteResultSetCompleteParams>>(m => m == QueryExecuteResultSetCompleteEvent.Type),
                It.IsAny<QueryExecuteResultSetCompleteParams>()), sendResultCompleteEvent);

            mock.Verify(rc => rc.SendError(It.IsAny<object>()), sendErrorCalls);
        }
    }
}
