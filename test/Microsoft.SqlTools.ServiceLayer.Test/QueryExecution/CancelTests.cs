//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class CancelTests
    {
        [Fact]
        public async void CancelInProgressQueryTest()
        {
            // If:
            // ... I request a query (doesn't matter what kind) and execute it
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var executeParams = new QueryExecuteParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);

            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;
            queryService.ActiveQueries[Common.OwnerUri].HasExecuted = false;    // Fake that it hasn't completed execution

            // ... And then I request to cancel the query
            var cancelParams = new QueryCancelParams {OwnerUri = Common.OwnerUri};
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
        public async void CancelExecutedQueryTest()
        {
            // If:
            // ... I request a query (doesn't matter what kind) and wait for execution
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var executeParams = new QueryExecuteParams {QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri};
            var executeRequest = RequestContextMocks.Create<QueryExecuteResult>(null);

            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // ... And then I request to cancel the query
            var cancelParams = new QueryCancelParams {OwnerUri = Common.OwnerUri};
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
            var queryService = Common.GetPrimedExecutionService(null, false, false, workspaceService.Object);

            var cancelParams = new QueryCancelParams { OwnerUri = "Doesn't Exist" };
            var cancelRequest = new EventFlowValidator<QueryCancelResult>()
                .AddResultValidation(r =>
                {
                    Assert.Null(r.Messages);
                }).Complete();
            await queryService.HandleCancelRequest(cancelParams, cancelRequest.Object);
            cancelRequest.Validate();
        }
    }
}
