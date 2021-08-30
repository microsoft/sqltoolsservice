//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
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
    public class ConnectionUriChangedTests
    {
        [Test]
        public async Task ChangeUriForExecutedQuery()
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

            // ... And then I change the uri for the query
            var changeUriParams = new ConnectionUriChangedParams {
                OriginalOwnerUri = Constants.OwnerUri,
                NewOwnerUri = newOwnerUri
            };
        
            
            await queryService.HandleConnectionUriChangedNotification(changeUriParams, new TestEventContext());

            // Then:
            // ... And the active queries should have the new query.
            Assert.That(queryService.ActiveQueries.TryGetValue(newOwnerUri, out query), "Query with newOwnerUri not found.");
            Assert.That(Equals(query.ConnectionOwnerURI, newOwnerUri), "OwnerUri was not changed!");
        }

        [Test]
        public void ChangeUriForMissingQuery()
        {
            // If:
            // ... I attempt to change the uri a query that doesn't exist
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            var queryService = Common.GetPrimedExecutionService(null, false, false, false, workspaceService.Object);
            const string newOwnerUri = "newTestFile";
            var changeUriParams = new ConnectionUriChangedParams {
                OriginalOwnerUri = Constants.OwnerUri,
                NewOwnerUri = newOwnerUri
            };

            Assert.ThrowsAsync<Exception>(async () => await queryService.HandleConnectionUriChangedNotification(changeUriParams, new TestEventContext()));

            Query query;
            Assert.False(queryService.ActiveQueries.TryGetValue(Constants.OwnerUri, out query), "Query was removed from Active Queries");
        }
    }
}
