﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.QueryStore;
using Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Moq;
using NUnit.Framework;
using static Microsoft.SqlServer.Management.QueryStoreModel.PlanSummary.PlanSummaryConfiguration;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.QueryStore
{
    public class QueryStoreTests : TestBase
    {
        private const string TestConnectionOwnerUri = "FakeConnectionOwnerUri";
        private static DateTimeOffset TestWindowStart = DateTimeOffset.Parse("6/10/2023 12:34:56 PM +0:00");
        private static DateTimeOffset TestWindowEnd = TestWindowStart.AddDays(7);
        private static DateTimeOffset TestWindowRecentStart = TestWindowEnd.AddHours(-1);
        private static BasicTimeInterval TestTimeInterval => new BasicTimeInterval()
        {
            StartDateTimeInUtc = TestWindowStart.ToString("O"),
            EndDateTimeInUtc = TestWindowEnd.ToString("O")
        };

        private static BasicTimeInterval RecentTestTimeInterval => new BasicTimeInterval()
        {
            StartDateTimeInUtc = TestWindowRecentStart.ToString("O"),
            EndDateTimeInUtc = TestWindowEnd.ToString("O")
        };

        [SetUp]
        public void Setup()
        {
            QueryStoreCommonConfiguration.DisplayTimeKind = DateTimeKind.Utc;
        }

        [Test]
        public async Task TopResourceConsumers()
        {
            QueryStoreService service = GetMock();

            MockRequest<QueryStoreQueryResult> request = new();
            await service.HandleGetTopResourceConsumersSummaryReportRequest(new GetTopResourceConsumersReportParams()
            {
                ConnectionOwnerUri = TestConnectionOwnerUri,
                ReturnAllQueries = true,
                SelectedMetric = Metric.WaitTime,
                SelectedStatistic = Statistic.Stdev,
                OrderByColumnId = "query_id",
                Descending = true,
                MinNumberOfQueryPlans = 1,
                TopQueriesReturned = 50,
                TimeInterval = TestTimeInterval
            }, request.Object);

            request.AssertSuccess(nameof(service.HandleGetTopResourceConsumersSummaryReportRequest));
            Assert.AreEqual(QueryStoreBaselines.HandleGetTopResourceConsumersSummaryReportRequest.ReplaceLineEndings(), request.Result.Query.ReplaceLineEndings());

            request = new();
            await service.HandleGetTopResourceConsumersDetailedSummaryReportRequest(new GetTopResourceConsumersReportParams()
            {
                ConnectionOwnerUri = TestConnectionOwnerUri,
                ReturnAllQueries = true,
                SelectedMetric = Metric.WaitTime,
                SelectedStatistic = Statistic.Stdev,
                OrderByColumnId = "query_id",
                Descending = true,
                MinNumberOfQueryPlans = 1,
                TopQueriesReturned = 50,
                TimeInterval = TestTimeInterval
            }, request.Object);

            request.AssertSuccess(nameof(service.HandleGetTopResourceConsumersDetailedSummaryReportRequest));
            Assert.AreEqual(QueryStoreBaselines.HandleGetTopResourceConsumersDetailedSummaryReportRequest.ReplaceLineEndings(), request.Result.Query.ReplaceLineEndings());
        }

        [Test]
        public async Task ForcedPlanQueries()
        {
            QueryStoreService service = GetMock();

            MockRequest<QueryStoreQueryResult> request = new();
            await service.HandleGetForcedPlanQueriesReportRequest(new GetForcedPlanQueriesReportParams()
            {
                ConnectionOwnerUri = TestConnectionOwnerUri,
                ReturnAllQueries = true,
                SelectedMetric = Metric.WaitTime,
                SelectedStatistic = Statistic.Stdev,
                OrderByColumnId = "query_id",
                Descending = true,
                MinNumberOfQueryPlans = 1,
                TopQueriesReturned = 50,
                TimeInterval = TestTimeInterval
            }, request.Object);

            request.AssertSuccess(nameof(service.HandleGetForcedPlanQueriesReportRequest));
            Assert.AreEqual(QueryStoreBaselines.HandleGetForcedPlanQueriesReportRequest.ReplaceLineEndings(), request.Result.Query.ReplaceLineEndings());
        }

        [Test]
        public async Task TrackedQueries()
        {
            QueryStoreService service = GetMock();

            MockRequest<QueryStoreQueryResult> request = new();
            await service.HandleGetTrackedQueriesReportRequest(new GetTrackedQueriesReportParams()
            {
                QuerySearchText = "test search text"
            }, request.Object);

            request.AssertSuccess(nameof(service.HandleGetTrackedQueriesReportRequest));
            Assert.AreEqual(QueryStoreBaselines.HandleGetTrackedQueriesReportRequest.ReplaceLineEndings(), request.Result.Query.ReplaceLineEndings());
        }

        [Test]
        public async Task HighVariationQueries()
        {
            QueryStoreService service = GetMock();

            MockRequest<QueryStoreQueryResult> request = new();
            await service.HandleGetHighVariationQueriesSummaryReportRequest(new GetHighVariationQueriesReportParams()
            {
                ConnectionOwnerUri = TestConnectionOwnerUri,
                ReturnAllQueries = true,
                SelectedMetric = Metric.WaitTime,
                SelectedStatistic = Statistic.Stdev,
                OrderByColumnId = "query_id",
                Descending = true,
                MinNumberOfQueryPlans = 1,
                TopQueriesReturned = 50,
                TimeInterval = TestTimeInterval
            }, request.Object);

            request.AssertSuccess(nameof(service.HandleGetHighVariationQueriesSummaryReportRequest));
            Assert.AreEqual(QueryStoreBaselines.HandleGetHighVariationQueriesSummaryReportRequest.ReplaceLineEndings(), request.Result.Query.ReplaceLineEndings());

            request = new();
            await service.HandleGetHighVariationQueriesDetailedSummaryReportRequest(new GetHighVariationQueriesReportParams()
            {
                ConnectionOwnerUri = TestConnectionOwnerUri,
                ReturnAllQueries = true,
                SelectedMetric = Metric.WaitTime,
                SelectedStatistic = Statistic.Stdev,
                OrderByColumnId = "query_id",
                Descending = true,
                MinNumberOfQueryPlans = 1,
                TopQueriesReturned = 50,
                TimeInterval = TestTimeInterval
            }, request.Object);

            request.AssertSuccess(nameof(service.HandleGetHighVariationQueriesDetailedSummaryReportRequest));
            Assert.AreEqual(QueryStoreBaselines.HandleGetHighVariationQueriesDetailedSummaryReportRequest.ReplaceLineEndings(), request.Result.Query.ReplaceLineEndings());
        }

        [Test]
        public async Task OverallResourceConsumption()
        {
            QueryStoreService service = GetMock();

            MockRequest<QueryStoreQueryResult> request = new();
            await service.HandleGetOverallResourceConsumptionReportRequest(new GetOverallResourceConsumptionReportParams()
            {
                ConnectionOwnerUri = TestConnectionOwnerUri,
                ReturnAllQueries = true,
                SelectedMetric = Metric.WaitTime,
                SelectedStatistic = Statistic.Stdev,
                MinNumberOfQueryPlans = 1,
                TopQueriesReturned = 50,
                SpecifiedTimeInterval = TestTimeInterval,
                SpecifiedBucketInterval = BucketInterval.Hour
            }, request.Object);

            request.AssertSuccess(nameof(service.HandleGetOverallResourceConsumptionReportRequest));
            Assert.AreEqual(QueryStoreBaselines.HandleGetOverallResourceConsumptionReportRequest.ReplaceLineEndings(), request.Result.Query.ReplaceLineEndings());
        }

        [Test]
        public async Task RegressedQueries()
        {
            QueryStoreService service = GetMock();

            MockRequest<QueryStoreQueryResult> request = new();
            await service.HandleGetRegressedQueriesSummaryReportRequest(new GetRegressedQueriesReportParams()
            {
                ConnectionOwnerUri = TestConnectionOwnerUri,
                ReturnAllQueries = true,
                SelectedMetric = Metric.WaitTime,
                SelectedStatistic = Statistic.Stdev,
                MinNumberOfQueryPlans = 1,
                TopQueriesReturned = 50,
                MinExecutionCount = 1,
                TimeIntervalHistory = TestTimeInterval,
                TimeIntervalRecent = RecentTestTimeInterval
            }, request.Object);

            request.AssertSuccess(nameof(service.HandleGetRegressedQueriesSummaryReportRequest));
            Assert.AreEqual(QueryStoreBaselines.HandleGetRegressedQueriesSummaryReportRequest.ReplaceLineEndings(), request.Result.Query.ReplaceLineEndings());

            request = new();
            await service.HandleGetRegressedQueriesDetailedSummaryReportRequest(new GetRegressedQueriesReportParams()
            {
                ConnectionOwnerUri = TestConnectionOwnerUri,
                ReturnAllQueries = true,
                SelectedMetric = Metric.WaitTime,
                SelectedStatistic = Statistic.Stdev,
                MinNumberOfQueryPlans = 1,
                TopQueriesReturned = 50,
                MinExecutionCount = 1,
                TimeIntervalHistory = TestTimeInterval,
                TimeIntervalRecent = RecentTestTimeInterval
            }, request.Object);

            request.AssertSuccess(nameof(service.HandleGetRegressedQueriesDetailedSummaryReportRequest));
            Assert.AreEqual(QueryStoreBaselines.HandleGetRegressedQueriesDetailedSummaryReportRequest.ReplaceLineEndings(), request.Result.Query.ReplaceLineEndings());
        }

        [Test]
        public async Task PlanSummary()
        {
            QueryStoreService service = GetMock();

            MockRequest<QueryStoreQueryResult> request = new();
            await service.HandleGetPlanSummaryChartViewRequest(new GetPlanSummaryParams()
            {
                ConnectionOwnerUri = TestConnectionOwnerUri,
                QueryId = 97,
                TimeInterval = TestTimeInterval,
                TimeIntervalMode = PlanTimeIntervalMode.SpecifiedRange,
                SelectedMetric = Metric.WaitTime,
                SelectedStatistic = Statistic.Stdev
            }, request.Object);

            request.AssertSuccess(nameof(service.HandleGetPlanSummaryChartViewRequest));
            Assert.AreEqual(QueryStoreBaselines.HandleGetPlanSummaryChartViewRequest.ReplaceLineEndings(), request.Result.Query.ReplaceLineEndings());

            request = new();
            await service.HandleGetPlanSummaryGridViewRequest(new GetPlanSummaryGridViewParams()
            {
                ConnectionOwnerUri = TestConnectionOwnerUri,
                QueryId = 97,
                TimeInterval = TestTimeInterval,
                TimeIntervalMode = PlanTimeIntervalMode.SpecifiedRange,
                SelectedMetric = Metric.WaitTime,
                SelectedStatistic = Statistic.Stdev,
                OrderByColumnId = "count_executions",
                Descending = true
            }, request.Object);

            request.AssertSuccess(nameof(service.HandleGetPlanSummaryGridViewRequest));
            Assert.AreEqual(QueryStoreBaselines.HandleGetPlanSummaryGridViewRequest.ReplaceLineEndings(), request.Result.Query.ReplaceLineEndings());
        }

        private QueryStoreService GetMock()
        {
            Mock<QueryStoreService> mock = new Mock<QueryStoreService>();
            mock.Setup(s => s.GetAvailableMetrics(It.IsAny<QueryStoreReportParams>()))
                .Returns(new List<Metric>()
                {
                    Metric.ClrTime,
                    Metric.CPUTime,
                    Metric.Dop,
                    Metric.Duration,
                    Metric.ExecutionCount,
                    Metric.LogicalReads,
                    Metric.LogicalWrites,
                    Metric.LogMemoryUsed,
                    Metric.MemoryConsumption,
                    Metric.PhysicalReads,
                    Metric.RowCount,
                    Metric.TempDbMemoryUsed,
                    Metric.WaitTime
                });

            return mock.Object;
        }
    }
}
