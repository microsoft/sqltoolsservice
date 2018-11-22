//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution
{
    public class SubsetTests
    {
        #region ResultSet Class Tests

        [Theory]
        [InlineData(0, 2)]
        [InlineData(0, 20)]
        [InlineData(1, 2)]
        public void ResultSetValidTest(int startRow, int rowCount)
        {
            // Setup:
            // ... I have a batch that has been executed
            Batch b = Common.GetBasicExecutedBatch();

            // If:
            // ... I have a result set and I ask for a subset with valid arguments
            ResultSet rs = b.ResultSets.First();
            var getSubsetTask = rs.GetSubset(startRow, rowCount);
            getSubsetTask.Wait(); // wait for task to complete
            ResultSetSubset subset = getSubsetTask.Result;

            // Then:
            // ... I should get the requested number of rows back
            Assert.Equal(Math.Min(rowCount, Common.StandardRows), subset.RowCount);
            Assert.Equal(Math.Min(rowCount, Common.StandardRows), subset.Rows.Length);
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

        [Fact]
        public async Task ResultSetNotReadTest()
        {
            // If:
            // ... I have a resultset that hasn't been executed and I request a valid result set from it
            // Then:
            // ... It should throw an exception for having not been read
            ResultSet rs = new ResultSet(Common.Ordinal, Common.Ordinal, MemoryFileSystem.GetFileStreamFactory());
            await Assert.ThrowsAsync<InvalidOperationException>(() => rs.GetSubset(0, 1));
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
            var task = b.GetSubset(0, 0, rowCount);
            task.Wait(); // wait for task to complete
            ResultSetSubset subset = task.Result;

            // Then:
            // I should get the requested number of rows
            Assert.Equal(Math.Min(rowCount, Common.StandardRows), subset.RowCount);
            Assert.Equal(Math.Min(rowCount, Common.StandardRows), subset.Rows.Length);
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
            // If:
            // ... I have a query that has results (doesn't matter what)
            var workspaceService = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(Common.ExecutionPlanTestDataSet, true, false, false, workspaceService);
            var executeParams = new ExecuteDocumentSelectionParams {QuerySelection = null, OwnerUri = Constants.OwnerUri};
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Constants.OwnerUri].ExecutionTask;

            // ... And I then ask for a valid set of results from it
            var subsetParams = new SubsetParams { OwnerUri = Constants.OwnerUri, RowsCount = 1, ResultSetIndex = 0, RowsStartIndex = 0 };
            var subsetRequest = new EventFlowValidator<SubsetResult>()
                .AddResultValidation(r =>
                {
                    // Then: Subset should not be null
                    Assert.NotNull(r.ResultSubset);
                }).Complete();
            await queryService.HandleResultSubsetRequest(subsetParams, subsetRequest.Object);
            subsetRequest.Validate();
        }

        [Fact]
        public async Task SubsetServiceMissingQueryTest()
        {
            // If:
            // ... I ask for a set of results for a file that hasn't executed a query
            var workspaceService = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, false, workspaceService);
            var subsetParams = new SubsetParams { OwnerUri = Constants.OwnerUri, RowsCount = 1, ResultSetIndex = 0, RowsStartIndex = 0 };
            var subsetRequest = new EventFlowValidator<SubsetResult>()
                .AddStandardErrorValidation()
                .Complete();
            await queryService.HandleResultSubsetRequest(subsetParams, subsetRequest.Object);
            subsetRequest.Validate();
        }

        [Fact]
        public async Task SubsetServiceUnexecutedQueryTest()
        {
            // If:
            // ... I have a query that hasn't finished executing (doesn't matter what)
            var workspaceService = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(Common.StandardTestDataSet, true, false, false, workspaceService);
            var executeParams = new ExecuteDocumentSelectionParams { QuerySelection = null, OwnerUri = Constants.OwnerUri };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Constants.OwnerUri].ExecutionTask;
            queryService.ActiveQueries[Constants.OwnerUri].Batches[0].ResultSets[0].hasStartedRead = false;

            // ... And I then ask for a valid set of results from it
            var subsetParams = new SubsetParams { OwnerUri = Constants.OwnerUri, RowsCount = 1, ResultSetIndex = 0, RowsStartIndex = 0 };
            var subsetRequest = new EventFlowValidator<SubsetResult>()
                .AddStandardErrorValidation()
                .Complete();
            await queryService.HandleResultSubsetRequest(subsetParams, subsetRequest.Object);
            subsetRequest.Validate();
        }

        [Fact]
        public async Task SubsetServiceOutOfRangeSubsetTest()
        {
            // If:
            // ... I have a query that doesn't have any result sets
            var workspaceService = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, false, workspaceService);
            var executeParams = new ExecuteDocumentSelectionParams { QuerySelection = null, OwnerUri = Constants.OwnerUri };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Constants.OwnerUri].ExecutionTask;

            // ... And I then ask for a set of results from it
            var subsetParams = new SubsetParams { OwnerUri = Constants.OwnerUri, RowsCount = 1, ResultSetIndex = 0, RowsStartIndex = 0 };
            var subsetRequest = new EventFlowValidator<SubsetResult>()
                .AddStandardErrorValidation()
                .Complete();
            await queryService.HandleResultSubsetRequest(subsetParams, subsetRequest.Object);
            subsetRequest.Validate();
        }

        #endregion
    }
}
