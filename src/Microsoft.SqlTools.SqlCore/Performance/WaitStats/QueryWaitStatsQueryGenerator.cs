//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.SqlTools.SqlCore.Performance.Common;
using Microsoft.SqlTools.SqlCore.Performance.TopResourceConsumers;

namespace Microsoft.SqlTools.SqlCore.Performance.WaitStats
{
    /// <summary>
    /// Helper Class used to generate the queries required for query store UI wait stats report
    /// </summary>
    public class QueryWaitStatsQueryGenerator
    {

        /// <summary>
        /// This creates a t-sql that returns total wait stats for each wait category
        /// for a given queryId between given time interval.
        /// Template Used: QueryTemplates.TotalWaitTimePerWaitCategoryForQueryIdTemplate.
        /// Used for extended tooltip agregated data on TRC report
        /// </summary>
        /// <returns></returns>
        public static string TotalWaitTimePerWaitCategoryForQueryId(TopResourceConsumersConfiguration configuration)
        {
            var replicaFilter = configuration.IsQDSROAvailable ? $"    AND ws.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            return QueryTemplates.GenerateTotalWaitTimePerWaitCategoryForQueryIdTemplate(
                QueryGeneratorUtils.ParameterQueryId,
                QueryGeneratorUtils.ParameterIntervalEndTime,
                QueryGeneratorUtils.ParameterIntervalStartTime,
                replicaFilter);
        }

        /// <summary>
        /// This creates a t-sql that returns aggregated wait time for each wait category
        /// between a given time interval.
        /// Template Used: QueryTemplates.AggWaitTimePerWaitCategoryTemplate
        /// Used for Query Wait Stats (per wait category view)
        /// </summary>
        public static string AggWaitTimePerWaitCategory(QueryWaitStatsConfiguration configuration, out IList<ColumnInfo> columnInfoList)
            => AggWaitTimePerWaitCategory(configuration, orderByColumn: null, descending: true, out columnInfoList);

        /// <summary>
        /// This creates a t-sql that returns aggregated wait time for each wait category
        /// between a given time interval.
        /// Template Used: QueryTemplates.AggWaitTimePerWaitCategoryTemplate
        /// Used for Query Wait Stats (per wait category view)
        /// </summary>
        public static string AggWaitTimePerWaitCategory(
            QueryWaitStatsConfiguration configuration,
            ColumnInfo orderByColumn,
            bool descending,
            out IList<ColumnInfo> columnInfoList)
        {
            var metric = configuration.SelectedMetric;
            StringBuilder summary = new StringBuilder();
            columnInfoList = new List<ColumnInfo>
            {
                new WaitCategoryIdColumnInfo(),
                new WaitCategoryDescColumnInfo()
            };

            // Retrieve all metrics from the list.
            //
            var statisticList = GetAvailableMetrics();

            foreach (var statistic in statisticList)
            {
                // We only have extended tool tip data for Total WaitTime
                //
                bool extendedTooltipData = (Metric.WaitTime == metric && statistic == configuration.SelectedStatistic);
                var column = new StatisticMetricColumnInfo(statistic, metric) { BindRuntimeData = extendedTooltipData };

                summary.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    {0} {1}_{2},",
                        QueryGeneratorUtils.GetWaitStatsSummary(statistic, QueryGeneratorUtils.GetStatsViewAlias(Metric.WaitTime)),
                        StatisticUtils.QueryString(statistic),
                        MetricUtils.QueryString(metric)));
                columnInfoList.Add(column);
            }

            columnInfoList.Add(new ExecutionCountColumnInfo());

            var replicaFilter = configuration.IsQDSROAvailable ? $"    AND ws.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            string query = QueryTemplates.GenerateAggWaitTimePerWaitCategoryTemplate(
                summary.ToString().TrimEnd(),
                QueryGeneratorUtils.ParameterIntervalEndTime,
                QueryGeneratorUtils.ParameterIntervalStartTime,
                QueryGeneratorUtils.RowsToReturnString(configuration.ReturnAllWaitCategories),
                replicaFilter);

            return Utils.AppendOrderBy(query, orderByColumn, isDescending: descending);
        }

        /// <summary>
        /// This creates a t-sql that returns aggregated wait time per queryId for a given wait category
        /// between a given time interval.
        /// Template Used: QueryTemplates.AggWaitTimePerQueryForWaitCategoryIdTemplate
        /// Used for extend aggregated tooltip data on Query Wait Stats (wait category view)
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static string AggWaitTimePerQueryForWaitCategoryId(QueryWaitStatsConfiguration configuration)
        {
            IList<ColumnInfo> columnInfoList;

            // Update configuration to limit the result for ToolTip
            //
            configuration.ReturnAllQueries = false;
            return AggWaitTimePerQueryForWaitCategoryId(configuration,
                new StatisticMetricColumnInfo(configuration.SelectedStatistic, configuration.SelectedMetric),
                true, out columnInfoList);
        }

        /// <summary>
        /// This creates a t-sql that returns aggregated wait time per queryId for a given wait category
        /// between a given time interval.
        /// Template Used: QueryTemplates.AggWaitTimePerQueryForWaitCategoryIdTemplate
        /// Used for Query Wait Stats (per query view)
        /// </summary>
        public static string AggWaitTimePerQueryForWaitCategoryId(QueryWaitStatsConfiguration configuration, out IList<ColumnInfo> columnInfoList)
            => AggWaitTimePerQueryForWaitCategoryId(configuration, orderByColumn: null, descending: true, out columnInfoList);

        /// <summary>
        /// This creates a t-sql that returns aggregated wait time per queryId for a given wait category
        /// between a given time interval.
        /// Template Used: QueryTemplates.AggWaitTimePerQueryForWaitCategoryIdTemplate
        /// Used for Query Wait Stats (per query view)
        /// </summary>
        public static string AggWaitTimePerQueryForWaitCategoryId(
            QueryWaitStatsConfiguration configuration,
            ColumnInfo orderByColumn,
            bool descending,
            out IList<ColumnInfo> columnInfoList)
        {
            var metric = configuration.SelectedMetric;

            if (orderByColumn == null)
            {
                orderByColumn = new StatisticMetricColumnInfo(configuration.SelectedStatistic,
                    configuration.SelectedMetric);
            }

            StringBuilder summary = new StringBuilder();
            columnInfoList = new List<ColumnInfo>
            {
                new QueryIdColumnInfo(),
                new ObjectIdColumnInfo(),
                new ObjectNameColumnInfo(),
                new QueryTextColumnInfo()
            };

            // Retrieve all metrics from the list.
            //
            var statisticList = GetAvailableMetrics();

            foreach (var statistic in statisticList)
            {
                var column = new StatisticMetricColumnInfo(statistic, metric);
                summary.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    {0} {1}_{2},",
                        QueryGeneratorUtils.GetWaitStatsSummary(statistic, QueryGeneratorUtils.GetStatsViewAlias(Metric.WaitTime)),
                        StatisticUtils.QueryString(statistic),
                        MetricUtils.QueryString(metric)));
                columnInfoList.Add(column);
            }

            columnInfoList.Add(new ExecutionCountColumnInfo());

            var replicaFilter = configuration.IsQDSROAvailable ? $"    AND ws.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            string query = QueryTemplates.GenerateAggWaitTimePerQueryForWaitCategoryIdTemplate(
                summary.ToString().TrimEnd(),
                QueryGeneratorUtils.ParameterIntervalEndTime,
                QueryGeneratorUtils.ParameterIntervalStartTime,
                QueryGeneratorUtils.ParameterWaitCategoryId,
                QueryGeneratorUtils.RowsToReturnString(configuration.ReturnAllQueries),
                replicaFilter);

            return Utils.AppendOrderBy(query, orderByColumn, isDescending: descending);
        }

        /// <summary>
        /// This method creates a common table expression for WaitStats sub query.
        /// </summary>
        /// <param name="waitstatsViewName"></param>
        /// <param name="statisticList"></param>
        /// <param name="includeQueryExecutionLastWaitTime"></param>
        /// <param name="addWithClause"></param>
        /// <param name="addSeparator"></param>
        /// <param name="endTime"></param>
        /// <param name="startTime"></param>
        /// <param name="includeReplicaGroupId">Where the replica group id predicate should be added or not</param>
        /// <returns></returns>
        public static string GetWaitStatsTableExpression(string waitstatsViewName,
            IList<Statistic> statisticList,
            bool includeReplicaGroupId,
            bool includeQueryExecutionLastWaitTime = false,
            bool addWithClause = false,
            bool addSeparator = false,
            string endTime = QueryGeneratorUtils.ParameterIntervalEndTime,
            string startTime = QueryGeneratorUtils.ParameterIntervalStartTime)
        {
            const string tableExpression =
@"{0}{1} AS
(
{2}
){3}";
            return string.Format(tableExpression,
                addWithClause ? "With " : string.Empty,
                waitstatsViewName,
                GetWaitStatsTableExpressionSubQuery(statisticList, includeQueryExecutionLastWaitTime, endTime, startTime, includeReplicaGroupId),
                addSeparator ? "," : string.Empty);
        }

        /// <summary>
        /// This method creates a sub query for wait stats which will be plugged into CTE.
        /// One of the two templates are choosen based on the required column -
        /// QueryTemplates.GenerateWaitStatsViewTemplateIncludeLastQueryExecutionWaitTime or
        /// QueryTemplates.GenerateWaitStatsViewTemplateGroupedByPlanIdIntervalIdWaitCategory
        /// system view used: sys.query_store_wait_stats
        /// </summary>
        /// <param name="statisticList"></param>
        /// <param name="includeQueryExecutionLastWaitTime"></param>
        /// <param name="endTime"></param>
        /// <param name="startTime"></param>
        /// <param name="includeReplicaGroupId">Where the replica group id predicate should be added or not</param>
        /// <returns></returns>
        private static string GetWaitStatsTableExpressionSubQuery(IList<Statistic> statisticList, bool includeQueryExecutionLastWaitTime, string endTime, string startTime, bool includeReplicaGroupId)
        {
            if (statisticList == null || statisticList.Count == 0)
            {
                statisticList = GetAvailableMetrics();
            }

            var summary = new StringBuilder(includeReplicaGroupId ? $"    ws.replica_group_id,{Environment.NewLine}" : string.Empty);

            foreach (var statistic in statisticList)
            {
                summary.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    {0} {1}_{2},",
                        QueryGeneratorUtils.GetWaitStatsSummary(statistic, QueryGeneratorUtils.GetStatsViewAlias(Metric.WaitTime)),
                        StatisticUtils.QueryString(statistic),
                        MetricUtils.QueryString(Metric.WaitTime)));
            }

            var replicaFilter =
                  includeQueryExecutionLastWaitTime
                  ? $"    WHERE replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}"
                  : $"    AND ws.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}";

            return includeQueryExecutionLastWaitTime ?
                QueryTemplates.GenerateWaitStatsViewTemplateIncludeLastQueryExecutionWaitTime(summary.ToString().TrimEnd(), endTime, startTime, includeReplicaGroupId ? replicaFilter : string.Empty, includeReplicaGroupId ? ", ws.replica_group_id" : string.Empty) :
                QueryTemplates.GenerateWaitStatsViewTemplateGroupedByPlanIdIntervalIdWaitCategory(summary.ToString().TrimEnd(), endTime, startTime, includeReplicaGroupId ? replicaFilter : string.Empty, includeReplicaGroupId ? ", ws.replica_group_id" : string.Empty);
        }

        /// <summary>
        /// Returns list of available metric for QueryWaitStats
        /// </summary>
        /// <returns></returns>
        private static IList<Statistic> GetAvailableMetrics() =>
            // Retrieve all metrics from the list.
            //
            Enum.GetValues(typeof(Statistic)).Cast<Statistic>().Except(new[] { Statistic.Last, Statistic.Variation }).ToList();
    }
}
