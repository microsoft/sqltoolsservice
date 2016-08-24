using System;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class ExecuteTests
    {
        #region Query Class Tests

        [Fact]
        public void QueryCreationTest()
        {
            // If I create a new query...
            Query query = new Query("NO OP", Common.CreateTestConnectionInfo(null, false));

            // Then: 
            // ... It should not have executed
            Assert.False(query.HasExecuted, "The query should not have executed.");

            // ... The results should be empty
            Assert.Empty(query.ResultSets);
            Assert.Empty(query.ResultSummary);
        }

        [Fact]
        public void QueryExecuteNoResultSets()
        {
            // If I execute a query that should get no result sets
            Query query = new Query("Query with no result sets", Common.CreateTestConnectionInfo(null, false));
            query.Execute().Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(query.HasExecuted, "The query should have been marked executed.");
            Assert.False(query.HasError);
            
            // ... The results should be empty
            Assert.Empty(query.ResultSets);
            Assert.Empty(query.ResultSummary);

            // ... The results should not be null
            Assert.NotNull(query.ResultSets);
            Assert.NotNull(query.ResultSummary);

            // ... There should be a message for how many rows were affected
            Assert.Equal(1, query.ResultMessages.Count);
        }

        [Fact]
        public void QueryExecuteQueryOneResultSet()
        {
            ConnectionInfo ci = Common.CreateTestConnectionInfo(new[] {Common.StandardTestData}, false);

            // If I execute a query that should get one result set
            int resultSets = 1;
            int rows = 5;
            int columns = 4;
            Query query = new Query("Query with one result sets", ci);
            query.Execute().Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(query.HasExecuted, "The query should have been marked executed.");
            Assert.False(query.HasError);

            // ... There should be exactly one result set
            Assert.Equal(resultSets, query.ResultSets.Count);

            // ... Inside the result set should be with 5 rows
            Assert.Equal(rows, query.ResultSets[0].Rows.Count);

            // ... Inside the result set should have 5 columns and 5 column definitions
            Assert.Equal(columns, query.ResultSets[0].Rows[0].Length);
            Assert.Equal(columns, query.ResultSets[0].Columns.Length);

            // ... There should be exactly one result set summary
            Assert.Equal(resultSets, query.ResultSummary.Length);

            // ... Inside the result summary, there should be 5 column definitions
            Assert.Equal(columns, query.ResultSummary[0].ColumnInfo.Length);

            // ... Inside the result summary, there should be 5 rows
            Assert.Equal(rows, query.ResultSummary[0].RowCount);

            // ... There should be a message for how many rows were affected
            Assert.Equal(resultSets, query.ResultMessages.Count);
        }

        [Fact]
        public void QueryExecuteQueryTwoResultSets()
        {
            var dataset = new[] {Common.StandardTestData, Common.StandardTestData};
            int resultSets = dataset.Length;
            int rows = Common.StandardTestData.Length;
            int columns = Common.StandardTestData[0].Count;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(dataset, false);

            // If I execute a query that should get two result sets
            Query query = new Query("Query with two result sets", ci);
            query.Execute().Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(query.HasExecuted, "The query should have been marked executed.");
            Assert.False(query.HasError);

            // ... There should be exactly two result sets
            Assert.Equal(resultSets, query.ResultSets.Count);

            foreach (ResultSet rs in query.ResultSets)
            {
                // ... Each result set should have 5 rows
                Assert.Equal(rows, rs.Rows.Count);

                // ... Inside each result set should be 5 columns and 5 column definitions
                Assert.Equal(columns, rs.Rows[0].Length);
                Assert.Equal(columns, rs.Columns.Length);
            }

            // ... There should be exactly two result set summaries
            Assert.Equal(resultSets, query.ResultSummary.Length);

            foreach (ResultSetSummary rs in query.ResultSummary)
            {
                // ... Inside each result summary, there should be 5 column definitions
                Assert.Equal(columns, rs.ColumnInfo.Length);

                // ... Inside each result summary, there should be 5 rows
                Assert.Equal(rows, rs.RowCount);
            }

            // ... There should be a message for how many rows were affected
            Assert.Equal(resultSets, query.ResultMessages.Count);
        }

        [Fact]
        public void QueryExecuteInvalidQuery()
        {
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, true);

            // If I execute a query that is invalid
            Query query = new Query("Invalid query", ci);
            query.Execute().Wait();

            // Then:
            // ... It should have executed with error
            Assert.True(query.HasExecuted);
            Assert.True(query.HasError);
            
            // ... There should be plenty of messages for the eror
            Assert.NotEmpty(query.ResultMessages);
        }

        [Fact]
        public void QueryExecuteExecutedQuery()
        {
            ConnectionInfo ci = Common.CreateTestConnectionInfo(new[] {Common.StandardTestData}, false);

            // If I execute a query
            Query query = new Query("Any query", ci);
            query.Execute().Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(query.HasExecuted, "The query should have been marked executed.");
            Assert.False(query.HasError);

            // If I execute it again
            // Then:
            // ... It should throw an invalid operation exception wrapped in an aggregate exception
            AggregateException ae = Assert.Throws<AggregateException>(() => query.Execute().Wait());
            Assert.Equal(1, ae.InnerExceptions.Count);
            Assert.IsType<InvalidOperationException>(ae.InnerExceptions[0]);

            // ... The data should still be available without error
            Assert.False(query.HasError);
            Assert.True(query.HasExecuted, "The query should still be marked executed.");
            Assert.NotEmpty(query.ResultSets);
            Assert.NotEmpty(query.ResultSummary);
        }

        [Theory]
        [InlineData("")]
        [InlineData("     ")]
        [InlineData(null)]
        public void QueryExecuteNoQuery(string query)
        {
            // If:
            // ... I create a query that has an empty query
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() => new Query(query, null));
        }

        [Fact]
        public void QueryExecuteNoConnectionInfo()
        {
            // If:
            // ... I create a query that has a null connection info
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() => new Query("Some Query", null));
        }

        #endregion

        #region Service Tests

        [Fact]
        public void QueryExecuteValidNoResultsTest()
        {
            // If:
            // ... I request to execute a valid query with no results
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var queryParams = new QueryExecuteParams { QueryText = "Doesn't Matter", OwnerUri = Common.OwnerUri };

            QueryExecuteResult result = null;
            QueryExecuteCompleteParams completeParams = null;
            var requestContext = Common.GetQueryExecuteResultContextMock(qer => result = qer, (et, cp) => completeParams = cp, null);
            queryService.HandleExecuteRequest(queryParams, requestContext.Object).Wait();

            // Then:
            // ... No Errors should have been sent
            // ... A successful result should have been sent with messages
            // ... A completion event should have been fired with empty results
            // ... There should be one active query
            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Never());
            Assert.Null(result.Messages);
            Assert.NotEmpty(completeParams.Messages);
            Assert.Empty(completeParams.ResultSetSummaries);
            Assert.False(completeParams.HasError);
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public void QueryExecuteValidResultsTest()
        {
            // If:
            // ... I request to execute a valid query with results
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(new[] { Common.StandardTestData }, false), true);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QueryText = "Doesn't Matter" };

            QueryExecuteResult result = null;
            QueryExecuteCompleteParams completeParams = null;
            var requestContext = Common.GetQueryExecuteResultContextMock(qer => result = qer, (et, cp) => completeParams = cp, null);
            queryService.HandleExecuteRequest(queryParams, requestContext.Object).Wait();

            // Then:
            // ... No errors should have been sent
            // ... A successful result should have been sent with messages
            // ... A completion event should have been fired with one result
            // ... There should be one active query
            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Never());
            Assert.Null(result.Messages);
            Assert.NotEmpty(completeParams.Messages);
            Assert.NotEmpty(completeParams.ResultSetSummaries);
            Assert.False(completeParams.HasError);
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public void QueryExecuteUnconnectedUriTest()
        {
            // If:
            // ... I request to execute a query using a file URI that isn't connected
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), false);
            var queryParams = new QueryExecuteParams { OwnerUri = "notConnected", QueryText = "Doesn't Matter" };

            QueryExecuteResult result = null;
            var requestContext = Common.GetQueryExecuteResultContextMock(qer => result = qer, null, null);
            queryService.HandleExecuteRequest(queryParams, requestContext.Object).Wait();

            // Then:
            // ... An error message should have been returned via the result
            // ... No completion event should have been fired
            // ... No error event should have been fired
            // ... There should be no active queries
            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Never(), Times.Never());
            Assert.NotNull(result.Messages);
            Assert.NotEmpty(result.Messages);
            Assert.Empty(queryService.ActiveQueries);
        }

        [Fact]
        public void QueryExecuteInProgressTest()
        {
            // If:
            // ... I request to execute a query
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QueryText = "Some Query" };

            // Note, we don't care about the results of the first request
            var firstRequestContext = Common.GetQueryExecuteResultContextMock(null, null, null);
            queryService.HandleExecuteRequest(queryParams, firstRequestContext.Object).Wait();

            // ... And then I request another query without waiting for the first to complete
            queryService.ActiveQueries[Common.OwnerUri].HasExecuted = false;   // Simulate query hasn't finished
            QueryExecuteResult result = null;
            var secondRequestContext = Common.GetQueryExecuteResultContextMock(qer => result = qer, null, null);
            queryService.HandleExecuteRequest(queryParams, secondRequestContext.Object).Wait();

            // Then:
            // ... No errors should have been sent
            // ... A result should have been sent with an error message
            // ... No completion event should have been fired
            // ... There should only be one active query
            VerifyQueryExecuteCallCount(secondRequestContext, Times.Once(), Times.AtMostOnce(), Times.Never());
            Assert.NotNull(result.Messages);
            Assert.NotEmpty(result.Messages);
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Fact]
        public void QueryExecuteCompletedTest()
        {
            // If:
            // ... I request to execute a query
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QueryText = "Some Query" };

            // Note, we don't care about the results of the first request
            var firstRequestContext = Common.GetQueryExecuteResultContextMock(null, null, null);
            queryService.HandleExecuteRequest(queryParams, firstRequestContext.Object).Wait();

            // ... And then I request another query after waiting for the first to complete
            QueryExecuteResult result = null;
            QueryExecuteCompleteParams complete = null;
            var secondRequestContext = Common.GetQueryExecuteResultContextMock(qer => result = qer, (et, qecp) => complete = qecp, null);
            queryService.HandleExecuteRequest(queryParams, secondRequestContext.Object).Wait();

            // Then:
            // ... No errors should have been sent
            // ... A result should have been sent with no errors
            // ... There should only be one active query
            VerifyQueryExecuteCallCount(secondRequestContext, Times.Once(), Times.Once(), Times.Never());
            Assert.Null(result.Messages);
            Assert.False(complete.HasError);
            Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void QueryExecuteMissingQueryTest(string query)
        {
            // If:
            // ... I request to execute a query with a missing query string
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QueryText = query };

            QueryExecuteResult result = null;
            var requestContext = Common.GetQueryExecuteResultContextMock(qer => result = qer, null, null);
            queryService.HandleExecuteRequest(queryParams, requestContext.Object).Wait();

            // Then:
            // ... No errors should have been sent
            // ... A result should have been sent with an error message
            // ... No completion event should have been fired
            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Never(), Times.Never());
            Assert.NotNull(result.Messages);
            Assert.NotEmpty(result.Messages);

            // ... There should not be an active query
            Assert.Empty(queryService.ActiveQueries);
        }

        [Fact]
        public void QueryExecuteInvalidQueryTest()
        {
            // If:
            // ... I request to execute a query that is invalid
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, true), true);
            var queryParams = new QueryExecuteParams { OwnerUri = Common.OwnerUri, QueryText = "Bad query!" };

            QueryExecuteResult result = null;
            QueryExecuteCompleteParams complete = null;
            var requestContext = Common.GetQueryExecuteResultContextMock(qer => result = qer, (et, qecp) => complete = qecp, null);
            queryService.HandleExecuteRequest(queryParams, requestContext.Object).Wait();

            // Then:
            // ... No errors should have been sent
            // ... A result should have been sent with success (we successfully started the query)
            // ... A completion event should have been sent with error
            VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Never());
            Assert.Null(result.Messages);
            Assert.True(complete.HasError);
            Assert.NotEmpty(complete.Messages);
        }

        #endregion

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
