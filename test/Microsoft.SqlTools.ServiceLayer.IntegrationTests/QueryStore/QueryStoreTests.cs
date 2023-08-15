//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.QueryStore;
using Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.QueryStore
{
    public class QueryStoreTests : TestBase
    {

        [Test]
        public async Task TopResourceConsumers()
        {
            QueryStoreService service = new(ConnectionService.Instance);

            // Validate that result indicates failure when there's an exception
            MockRequest<QueryStoreQueryResult> requestMock = new();
            await service.HandleGetTopResourceConsumersSummaryReportRequest(new GetTopResourceConsumersReportParams
            {
                ConnectionOwnerUri = "",
                ReturnAllQueries = true,
                SelectedMetric = Metric.WaitTime,
                SelectedStatistic = Statistic.Stdev,
                OrderByColumnId = "test_column",
                Descending = true,
                MinNumberOfQueryPlans = 1,
                TopQueriesReturned = 50
            }, requestMock.Object);

            requestMock.AssertSuccess(nameof(service.HandleGetTopResourceConsumersSummaryReportRequest));
            Assert.AreEqual(QueryStoreBaselines.HandleGetTopResourceConsumersSummaryReportRequest, requestMock.Result.Query);
        }
    }
}
