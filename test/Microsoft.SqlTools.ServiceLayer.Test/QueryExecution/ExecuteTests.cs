using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class ExecuteTests
    {
        #region Batch Class Tests

        [Fact]
        public void BatchCreationTest()
        {
            // If I create a new batch...
            Batch batch = new Batch("NO OP");

            // Then: 
            // ... The text of the batch should be stored
            Assert.NotEmpty(batch.BatchText);

            // ... It should not have executed and no error
            Assert.False(batch.HasExecuted, "The query should not have executed.");
            Assert.False(batch.HasError, "The batch should not have an error");

            // ... The results should be empty
            Assert.Empty(batch.ResultSets);
            Assert.Empty(batch.ResultSummaries);
            Assert.Empty(batch.ResultMessages);
        }

        [Fact]
        public void BatchExecuteNoResultSets()
        {
            // If I execute a query that should get no result sets
            Batch batch = new Batch("Query with no result sets");
            batch.Execute(GetConnection(Common.CreateTestConnectionInfo(null, false)), CancellationToken.None).Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(batch.HasExecuted, "The query should have been marked executed.");
            Assert.False(batch.HasError, "The batch should not have an error");

            // ... The results should be empty
            Assert.Empty(batch.ResultSets);
            Assert.Empty(batch.ResultSummaries);

            // ... The results should not be null
            Assert.NotNull(batch.ResultSets);
            Assert.NotNull(batch.ResultSummaries);

            // ... There should be a message for how many rows were affected
            Assert.Equal(1, batch.ResultMessages.Count);
        }

        [Fact]
        public void BatchExecuteOneResultSet()
        {
            int resultSets = 1;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(new[] { Common.StandardTestData }, false);

            // If I execute a query that should get one result set
            Batch batch = new Batch("Query with one result sets");
            batch.Execute(GetConnection(ci), CancellationToken.None).Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(batch.HasExecuted, "The batch should have been marked executed.");
            Assert.False(batch.HasError, "The batch should not have an error");

            // ... There should be exactly one result set
            Assert.Equal(resultSets, batch.ResultSets.Count);
            Assert.Equal(resultSets, batch.ResultSummaries.Length);

            // ... Inside the result set should be with 5 rows
            Assert.Equal(Common.StandardRows, batch.ResultSets[0].Rows.Count);
            Assert.Equal(Common.StandardRows, batch.ResultSummaries[0].RowCount);

            // ... Inside the result set should have 5 columns and 5 column definitions
            Assert.Equal(Common.StandardColumns, batch.ResultSets[0].Rows[0].Length);
            Assert.Equal(Common.StandardColumns, batch.ResultSets[0].Columns.Length);
            Assert.Equal(Common.StandardColumns, batch.ResultSummaries[0].ColumnInfo.Length);

            // ... There should be a message for how many rows were affected
            Assert.Equal(resultSets, batch.ResultMessages.Count);
        }

        [Fact]
        public void BatchExecuteTwoResultSets()
        {
            var dataset = new[] { Common.StandardTestData, Common.StandardTestData };
            int resultSets = dataset.Length;
            ConnectionInfo ci = Common.CreateTestConnectionInfo(dataset, false);

            // If I execute a query that should get two result sets
            Batch batch = new Batch("Query with two result sets");
            batch.Execute(GetConnection(ci), CancellationToken.None).Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(batch.HasExecuted, "The batch should have been marked executed.");
            Assert.False(batch.HasError, "The batch should not have an error");

            // ... There should be exactly two result sets
            Assert.Equal(resultSets, batch.ResultSets.Count);

            foreach (ResultSet rs in batch.ResultSets)
            {
                // ... Each result set should have 5 rows
                Assert.Equal(Common.StandardRows, rs.Rows.Count);

                // ... Inside each result set should be 5 columns and 5 column definitions
                Assert.Equal(Common.StandardColumns, rs.Rows[0].Length);
                Assert.Equal(Common.StandardColumns, rs.Columns.Length);
            }

            // ... There should be exactly two result set summaries
            Assert.Equal(resultSets, batch.ResultSummaries.Length);

            foreach (ResultSetSummary rs in batch.ResultSummaries)
            {
                // ... Inside each result summary, there should be 5 rows
                Assert.Equal(Common.StandardRows, rs.RowCount);

                // ... Inside each result summary, there should be 5 column definitions
                Assert.Equal(Common.StandardColumns, rs.ColumnInfo.Length);
            }

            // ... There should be a message for how many rows were affected
            Assert.Equal(resultSets, batch.ResultMessages.Count);
        }

        [Fact]
        public void BatchExecuteInvalidQuery()
        {
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, true);

            // If I execute a batch that is invalid
            Batch batch = new Batch("Invalid query");
            batch.Execute(GetConnection(ci), CancellationToken.None).Wait();

            // Then:
            // ... It should have executed with error
            Assert.True(batch.HasExecuted);
            Assert.True(batch.HasError);

            // ... There should be no result sets
            Assert.Empty(batch.ResultSets);
            Assert.Empty(batch.ResultSummaries);

            // ... There should be plenty of messages for the error
            Assert.NotEmpty(batch.ResultMessages);
        }

        [Fact]
        public async Task BatchExecuteExecuted()
        {
            ConnectionInfo ci = Common.CreateTestConnectionInfo(new[] { Common.StandardTestData }, false);

            // If I execute a batch
            Batch batch = new Batch("Any query");
            batch.Execute(GetConnection(ci), CancellationToken.None).Wait();

            // Then:
            // ... It should have executed without error
            Assert.True(batch.HasExecuted, "The batch should have been marked executed.");
            Assert.False(batch.HasError, "The batch should not have an error");

            // If I execute it again
            // Then:
            // ... It should throw an invalid operation exception
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                batch.Execute(GetConnection(ci), CancellationToken.None));

            // ... The data should still be available without error
            Assert.False(batch.HasError, "The batch should not be in an error condition");
            Assert.True(batch.HasExecuted, "The batch should still be marked executed.");
            Assert.NotEmpty(batch.ResultSets);
            Assert.NotEmpty(batch.ResultSummaries);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void BatchExecuteNoSql(string query)
        {
            // If:
            // ... I create a batch that has an empty query
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() => new Batch(query));
        }

        #endregion

        #region Query Class Tests

        [Fact]
        public void QueryExecuteNoConnectionInfo()
        {
            // If:
            // ... I create a query that has a null connection info
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() => new Query("Some Query", null, new QueryExecutionSettings()));
        }

        [Fact]
        public void QueryExecuteNoSettings()
        {
            // If:
            // ... I create a query that has a null settings
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() =>
                new Query("Some query", Common.CreateTestConnectionInfo(null, false), null));
        }

        #endregion

        #region Service Tests

        //[Fact]
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
            //VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Never());
            //Assert.Null(result.Messages);
            //Assert.NotEmpty(completeParams.Messages);
            //Assert.Equal(1, completeParams.BatchSummaries);
            //Assert.True(completeParams.);
            //Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        //[Fact]
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
            //VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Never());
            //Assert.Null(result.Messages);
            //Assert.NotEmpty(completeParams.Messages);
            //Assert.NotEmpty(completeParams.ResultSetSummaries);
            //Assert.False(completeParams.HasError);
            //Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        //[Fact]
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
            //VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Never(), Times.Never());
            //Assert.NotNull(result.Messages);
            //Assert.NotEmpty(result.Messages);
            //Assert.Empty(queryService.ActiveQueries);
        }

        //[Fact]
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
            //queryService.ActiveQueries[Common.OwnerUri].HasExecuted = false;   // Simulate query hasn't finished
            //QueryExecuteResult result = null;
            //var secondRequestContext = Common.GetQueryExecuteResultContextMock(qer => result = qer, null, null);
            //queryService.HandleExecuteRequest(queryParams, secondRequestContext.Object).Wait();

            //// Then:
            //// ... No errors should have been sent
            //// ... A result should have been sent with an error message
            //// ... No completion event should have been fired
            //// ... There should only be one active query
            //VerifyQueryExecuteCallCount(secondRequestContext, Times.Once(), Times.AtMostOnce(), Times.Never());
            //Assert.NotNull(result.Messages);
            //Assert.NotEmpty(result.Messages);
            //Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        //[Fact]
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
            //VerifyQueryExecuteCallCount(secondRequestContext, Times.Once(), Times.Once(), Times.Never());
            //Assert.Null(result.Messages);
            //Assert.False(complete.HasError);
            //Assert.Equal(1, queryService.ActiveQueries.Count);
        }

        //[Theory]
        //[InlineData("")]
        //[InlineData(null)]
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
            //VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Never(), Times.Never());
            //Assert.NotNull(result.Messages);
            //Assert.NotEmpty(result.Messages);
        }

        //[Fact]
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
            //VerifyQueryExecuteCallCount(requestContext, Times.Once(), Times.Once(), Times.Never());
            //Assert.Null(result.Messages);
            //Assert.True(complete.HasError);
            //Assert.NotEmpty(complete.Messages);
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

        private DbConnection GetConnection(ConnectionInfo info)
        {
            return info.Factory.CreateSqlConnection(ConnectionService.BuildConnectionString(info.ConnectionDetails));
        }
    }
}
