//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class SubsetTests
    {
        #region Batch Class Tests

        [Theory]
        [InlineData(2)]
        [InlineData(20)]
        public void BatchSubsetValidTest(int rowCount)
        {
            // If I have an executed batch
            Batch b = Common.GetBasicExecutedBatch();

            // ... And I ask for a subset with valid arguments
            ResultSetSubset subset = b.GetSubset(0, 0, rowCount).Result;

            // Then:
            // I should get the requested number of rows
            Assert.Equal(Math.Min(rowCount, Common.StandardTestData.Length), subset.RowCount);
            Assert.Equal(Math.Min(rowCount, Common.StandardTestData.Length), subset.Rows.Length);
        }

        [Theory]
        [InlineData(-1, 0, 2)]  // Invalid result set, too low
        [InlineData(2, 0, 2)]   // Invalid result set, too high
        [InlineData(0, -1, 2)]  // Invalid start index, too low
        [InlineData(0, 10, 2)]  // Invalid start index, too high
        [InlineData(0, 0, -1)]  // Invalid row count, too low
        [InlineData(0, 0, 0)]   // Invalid row count, zero
        public void BatchSubsetInvalidParamsTest(int resultSetIndex, int rowStartInex, int rowCount)
        {
            // If I have an executed batch
            Batch b = Common.GetBasicExecutedBatch();

            // ... And I ask for a subset with an invalid result set index
            // Then: 
            // ... It should throw an exception
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => b.GetSubset(resultSetIndex, rowStartInex, rowCount)).Wait();
        }

        #endregion

        #region Query Class Tests

        [Fact]
        public void SubsetUnexecutedQueryTest()
        {
            // If I have a query that has *not* been executed
            Query q = new Query(Common.StandardQuery, Common.CreateTestConnectionInfo(null, false), new QueryExecutionSettings(), Common.GetFileStreamFactory());

            // ... And I ask for a subset with valid arguments
            // Then:
            // ... It should throw an exception
            Assert.ThrowsAsync<InvalidOperationException>(() => q.GetSubset(0, 0, 0, 2)).Wait();
        }

        [Theory]
        [InlineData(-1)]  // Invalid batch, too low
        [InlineData(2)]   // Invalid batch, too high
        public void QuerySubsetInvalidParamsTest(int batchIndex)
        {
            // If I have an executed query
            Query q = Common.GetBasicExecutedQuery();

            // ... And I ask for a subset with an invalid result set index
            // Then: 
            // ... It should throw an exception
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => q.GetSubset(batchIndex, 0, 0, 1)).Wait();
        }

        #endregion

        #region Service Intergration Tests

        [Fact]
        public async Task SubsetServiceValidTest()
        {
            // If:
            // ... I have a query that has results (doesn't matter what)
            var queryService =Common.GetPrimedExecutionService(
                Common.CreateMockFactory(new[] {Common.StandardTestData}, false), true);
            var executeParams = new QueryExecuteParams {QuerySelection = null, OwnerUri = Common.OwnerUri};
            var executeRequest = RequestContextMocks.SetupRequestContextMock<QueryExecuteResult, QueryExecuteCompleteParams>(null, QueryExecuteCompleteEvent.Type, null, null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);

            // ... And I then ask for a valid set of results from it
            var subsetParams = new QueryExecuteSubsetParams {OwnerUri = Common.OwnerUri, RowsCount = 1, ResultSetIndex = 0, RowsStartIndex = 0};
            QueryExecuteSubsetResult result = null;
            var subsetRequest = GetQuerySubsetResultContextMock(qesr => result = qesr, null);
            await queryService.HandleResultSubsetRequest(subsetParams, subsetRequest.Object);

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
            var executeParams = new QueryExecuteParams { QuerySelection = null, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.SetupRequestContextMock<QueryExecuteResult, QueryExecuteCompleteParams>(null, QueryExecuteCompleteEvent.Type, null, null);
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
            var executeParams = new QueryExecuteParams { QuerySelection = null, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.SetupRequestContextMock<QueryExecuteResult, QueryExecuteCompleteParams>(null, QueryExecuteCompleteEvent.Type, null, null);
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
