//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.QueryStore;
using Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts;
using Microsoft.SqlTools.SqlCore.Performance;
using Microsoft.SqlTools.SqlCore.Performance.Common;
using Moq;
using NUnit.Framework;
using static Microsoft.SqlTools.SqlCore.Performance.PlanSummary.PlanSummaryConfiguration;

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
            QueryStoreQueryGenerator.MetricFetcher = GetMockMetricFetcher();
        }

        [Test]
        public async Task TopResourceConsumers()
        {
            QueryStoreService service = GetMockService();

            QueryStoreQueryResult result = await service.HandleGetTopResourceConsumersSummaryReportRequest(new GetTopResourceConsumersReportParams()
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
            });

            Assert.True(result.Success);
            Assert.That(result.Query.ReplaceLineEndings(), Is.EqualTo(QueryStoreBaselines.HandleGetTopResourceConsumersSummaryReportRequest.ReplaceLineEndings()));

            result = await service.HandleGetTopResourceConsumersDetailedSummaryReportRequest(new GetTopResourceConsumersReportParams()
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
            });

            Assert.True(result.Success);
            Assert.That(result.Query.ReplaceLineEndings(), Is.EqualTo(QueryStoreBaselines.HandleGetTopResourceConsumersDetailedSummaryReportRequest.ReplaceLineEndings()));
        }

        [Test]
        public async Task ForcedPlanQueries()
        {
            QueryStoreService service = GetMockService();

            QueryStoreQueryResult result = await service.HandleGetForcedPlanQueriesReportRequest(new GetForcedPlanQueriesReportParams()
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
            });

            Assert.True(result.Success);
            Assert.AreEqual(QueryStoreBaselines.HandleGetForcedPlanQueriesReportRequest.ReplaceLineEndings(), result.Query.ReplaceLineEndings());
        }

        [Test]
        public async Task TrackedQueries()
        {
            QueryStoreService service = GetMockService();

            QueryStoreQueryResult result = await service.HandleGetTrackedQueriesReportRequest(new GetTrackedQueriesReportParams()
            {
                QuerySearchText = "test search text"
            });

            Assert.True(result.Success);
            Assert.AreEqual(QueryStoreBaselines.HandleGetTrackedQueriesReportRequest.ReplaceLineEndings(), result.Query.ReplaceLineEndings());
        }

        [Test]
        public async Task HighVariationQueries()
        {
            QueryStoreService service = GetMockService();

            QueryStoreQueryResult result = await service.HandleGetHighVariationQueriesSummaryReportRequest(new GetHighVariationQueriesReportParams()
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
            });

            Assert.True(result.Success);
            Assert.That(result.Query.ReplaceLineEndings(), Is.EqualTo(QueryStoreBaselines.HandleGetHighVariationQueriesSummaryReportRequest.ReplaceLineEndings()));

            result = await service.HandleGetHighVariationQueriesDetailedSummaryReportRequest(new GetHighVariationQueriesReportParams()
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
            });

            Assert.True(result.Success);
            Assert.That(result.Query.ReplaceLineEndings(), Is.EqualTo(QueryStoreBaselines.HandleGetHighVariationQueriesDetailedSummaryReportRequest.ReplaceLineEndings()));
        }

        [Test]
        public async Task OverallResourceConsumption()
        {
            QueryStoreService service = GetMockService();

            QueryStoreQueryResult result = await service.HandleGetOverallResourceConsumptionReportRequest(new GetOverallResourceConsumptionReportParams()
            {
                ConnectionOwnerUri = TestConnectionOwnerUri,
                ReturnAllQueries = true,
                SelectedMetric = Metric.WaitTime,
                SelectedStatistic = Statistic.Stdev,
                MinNumberOfQueryPlans = 1,
                TopQueriesReturned = 50,
                SpecifiedTimeInterval = TestTimeInterval,
                SpecifiedBucketInterval = BucketInterval.Hour
            });

            Assert.True(result.Success);
            Assert.That(result.Query.ReplaceLineEndings(), Is.EqualTo(QueryStoreBaselines.HandleGetOverallResourceConsumptionReportRequest.ReplaceLineEndings()));
        }

        [Test]
        public async Task RegressedQueries()
        {
            QueryStoreService service = GetMockService();

            QueryStoreQueryResult result = await service.HandleGetRegressedQueriesSummaryReportRequest(new GetRegressedQueriesReportParams()
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
            });

            Assert.True(result.Success);
            Assert.That(result.Query.ReplaceLineEndings(), Is.EqualTo(QueryStoreBaselines.HandleGetRegressedQueriesSummaryReportRequest.ReplaceLineEndings()));

            result = await service.HandleGetRegressedQueriesDetailedSummaryReportRequest(new GetRegressedQueriesReportParams()
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
            });

            Assert.True(result.Success);
            Assert.That(result.Query.ReplaceLineEndings(), Is.EqualTo(QueryStoreBaselines.HandleGetRegressedQueriesDetailedSummaryReportRequest.ReplaceLineEndings()));
        }

        [Test]
        public async Task PlanSummary()
        {
            QueryStoreService service = GetMockService();

            QueryStoreQueryResult result = await service.HandleGetPlanSummaryChartViewRequest(new GetPlanSummaryParams()
            {
                ConnectionOwnerUri = TestConnectionOwnerUri,
                QueryId = 97,
                TimeInterval = TestTimeInterval,
                TimeIntervalMode = PlanTimeIntervalMode.SpecifiedRange,
                SelectedMetric = Metric.WaitTime,
                SelectedStatistic = Statistic.Stdev
            });

            Assert.True(result.Success);
            Assert.That(result.Query.ReplaceLineEndings(), Is.EqualTo(QueryStoreBaselines.HandleGetPlanSummaryChartViewRequest.ReplaceLineEndings()));

            result = await service.HandleGetPlanSummaryGridViewRequest(new GetPlanSummaryGridViewParams()
            {
                ConnectionOwnerUri = TestConnectionOwnerUri,
                QueryId = 97,
                TimeInterval = TestTimeInterval,
                TimeIntervalMode = PlanTimeIntervalMode.SpecifiedRange,
                SelectedMetric = Metric.WaitTime,
                SelectedStatistic = Statistic.Stdev,
                OrderByColumnId = "count_executions",
                Descending = true
            });

            Assert.True(result.Success);
            Assert.That(result.Query.ReplaceLineEndings(), Is.EqualTo(QueryStoreBaselines.HandleGetPlanSummaryGridViewRequest.ReplaceLineEndings()));
        }

        private QueryStoreService GetMockService()
        {
            Mock<QueryStoreService> mock = new Mock<QueryStoreService>();

            mock.Setup(s => s.GetSqlConnection(It.IsAny<QueryStoreReportParams>()))
                .Returns(new SqlConnection());

            return mock.Object;
        }

        private QueryStoreMetricFetcher GetMockMetricFetcher()
        {
            Mock<QueryStoreMetricFetcher> mock = new Mock<QueryStoreMetricFetcher>();

            mock.Setup(s => s.GetAvailableMetrics(It.IsAny<SqlConnection>()))
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
