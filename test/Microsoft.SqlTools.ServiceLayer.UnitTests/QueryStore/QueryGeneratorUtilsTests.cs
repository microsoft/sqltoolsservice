//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.SqlCore.Performance.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryStore
{
    public class QueryGeneratorUtilsTests
    {
        private const string RuntimeStatsTableName = "rs";
        private const string TotalCpuTime = "ROUND(CONVERT(float, SUM(rs.avg_cpu_time*rs.count_executions))*0.001,2)";
        private const string AvgCpuTime = "ROUND(CONVERT(float, SUM(rs.avg_cpu_time*rs.count_executions))/NULLIF(SUM(rs.count_executions), 0)*0.001,2)";
        private const string MinCpuTime = "ROUND(CONVERT(float, MIN(rs.min_cpu_time))*0.001,2)";
        private const string MaxCpuTime = "ROUND(CONVERT(float, MAX(rs.max_cpu_time))*0.001,2)";
        private const string StdevCpuTime = "ROUND(CONVERT(float, SQRT( SUM(rs.stdev_cpu_time*rs.stdev_cpu_time*rs.count_executions)/NULLIF(SUM(rs.count_executions), 0)))*0.001,2)";
        private const string TotalExecutionCount = "CONVERT(float, SUM(rs.count_executions))";
        private const string TotalLogicalReads = "ROUND(CONVERT(float, SUM(rs.avg_logical_io_reads*rs.count_executions))*8,2)";
        private const string StdevMemoryConsumption = "ROUND(CONVERT(float, SQRT( SUM(rs.stdev_query_max_used_memory*rs.stdev_query_max_used_memory*rs.count_executions)/NULLIF(SUM(rs.count_executions), 0)))*8,2)";
        private const string VariationWaitTime = "ISNULL(ROUND(CONVERT(float, (SQRT( SUM(ws.stdev_query_wait_time*ws.stdev_query_wait_time*ws.count_executions)/NULLIF(SUM(ws.count_executions), 0))*SUM(ws.count_executions)) / NULLIF(SUM(ws.avg_query_wait_time*ws.count_executions), 0)),2), 0)";

        [Test]
        public void When_statistic_is_Last_and_metric_is_CPUTime_Getsummary_Throws_ArgumentException()
        {
            var statistic = Statistic.Last;
            var metric = Metric.CPUTime;

            //Verify exception is thrown
            Assert.Throws<ArgumentException>(() => QueryGeneratorUtils.GetRuntimeStatsSummary(statistic, metric, RuntimeStatsTableName));
        }

        [Test]
        public void When_statistic_is_Total_and_metric_is_CPUTime_Getsummary_Returns_ValidSummary()
        {
            var statistic = Statistic.Total;
            var metric = Metric.CPUTime;

            var summary = QueryGeneratorUtils.GetRuntimeStatsSummary(statistic, metric, RuntimeStatsTableName);

            //Verify valid summary with test template
            Assert.That(summary, Is.EqualTo(TotalCpuTime), "Summary doesn't match");
        }

        [Test]
        public void When_statistic_is_Avg_and_metric_is_CPUTime_Getsummary_Returns_ValidSummary()
        {
            var statistic = Statistic.Avg;
            var metric = Metric.CPUTime;

            var summary = QueryGeneratorUtils.GetRuntimeStatsSummary(statistic, metric, RuntimeStatsTableName);

            //Verify valid summary with test template
            Assert.That(summary, Is.EqualTo(AvgCpuTime), "Summary doesn't match");
        }

        [Test]
        public void When_statistic_is_Min_and_metric_is_CPUTime_Getsummary_Returns_ValidSummary()
        {
            //Arrange
            var statistic = Statistic.Min;
            var metric = Metric.CPUTime;

            //Act
            var summary = QueryGeneratorUtils.GetRuntimeStatsSummary(statistic, metric, RuntimeStatsTableName);

            //Verify valid summary with test template
            Assert.That(summary, Is.EqualTo(MinCpuTime), "Summary doesn't match");
        }

        [Test]
        public void When_statistic_is_Max_and_metric_is_CPUTime_Getsummary_Returns_ValidSummary()
        {
            //Arrange
            var statistic = Statistic.Max;
            var metric = Metric.CPUTime;

            //Act
            var summary = QueryGeneratorUtils.GetRuntimeStatsSummary(statistic, metric, RuntimeStatsTableName);

            //Verify valid summary with test template
            Assert.That(summary, Is.EqualTo(MaxCpuTime), "Summary doesn't match");
        }

        [Test]
        public void When_statistic_is_Stdev_and_metric_is_CPUTime_Getsummary_Returns_ValidSummary()
        {
            //Arrange
            var statistic = Statistic.Stdev;
            var metric = Metric.CPUTime;

            //Act
            var summary = QueryGeneratorUtils.GetRuntimeStatsSummary(statistic, metric, RuntimeStatsTableName);

            //Verify valid summary with test template
            Assert.That(summary, Is.EqualTo(StdevCpuTime), "Summary doesn't match");
        }

        [Test]
        public void When_statistic_is_Total_and_metric_is_ExecutionCount_Getsummary_Returns_ValidSummary()
        {
            //Arrange
            var statistic = Statistic.Total;
            var metric = Metric.ExecutionCount;

            //Act
            var summary = QueryGeneratorUtils.GetRuntimeStatsSummary(statistic, metric, RuntimeStatsTableName);

            //Verify valid summary with test template
            Assert.That(summary, Is.EqualTo(TotalExecutionCount), "Summary doesn't match");
        }

        [Test]
        public void When_statistic_is_Total_and_metric_is_LogicalReads_Getsummary_Returns_ValidSummary()
        {
            //Arrange
            var statistic = Statistic.Total;
            var metric = Metric.LogicalReads;

            //Act
            var summary = QueryGeneratorUtils.GetRuntimeStatsSummary(statistic, metric, RuntimeStatsTableName);

            //Verify valid summary with test template
            Assert.That(summary, Is.EqualTo(TotalLogicalReads), "Summary doesn't match");
        }

        [Test]
        public void When_statistic_is_Stdev_and_metric_is_MemoryConsumption_Getsummary_Returns_ValidSummary()
        {
            //Arrange
            var statistic = Statistic.Stdev;
            var metric = Metric.MemoryConsumption;

            //Act
            var summary = QueryGeneratorUtils.GetRuntimeStatsSummary(statistic, metric, RuntimeStatsTableName);

            //Verify valid summary with test template
            Assert.That(summary, Is.EqualTo(StdevMemoryConsumption), "Summary doesn't match");
        }

        [Test]
        public void When_statistic_is_variation_and_metric_is_WaitTime_Getsummary_Returns_ValidSummary()
        {
            //Arrange
            var statistic = Statistic.Variation;
            var metric = Metric.WaitTime;

            //Act
            var summary = QueryGeneratorUtils.GetRuntimeStatsSummary(statistic, metric, "ws");

            //Verify valid summary with test template
            Assert.That(summary, Is.EqualTo(VariationWaitTime), "Summary doesn't match");
        }
    }
}
