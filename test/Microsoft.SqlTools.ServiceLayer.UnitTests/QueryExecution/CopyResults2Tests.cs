//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution
{
    public class CopyResults2Tests
    {
        [Test]
        public async Task CopyResults2TextPreservesSelectionRowOrder()
        {
            // If:
            // ... I have an executed query with standard test data
            var workspaceService = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(
                Common.StandardTestDataSet,
                true,
                false,
                false,
                workspaceService);

            var executeParams = new ExecuteDocumentSelectionParams
            {
                QuerySelection = Common.WholeDocument,
                OwnerUri = Constants.OwnerUri
            };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.WorkTask;
            await queryService.ActiveQueries[Constants.OwnerUri].ExecutionTask;
            var executedQuery = queryService.ActiveQueries[Constants.OwnerUri];

            // CopyTextBuilder currently retrieves subsets through QueryExecutionService.Instance,
            // so mirror the executed query there for this test.
            QueryExecutionService.Instance.ActiveQueries[Constants.OwnerUri] = executedQuery;

            // ... And I request copy2 with rows intentionally out of numeric order
            var copyParams = new CopyResults2RequestParams
            {
                OwnerUri = Constants.OwnerUri,
                BatchIndex = 0,
                ResultSetIndex = 0,
                CopyType = CopyType.Text,
                IncludeHeaders = false,
                LineSeparator = "\n",
                Selections = new[]
                {
                    new TableSelectionRange { FromRow = 3, ToRow = 3, FromColumn = 0, ToColumn = 0 },
                    new TableSelectionRange { FromRow = 1, ToRow = 1, FromColumn = 0, ToColumn = 0 }
                }
            };

            var copyRequest = new EventFlowValidator<CopyResults2RequestResult>()
                .AddResultValidation(result =>
                {
                    // Then:
                    // ... copied text should follow selection order, not sorted row order
                    Assert.NotNull(result);
                    Assert.AreEqual("Cell3.0\nCell1.0", result.Content);
                })
                .Complete();

            try
            {
                await queryService.HandleCopyResults2Request(copyParams, copyRequest.Object);
                copyRequest.Validate();
            }
            finally
            {
                QueryExecutionService.Instance.ActiveQueries.TryRemove(Constants.OwnerUri, out _);
            }
        }

        [Test]
        public async Task CopyResults2TextWithHeadersPreservesSelectionRowOrder()
        {
            // If:
            // ... I have an executed query with standard test data
            var workspaceService = Common.GetPrimedWorkspaceService(Constants.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(
                Common.StandardTestDataSet,
                true,
                false,
                false,
                workspaceService);

            var executeParams = new ExecuteDocumentSelectionParams
            {
                QuerySelection = Common.WholeDocument,
                OwnerUri = Constants.OwnerUri
            };
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.WorkTask;
            await queryService.ActiveQueries[Constants.OwnerUri].ExecutionTask;
            var executedQuery = queryService.ActiveQueries[Constants.OwnerUri];

            // CopyTextBuilder currently retrieves subsets through QueryExecutionService.Instance,
            // so mirror the executed query there for this test.
            QueryExecutionService.Instance.ActiveQueries[Constants.OwnerUri] = executedQuery;

            // ... And I request copy2 with headers and rows intentionally out of numeric order
            var copyParams = new CopyResults2RequestParams
            {
                OwnerUri = Constants.OwnerUri,
                BatchIndex = 0,
                ResultSetIndex = 0,
                CopyType = CopyType.Text,
                IncludeHeaders = true,
                LineSeparator = "\n",
                Selections = new[]
                {
                    new TableSelectionRange { FromRow = 3, ToRow = 3, FromColumn = 0, ToColumn = 0 },
                    new TableSelectionRange { FromRow = 1, ToRow = 1, FromColumn = 0, ToColumn = 0 }
                }
            };

            var copyRequest = new EventFlowValidator<CopyResults2RequestResult>()
                .AddResultValidation(result =>
                {
                    // Then:
                    // ... copied text should include headers and follow selection order
                    Assert.NotNull(result);
                    Assert.AreEqual("Col0\nCell3.0\nCell1.0", result.Content);
                })
                .Complete();

            try
            {
                await queryService.HandleCopyResults2Request(copyParams, copyRequest.Object);
                copyRequest.Validate();
            }
            finally
            {
                QueryExecutionService.Instance.ActiveQueries.TryRemove(Constants.OwnerUri, out _);
            }
        }
    }
}
