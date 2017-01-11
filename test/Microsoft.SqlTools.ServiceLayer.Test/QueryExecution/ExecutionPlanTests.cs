//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class ExecutionPlanTests
    {
        #region ResultSet Class Tests

        [Fact]
        public void ExecutionPlanValid()
        {
            // Setup:
            // ... I have a batch that has been executed with a execution plan 
            Batch b = Common.GetExecutedBatchWithExecutionPlan();

            // If:
            // ... I have a result set and I ask for a valid execution plan
            ResultSet planResultSet = b.ResultSets.First();
            ExecutionPlan plan = planResultSet.GetExecutionPlan().Result;

            // Then:
            // ... I should get the execution plan back
            Assert.Equal("xml", plan.Format);
            Assert.Contains("Execution Plan", plan.Content);
        }

        [Fact]
        public void ExecutionPlanInvalid()
        {
            // Setup:
            // ... I have a batch that has been executed
            Batch b = Common.GetBasicExecutedBatch();

            // If:
            // ... I have a result set and I ask for an execution plan that doesn't exist
            ResultSet planResultSet = b.ResultSets.First();
            ExecutionPlan plan = planResultSet.GetExecutionPlan().Result;

            // Then:
            // ... I should get a null value since it has no execution plan 
            Assert.Null(plan);
        }

        #endregion

        #region Batch Class Tests

        [Fact]
        public void BatchExecutionPlanValidTest()
        {
            // If I have an executed batch which has an execution plan 
            Batch b = Common.GetExecutedBatchWithExecutionPlan();

            // ... And I ask for a valid execution plan 
            ExecutionPlan plan = b.GetExecutionPlan(0).Result;

            // Then:
            // ... I should get the execution plan back
            Assert.Equal("xml", plan.Format);
            Assert.Contains("Execution Plan", plan.Content);
        }

        [Fact]
        public void BatchExecutionPlanInvalidTest()
        {
            // Setup:
            // ... I have a batch that has been executed without an execution plan 
            Batch b = Common.GetBasicExecutedBatch();

            // If: 
            // ... I ask for an invalid execution plan 
            ExecutionPlan plan = b.GetExecutionPlan(0).Result;

            // Then:
            // ... I should get a null value since it has no execution plan 
            Assert.Null(plan);
        }

        [Theory]
        [InlineData(-1)]  // Invalid result set, too low
        [InlineData(2)]   // Invalid result set, too high
        public void BatchExecutionPlanInvalidParamsTest(int resultSetIndex)
        {
            // If I have an executed batch which has an execution plan 
            Batch b = Common.GetExecutedBatchWithExecutionPlan();

            // ... And I ask for an execution plan with an invalid result set index
            // Then: 
            // ... It should throw an exception
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => b.GetExecutionPlan(resultSetIndex)).Wait();
        }

        #endregion

        #region Query Class Tests

        [Theory]
        [InlineData(-1)]  // Invalid batch, too low
        [InlineData(2)]   // Invalid batch, too high
        public void QueryExecutionPlanInvalidParamsTest(int batchIndex)
        {
            // Setup query settings
            QueryExecutionSettings querySettings = new QueryExecutionSettings(); 
            querySettings.ExecutionPlanOptions = new ExecutionPlanOptions()
            { 
                IncludeActualExecutionPlanXml = false,
                IncludeEstimatedExecutionPlanXml = true
            };

            // If I have an executed query
            Query q = Common.GetBasicExecutedQuery(querySettings);

            // ... And I ask for a subset with an invalid result set index
            // Then: 
            // ... It should throw an exception
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => q.GetExecutionPlan(batchIndex, 0)).Wait();
        }

        #endregion


        #region Service Intergration Tests

        [Fact]
        public async Task ExecutionPlanServiceValidTest()
        {
            // If:
            // ... I have a query that has results in the form of an execution plan 
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(new[] {Common.GetExecutionPlanTestData()}, true, false, workspaceService);
            var executeParams = new QueryExecuteParams {QuerySelection = null, OwnerUri = Common.OwnerUri};
            executeParams.ExecutionPlanOptions = new ExecutionPlanOptions()
            { 
                IncludeActualExecutionPlanXml = false,
                IncludeEstimatedExecutionPlanXml = true
            };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // ... And I then ask for a valid execution plan 
            var executionPlanParams = new QueryExecutionPlanParams { OwnerUri = Common.OwnerUri, BatchIndex = 0, ResultSetIndex = 0 };
            var executionPlanRequest = new EventFlowValidator<QueryExecutionPlanResult>()
                .AddResultValidation(r =>
                {
                    // Then: Messages should be null and execution plan should not be null
                    Assert.Null(r.Message);
                    Assert.NotNull(r.ExecutionPlan);
                }).Complete();
            await queryService.HandleExecutionPlanRequest(executionPlanParams, executionPlanRequest.Object);
            executionPlanRequest.Validate();
        }

        
        [Fact]
        public async Task ExecutionPlanServiceMissingQueryTest()
        {
            // If:
            // ... I ask for an execution plan for a file that hasn't executed a query
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var executionPlanParams = new QueryExecutionPlanParams { OwnerUri = Common.OwnerUri, ResultSetIndex = 0, BatchIndex = 0 };
            var executionPlanRequest = new EventFlowValidator<QueryExecutionPlanResult>()
                .AddResultValidation(r =>
                {
                    // Then: Messages should not be null and the execution plan should be null
                    Assert.NotNull(r.Message);
                    Assert.Null(r.ExecutionPlan);
                }).Complete();
            await queryService.HandleExecutionPlanRequest(executionPlanParams, executionPlanRequest.Object);
            executionPlanRequest.Validate();
        }

        [Fact]
        public async Task ExecutionPlanServiceUnexecutedQueryTest()
        {
            // If:
            // ... I have a query that hasn't finished executing (doesn't matter what)
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(new[] { Common.GetExecutionPlanTestData() }, true, false, workspaceService);
            var executeParams = new QueryExecuteParams { QuerySelection = null, OwnerUri = Common.OwnerUri };
            executeParams.ExecutionPlanOptions = new ExecutionPlanOptions()
            { 
                IncludeActualExecutionPlanXml = false,
                IncludeEstimatedExecutionPlanXml = true
            };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;
            queryService.ActiveQueries[Common.OwnerUri].Batches[0].ResultSets[0].hasBeenRead = false;

            // ... And I then ask for a valid execution plan from it 
            var executionPlanParams = new QueryExecutionPlanParams { OwnerUri = Common.OwnerUri, ResultSetIndex = 0, BatchIndex = 0 };
            var executionPlanRequest = new EventFlowValidator<QueryExecutionPlanResult>()
                .AddResultValidation(r =>
                {
                    // Then: There should not be an execution plan and message should not be null
                    Assert.NotNull(r.Message);
                    Assert.Null(r.ExecutionPlan);
                }).Complete();
            await queryService.HandleExecutionPlanRequest(executionPlanParams, executionPlanRequest.Object);
            executionPlanRequest.Validate();
        }

        [Fact]
        public async Task ExecutionPlanServiceOutOfRangeSubsetTest()
        {
            // If:
            // ... I have a query that doesn't have any result sets
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var executeParams = new QueryExecuteParams { QuerySelection = null, OwnerUri = Common.OwnerUri };
            executeParams.ExecutionPlanOptions = new ExecutionPlanOptions()
            { 
                IncludeActualExecutionPlanXml = false,
                IncludeEstimatedExecutionPlanXml = true
            };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // ... And I then ask for an execution plan from a result set 
            var executionPlanParams = new QueryExecutionPlanParams { OwnerUri = Common.OwnerUri, ResultSetIndex = 0, BatchIndex = 0 };
            var executionPlanRequest = new EventFlowValidator<QueryExecutionPlanResult>()
                .AddResultValidation(r =>
                {
                    // Then: There should be an error message and no execution plan
                    Assert.NotNull(r.Message);
                    Assert.Null(r.ExecutionPlan);
                }).Complete();
            await queryService.HandleExecutionPlanRequest(executionPlanParams, executionPlanRequest.Object);
            executionPlanRequest.Validate();
        }
        
        #endregion
        
    }
}
