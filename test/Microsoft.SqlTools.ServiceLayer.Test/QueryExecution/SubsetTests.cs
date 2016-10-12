//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class SubsetTests
    {
        #region ResultSet Class Tests

        [Theory]
        [InlineData(0,2)]
        [InlineData(0,20)]
        [InlineData(1,2)]
        public void ResultSetValidTest(int startRow, int rowCount)
        {
            // Setup:
            // ... I have a batch that has been executed
            Batch b = Common.GetBasicExecutedBatch();

            // If:
            // ... I have a result set and I ask for a subset with valid arguments
            ResultSet rs = b.ResultSets.First();
            ResultSetSubset subset = rs.GetSubset(startRow, rowCount).Result;

            // Then:
            // ... I should get the requested number of rows back
            Assert.Equal(Math.Min(rowCount, Common.StandardTestData.Length), subset.RowCount);
            Assert.Equal(Math.Min(rowCount, Common.StandardTestData.Length), subset.Rows.Length);
        }

        [Theory]
        [InlineData(-1, 2)]  // Invalid start index, too low
        [InlineData(10, 2)]  // Invalid start index, too high
        [InlineData(0, -1)]  // Invalid row count, too low
        [InlineData(0, 0)]   // Invalid row count, zero
        public void ResultSetInvalidParmsTest(int rowStartIndex, int rowCount)
        {
            // If:
            // I have an executed batch with a resultset in it and request invalid result set from it
            Batch b = Common.GetBasicExecutedBatch();
            ResultSet rs = b.ResultSets.First();

            // Then:
            // ... It should throw an exception
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => rs.GetSubset(rowStartIndex, rowCount)).Wait();
        }

        #endregion

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
        [InlineData(-1)]  // Invalid result set, too low
        [InlineData(2)]   // Invalid result set, too high
        public void BatchSubsetInvalidParamsTest(int resultSetIndex)
        {
            // If I have an executed batch
            Batch b = Common.GetBasicExecutedBatch();

            // ... And I ask for a subset with an invalid result set index
            // Then: 
            // ... It should throw an exception
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => b.GetSubset(resultSetIndex, 0, 2)).Wait();
        }

        [Fact]
        public async Task BatchSubsetIncompleteTest()
        {
            // If:
            // ... I have a batch that hasn't completed execution
            Batch b = new Batch(Common.StandardQuery, BufferRange.None, Common.Ordinal, Common.GetFileStreamFactory());
            Assert.False(b.HasExecuted);

            // ... And I ask for a subset
            // Then:
            // ... It should throw an exception
            await Assert.ThrowsAsync<InvalidOperationException>(() => b.GetSubset(Common.Ordinal, 0, 2));

        }

        #endregion

        #region Query Class Tests

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

            // Set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns(Common.StandardQuery);
            // Set up workspace mock
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);
            // If:
            // ... I have a query that has results (doesn't matter what)
            var queryService = await Common.GetPrimedExecutionService(
                Common.CreateMockFactory(new[] {Common.StandardTestData}, false), true,
                workspaceService.Object);
            var executeParams = new QueryExecuteParams {QuerySelection = null, OwnerUri = Common.OwnerUri};
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

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
        public async void SubsetServiceMissingQueryTest()
        {

            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            // If:
            // ... I ask for a set of results for a file that hasn't executed a query
            var queryService = await Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true, workspaceService.Object);
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
        public async void SubsetServiceUnexecutedQueryTest()
        {
            
            // Set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns(Common.StandardQuery);
            // Set up workspace mock
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);
            // If:
            // ... I have a query that hasn't finished executing (doesn't matter what)
            var queryService = await Common.GetPrimedExecutionService(
                Common.CreateMockFactory(new[] { Common.StandardTestData }, false), true,
                workspaceService.Object);
            var executeParams = new QueryExecuteParams { QuerySelection = null, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;
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
        public async void SubsetServiceOutOfRangeSubsetTest()
        {            
            // If:
            // ... I have a query that doesn't have any result sets
            var queryService = await Common.GetPrimedExecutionService(
                Common.CreateMockFactory(null, false), true, Common.GetPrimedWorkspaceService());
            var executeParams = new QueryExecuteParams { QuerySelection = null, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

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

        private static Mock<RequestContext<QueryExecuteSubsetResult>> GetQuerySubsetResultContextMock(
            Action<QueryExecuteSubsetResult> resultCallback,
            Action<object> errorCallback)
        {
            return RequestContextMocks.Create(resultCallback)
                .AddErrorHandling(errorCallback);
        }

        private static void VerifyQuerySubsetCallCount(Mock<RequestContext<QueryExecuteSubsetResult>> mock, Times sendResultCalls,
            Times sendErrorCalls)
        {
            mock.Verify(rc => rc.SendResult(It.IsAny<QueryExecuteSubsetResult>()), sendResultCalls);
            mock.Verify(rc => rc.SendError(It.IsAny<object>()), sendErrorCalls);
        }

        #endregion

    }
}
