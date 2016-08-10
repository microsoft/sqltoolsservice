using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class SubsetTests
    {
        #region Query Class Tests

        [Theory]
        [InlineData(2)]
        [InlineData(20)]
        public void SubsetValidTest(int rowCount)
        {
            // If I have an executed query
            Query q = Common.GetBasicExecutedQuery();

            // ... And I ask for a subset with valid arguments
            ResultSetSubset subset = q.GetSubset(0, 0, rowCount);

            // Then:
            // I should get the requested number of rows
            Assert.Equal(Math.Min(rowCount, Common.StandardTestData.Length), subset.RowCount);
            Assert.Equal(Math.Min(rowCount, Common.StandardTestData.Length), subset.Rows.Length);
        }

        [Fact]
        public void SubsetUnexecutedQueryTest()
        {
            // If I have a query that has *not* been executed
            Query q = new Query("NO OP", Common.CreateTestConnectionInfo(null, false));

            // ... And I ask for a subset with valid arguments
            // Then:
            // ... It should throw an exception
            Assert.Throws<InvalidOperationException>(() => q.GetSubset(0, 0, 2));
        }

        [Theory]
        [InlineData(-1, 0, 2)]  // Invalid result set, too low
        [InlineData(2, 0, 2)]   // Invalid result set, too high
        [InlineData(0, -1, 2)]  // Invalid start index, too low
        [InlineData(0, 10, 2)]  // Invalid start index, too high
        [InlineData(0, 0, -1)]  // Invalid row count, too low
        [InlineData(0, 0, 0)]   // Invalid row count, zero
        public void SubsetInvalidParamsTest(int resultSetIndex, int rowStartInex, int rowCount)
        {
            // If I have an executed query
            Query q = Common.GetBasicExecutedQuery();

            // ... And I ask for a subset with an invalid result set index
            // Then: 
            // ... It should throw an exception
            Assert.Throws<ArgumentOutOfRangeException>(() => q.GetSubset(resultSetIndex, rowStartInex, rowCount));
        }

        #endregion

        #region Service Intergration Tests

        [Fact]
        public void SubsetServiceValidTest()
        {
            // If:
            // ... I have a query that has results (doesn't matter what)
            var queryService =Common.GetPrimedExecutionService(
                Common.CreateMockFactory(new[] {Common.StandardTestData}, false), true);
            var executeParams = new QueryExecuteParams {QueryText = "Doesn'tMatter", OwnerUri = Common.OwnerUri};
            var executeRequest = Common.GetQueryExecuteResultContextMock(null, null, null);
            queryService.HandleExecuteRequest(executeParams, executeRequest.Object).Wait();

            // ... And I then ask for a valid set of results from it
            var subsetParams = new QueryExecuteSubsetParams {OwnerUri = Common.OwnerUri, RowsCount = 1, ResultSetIndex = 0, RowsStartIndex = 0};
            QueryExecuteSubsetResult result = null;
            var subsetRequest = GetQuerySubsetResultContextMock(qesr => result = qesr, null);
            queryService.HandleResultSubsetRequest(subsetParams, subsetRequest.Object).Wait();

            // Then:
            // ... I should have a successful result
            // ... There should be rows there (other test validate that the rows are correct)
            // ... There should not be any error calls
            VerifyQuerySubsetCallCount(subsetRequest, Times.Once(), Times.Never());
            Assert.Null(result.Message);
            Assert.NotNull(result.ResultSubset);
        }

        [Fact]
        public void SubsetServiceMissingQueryTest()
        {
            // If:
            // ... I ask for a set of results for a file that hasn't executed a query
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var subsetParams = new QueryExecuteSubsetParams { OwnerUri = Common.OwnerUri, RowsCount = 1, ResultSetIndex = 0, RowsStartIndex = 0 };
            QueryExecuteSubsetResult result = null;
            var subsetRequest = GetQuerySubsetResultContextMock(qesr => result = qesr, null);
            queryService.HandleResultSubsetRequest(subsetParams, subsetRequest.Object).Wait();

            // Then:
            // ... I should have an error result
            // ... There should be no rows in the result set
            // ... There should not be any error calls
            VerifyQuerySubsetCallCount(subsetRequest, Times.Once(), Times.Never());
            Assert.NotNull(result.Message);
            Assert.Null(result.ResultSubset);
        }

        [Fact]
        public void SubsetServiceUnexecutedQueryTest()
        {
            // If:
            // ... I have a query that hasn't finished executing (doesn't matter what)
            var queryService = Common.GetPrimedExecutionService(
                Common.CreateMockFactory(new[] { Common.StandardTestData }, false), true);
            var executeParams = new QueryExecuteParams { QueryText = "Doesn'tMatter", OwnerUri = Common.OwnerUri };
            var executeRequest = Common.GetQueryExecuteResultContextMock(null, null, null);
            queryService.HandleExecuteRequest(executeParams, executeRequest.Object).Wait();
            queryService.ActiveQueries[Common.OwnerUri].HasExecuted = false;

            // ... And I then ask for a valid set of results from it
            var subsetParams = new QueryExecuteSubsetParams { OwnerUri = Common.OwnerUri, RowsCount = 1, ResultSetIndex = 0, RowsStartIndex = 0 };
            QueryExecuteSubsetResult result = null;
            var subsetRequest = GetQuerySubsetResultContextMock(qesr => result = qesr, null);
            queryService.HandleResultSubsetRequest(subsetParams, subsetRequest.Object).Wait();

            // Then:
            // ... I should get an error result
            // ... There should not be rows 
            // ... There should not be any error calls
            VerifyQuerySubsetCallCount(subsetRequest, Times.Once(), Times.Never());
            Assert.NotNull(result.Message);
            Assert.Null(result.ResultSubset);
        }

        [Fact]
        public void SubsetServiceOutOfRangeSubsetTest()
        {
            // If:
            // ... I have a query that doesn't have any result sets
            var queryService = Common.GetPrimedExecutionService(
                Common.CreateMockFactory(null, false), true);
            var executeParams = new QueryExecuteParams { QueryText = "Doesn'tMatter", OwnerUri = Common.OwnerUri };
            var executeRequest = Common.GetQueryExecuteResultContextMock(null, null, null);
            queryService.HandleExecuteRequest(executeParams, executeRequest.Object).Wait();

            // ... And I then ask for a set of results from it
            var subsetParams = new QueryExecuteSubsetParams { OwnerUri = Common.OwnerUri, RowsCount = 1, ResultSetIndex = 0, RowsStartIndex = 0 };
            QueryExecuteSubsetResult result = null;
            var subsetRequest = GetQuerySubsetResultContextMock(qesr => result = qesr, null);
            queryService.HandleResultSubsetRequest(subsetParams, subsetRequest.Object).Wait();

            // Then:
            // ... I should get an error result
            // ... There should not be rows 
            // ... There should not be any error calls
            VerifyQuerySubsetCallCount(subsetRequest, Times.Once(), Times.Never());
            Assert.NotNull(result.Message);
            Assert.Null(result.ResultSubset);
        }

        #endregion

        #region Mocking

        private Mock<RequestContext<QueryExecuteSubsetResult>> GetQuerySubsetResultContextMock(
            Action<QueryExecuteSubsetResult> resultCallback,
            Action<object> errorCallback)
        {
            var requestContext = new Mock<RequestContext<QueryExecuteSubsetResult>>();

            // Setup the mock for SendResult
            var sendResultFlow = requestContext
                .Setup(rc => rc.SendResult(It.IsAny<QueryExecuteSubsetResult>()))
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

        private void VerifyQuerySubsetCallCount(Mock<RequestContext<QueryExecuteSubsetResult>> mock, Times sendResultCalls,
            Times sendErrorCalls)
        {
            mock.Verify(rc => rc.SendResult(It.IsAny<QueryExecuteSubsetResult>()), sendResultCalls);
            mock.Verify(rc => rc.SendError(It.IsAny<object>()), sendErrorCalls);
        }

        #endregion

    }
}
