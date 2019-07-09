//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution
{
    public class CancelTests
    {
        [Fact]
        public async Task CancelInProgressQueryTest()
        {
            // If:
            // ... I request a query (doesn't matter what kind) and execute it
            var workspaceService = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, false, workspaceService);
            var executeParams = new ExecuteDocumentSelectionParams { QuerySelection = Common.WholeDocument, OwnerUri = Constants.OwnerUri };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);

            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.WorkTask;
            await queryService.ActiveQueries[Constants.OwnerUri].ExecutionTask;
            queryService.ActiveQueries[Constants.OwnerUri].HasExecuted = false;    // Fake that it hasn't completed execution

            // ... And then I request to cancel the query
            var cancelParams = new QueryCancelParams {OwnerUri = Constants.OwnerUri};
            var cancelRequest = new EventFlowValidator<QueryCancelResult>()
                .AddResultValidation(r =>
                {
                    Assert.Null(r.Messages);
                }).Complete();
            await queryService.HandleCancelRequest(cancelParams, cancelRequest.Object);

            // Then:
            // ... The query should not have been disposed but should have been cancelled
            Assert.Equal(1, queryService.ActiveQueries.Count);
            Assert.Equal(true, queryService.ActiveQueries[Constants.OwnerUri].HasCancelled);
            cancelRequest.Validate();
        }

        [Fact]
        public async Task CancelExecutedQueryTest()
        {
            // If:
            // ... I request a query (doesn't matter what kind) and wait for execution
            var workspaceService = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, false, workspaceService);
            var executeParams = new ExecuteDocumentSelectionParams {QuerySelection = Common.WholeDocument, OwnerUri = Constants.OwnerUri};
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);

            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.WorkTask;
            await queryService.ActiveQueries[Constants.OwnerUri].ExecutionTask;

            // ... And then I request to cancel the query
            var cancelParams = new QueryCancelParams {OwnerUri = Constants.OwnerUri};
            var cancelRequest = new EventFlowValidator<QueryCancelResult>()
                .AddResultValidation(r =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(r.Messages));
                }).Complete();

            await queryService.HandleCancelRequest(cancelParams, cancelRequest.Object);

            // Then:
            // ... The query should not have been disposed and cancel should not have excecuted
            Assert.NotEmpty(queryService.ActiveQueries);
            Assert.Equal(false, queryService.ActiveQueries[Constants.OwnerUri].HasCancelled);
            cancelRequest.Validate();
        }

        [Fact]
        public async Task CancelNonExistantTest()
        {
            // If:
            // ... I request to cancel a query that doesn't exist
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            var queryService = Common.GetPrimedExecutionService(null, false, false, false, workspaceService.Object);

            var cancelParams = new QueryCancelParams { OwnerUri = "Doesn't Exist" };
            var cancelRequest = new EventFlowValidator<QueryCancelResult>()
                .AddResultValidation(r =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(r.Messages));
                }).Complete();
            await queryService.HandleCancelRequest(cancelParams, cancelRequest.Object);
            cancelRequest.Validate();
        }

        [Fact]
        public async Task CancelQueryBeforeExecutionStartedTest()
        {
            // Setup query settings
            QueryExecutionSettings querySettings = new QueryExecutionSettings
            {
                ExecutionPlanOptions = new ExecutionPlanOptions
                {
                    IncludeActualExecutionPlanXml = false,
                    IncludeEstimatedExecutionPlanXml = true
                }
            };

            // Create query with a failure callback function
            ConnectionInfo ci = Common.CreateTestConnectionInfo(null, false, false);
            ConnectionService.Instance.OwnerToConnectionMap[ci.OwnerUri] = ci;
            Query query = new Query(Constants.StandardQuery, ci, querySettings, MemoryFileSystem.GetFileStreamFactory());

            string errorMessage = null;
            Query.QueryAsyncErrorEventHandler failureCallback = async (q, e) =>
            {
                errorMessage = "Error Occured";
            };
            query.QueryFailed += failureCallback;

            query.Cancel();
            query.Execute();
            await query.ExecutionTask;

            // Validate that query has not been executed but cancelled and query failed called function was called
            Assert.Equal(true, query.HasCancelled);
            Assert.Equal(false, query.HasExecuted);
            Assert.Equal("Error Occured", errorMessage);
        }
    }
}
