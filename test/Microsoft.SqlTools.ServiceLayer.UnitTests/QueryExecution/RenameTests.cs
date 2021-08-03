//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution
{
    public class RenameTests
    {
        [Test]
        public async Task RenameExecutedQuery()
        {
            // If:
            // ... I request a query (doesn't matter what kind)
            var workspaceService = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, false, workspaceService);
            var executeParams = new ExecuteDocumentSelectionParams {QuerySelection = null, OwnerUri = Constants.OwnerUri};
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.WorkTask;
            await queryService.ActiveQueries[Constants.OwnerUri].ExecutionTask;
            
            const string newOwnerUri = "newTestFile";
            Query query;
            queryService.ActiveQueries.TryGetValue(Constants.OwnerUri, out query);

            // ... And then I rename the query
            var renameParams = new QueryRenameParams {
                OriginalOwnerUri = Constants.OwnerUri,
                NewOwnerUri = newOwnerUri
            };
            var renameRequest = new EventFlowValidator<QueryRenameResult>()
                .AddStandardQueryRenameValidator()
                .Complete();
            await queryService.HandleRenameRequest(renameParams, renameRequest.Object);

            // Then:
            // ... And the active queries should have the new query.
            renameRequest.Validate();
            Assert.That(queryService.ActiveQueries.TryGetValue(newOwnerUri, out query), "Query with newOwnerUri not found.");
            Assert.That(Equals(query.editorConnection.OwnerUri, newOwnerUri), "OwnerUri was not changed!");
        }

        [Test]
        public async Task RenameMissingQuery()
        {
            // If:
            // ... I attempt to rename a query that doesn't exist
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            var queryService = Common.GetPrimedExecutionService(null, false, false, false, workspaceService.Object);
            const string newOwnerUri = "newTestFile";
            var renameParams = new QueryRenameParams {
                OriginalOwnerUri = Constants.OwnerUri,
                NewOwnerUri = newOwnerUri
            };

            var renameRequest = new EventFlowValidator<QueryRenameResult>()
                .AddStandardErrorValidation()
                .Complete();
            await queryService.HandleRenameRequest(renameParams, renameRequest.Object);

            // Then: I should have received an error
            renameRequest.Validate();
        }
    }

    public static class QueryRenameEventFlowValidatorExtensions
    {
        public static EventFlowValidator<QueryRenameResult> AddStandardQueryRenameValidator(
            this EventFlowValidator<QueryRenameResult> evf)
        {
            // We just need to make sure that the result is not null
            evf.AddResultValidation(Assert.NotNull);

            return evf;
        }
    }
}
