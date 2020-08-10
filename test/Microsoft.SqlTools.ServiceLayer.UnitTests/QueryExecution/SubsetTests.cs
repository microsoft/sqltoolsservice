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
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution
{
    public class SubsetTests
    {
        #region ResultSet Class Tests

        static private readonly object[] validSet =
        {
            new object[] {0, 2 },
            new object[] {0, 20 },
            new object[] {1, 2 },
        };

        [Test, TestCaseSource(nameof(validSet))]
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
            Assert.AreEqual(Math.Min(rowCount, Common.StandardRows), subset.RowCount);
            Assert.AreEqual(Math.Min(rowCount, Common.StandardRows), subset.Rows.Length);
        }

        static private readonly object[] invalidSet =
        {
            new object[] {-1, 2},  // Invalid start index, too low
            new object[] {10, 2}, // Invalid start index, too high
            new object[] {0, -1}, // Invalid row count, too low
            new object[] {0, 0 },   // Invalid row count, zero
        };
        [Test, TestCaseSource(nameof(invalidSet))]
        public void ResultSetInvalidParmsTest(int rowStartIndex, int rowCount)
        {
            // If:
            // I have an executed batch with a resultset in it and request invalid result set from it
            Batch b = Common.GetBasicExecutedBatch();
            ResultSet rs = b.ResultSets.First();

            // Then:
            // ... It should throw an exception
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => rs.GetSubset(rowStartIndex, rowCount));
        }

        [Test]
        public void ResultSetNotReadTest()
        {
            // If:
            // ... I have a resultset that hasn't been executed and I request a valid result set from it
            // Then:
            // ... It should throw an exception for having not been read
            ResultSet rs = new ResultSet(Common.Ordinal, Common.Ordinal, MemoryFileSystem.GetFileStreamFactory());
            Assert.ThrowsAsync<InvalidOperationException>(() => rs.GetSubset(0, 1));
        }

        #endregion

        #region Batch Class Tests

        [Test]
        public void BatchSubsetValidTest([Values(2,20)] int rowCount)
        {
            // If I have an executed batch
            Batch b = Common.GetBasicExecutedBatch();

            // ... And I ask for a subset with valid arguments
            var task = b.GetSubset(0, 0, rowCount);
            task.Wait(); // wait for task to complete
            ResultSetSubset subset = task.Result;

            // Then:
            // I should get the requested number of rows
            Assert.AreEqual(Math.Min(rowCount, Common.StandardRows), subset.RowCount);
            Assert.AreEqual(Math.Min(rowCount, Common.StandardRows), subset.Rows.Length);
        }

        [Test]
        public void BatchSubsetInvalidParamsTest([Values(-1,2)] int resultSetIndex)
        {
            // If I have an executed batch
            Batch b = Common.GetBasicExecutedBatch();

            // ... And I ask for a subset with an invalid result set index
            // Then: 
            // ... It should throw an exception
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => b.GetSubset(resultSetIndex, 0, 2));
        }

        #endregion

        #region Query Class Tests

        [Test]
        public void QuerySubsetInvalidParamsTest([Values(-1,2)] int batchIndex)
        {
            // If I have an executed query
            Query q = Common.GetBasicExecutedQuery();

            // ... And I ask for a subset with an invalid result set index
            // Then: 
            // ... It should throw an exception
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => q.GetSubset(batchIndex, 0, 0, 1));
        }

        #endregion

        #region Service Intergration Tests

        [Test]
        public async Task SubsetServiceValidTest()
        {
            // If:
            // ... I have a query that has results (doesn't matter what)
            var workspaceService = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(Common.ExecutionPlanTestDataSet, true, false, false, workspaceService);
            var executeParams = new ExecuteDocumentSelectionParams {QuerySelection = null, OwnerUri = Constants.OwnerUri};
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.WorkTask;
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

        [Test]
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

        [Test]
        public async Task SubsetServiceUnexecutedQueryTest()
        {
            // If:
            // ... I have a query that hasn't finished executing (doesn't matter what)
            var workspaceService = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(Common.StandardTestDataSet, true, false, false, workspaceService);
            var executeParams = new ExecuteDocumentSelectionParams { QuerySelection = null, OwnerUri = Constants.OwnerUri };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.WorkTask;
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

        [Test]
        public async Task SubsetServiceOutOfRangeSubsetTest()
        {
            // If:
            // ... I have a query that doesn't have any result sets
            var workspaceService = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, false, workspaceService);
            var executeParams = new ExecuteDocumentSelectionParams { QuerySelection = null, OwnerUri = Constants.OwnerUri };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.WorkTask;
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
