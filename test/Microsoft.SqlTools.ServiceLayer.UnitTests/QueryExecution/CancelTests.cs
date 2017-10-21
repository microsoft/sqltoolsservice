//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
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
            // ... The query should not have been disposed
            Assert.Equal(1, queryService.ActiveQueries.Count);
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
            // ... The query should not have been disposed
            Assert.NotEmpty(queryService.ActiveQueries);
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
    }
}
