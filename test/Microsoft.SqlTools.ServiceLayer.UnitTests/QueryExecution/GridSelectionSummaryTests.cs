//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution
{
    public class GridSelectionSummaryTests
    {
        [Test]
        public async Task SelectionSummaryContinuesFromReturnedRowCountForShortPages()
        {
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

            var requestedStarts = new List<long>();
            queryService.GridSelectionSummaryResultSubsetProvider = (parameters, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                requestedStarts.Add(parameters.RowsStartIndex);

                int returnedRowCount = parameters.RowsStartIndex == 0
                    ? 75
                    : parameters.RowsCount;
                var rows = Enumerable.Range(0, returnedRowCount)
                    .Select(_ => new[]
                    {
                        new DbCellValue
                        {
                            DisplayValue = "1",
                            RawObject = 1,
                            IsNull = false
                        }
                    })
                    .ToArray();

                return Task.FromResult(new ResultSetSubset
                {
                    RowCount = rows.Length,
                    Rows = rows
                });
            };

            var summaryParams = new GridSelectionSummaryRequestParams
            {
                OwnerUri = Constants.OwnerUri,
                BatchIndex = 0,
                ResultSetIndex = 0,
                Selections = new[]
                {
                    new TableSelectionRange
                    {
                        FromRow = 0,
                        ToRow = 400,
                        FromColumn = 0,
                        ToColumn = 0
                    }
                }
            };
            var summaryRequest = new EventFlowValidator<GridSelectionSummaryResponse>()
                .AddResultValidation(result =>
                {
                    Assert.That(result.Count, Is.EqualTo(401));
                    Assert.That(result.Sum, Is.EqualTo(401m));
                })
                .Complete();

            await queryService.HandleGridSelectionSummaryRequest(summaryParams, summaryRequest.Object);

            summaryRequest.Validate();
            Assert.That(requestedStarts, Is.EqualTo(new long[] { 0, 75, 275 }));
        }

        [Test]
        public async Task SelectionSummaryRejectsAnEmptyPageBeforeSelectionEnd()
        {
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

            queryService.GridSelectionSummaryResultSubsetProvider = (_, _) => Task.FromResult(new ResultSetSubset
            {
                RowCount = 0,
                Rows = Array.Empty<DbCellValue[]>()
            });
            var summaryParams = new GridSelectionSummaryRequestParams
            {
                OwnerUri = Constants.OwnerUri,
                BatchIndex = 0,
                ResultSetIndex = 0,
                Selections = new[]
                {
                    new TableSelectionRange
                    {
                        FromRow = 0,
                        ToRow = 1,
                        FromColumn = 0,
                        ToColumn = 0
                    }
                }
            };
            var summaryRequest = RequestContextMocks.Create<GridSelectionSummaryResponse>(null);

            Assert.ThrowsAsync<InvalidOperationException>(() =>
                queryService.HandleGridSelectionSummaryRequest(summaryParams, summaryRequest.Object));
        }
    }
}
