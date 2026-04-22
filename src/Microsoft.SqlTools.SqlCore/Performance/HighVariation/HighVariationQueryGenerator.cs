//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.SqlTools.SqlCore.Performance.Common;
using Microsoft.SqlTools.SqlCore.Performance.WaitStats;

namespace Microsoft.SqlTools.SqlCore.Performance.HighVariation
{
    /// <summary>
    /// Class used to generate the queries required for query store UI
    /// </summary>
    public class HighVariationQueryGenerator
    {
        #region High Variation Summary

        /// <summary>
        /// This method generates a dynamic query for high variation summary.
        /// Template used : QueryTemplates.HighVariationQueryTemplate
        /// </summary>
        /// <param name="configuration">HighVariation Configuration.</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the HighVariationPane to help keep track of the order of column</param>
        /// <returns></returns>
        public static string HighVariationSummary(HighVariationConfiguration configuration, out IList<ColumnInfo> columnInfoList)
            => HighVariationSummary(configuration, orderByColumn: null, descending: true, out columnInfoList);

        /// <summary>
        /// This method generates a dynamic query for high variation summary.
        /// Template used : QueryTemplates.HighVariationQueryTemplate
        /// </summary>
        /// <param name="configuration">HighVariation Configuration.</param>
        /// <param name="orderByColumn">The current sorting column</param>
        /// <param name="descending">Whether we want to sort by ascending or descending</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the HighVariationPane to help keep track of the order of column</param>
        /// <returns></returns>
        public static string HighVariationSummary(
            HighVariationConfiguration configuration,
            ColumnInfo orderByColumn,
            bool descending,
            out IList<ColumnInfo> columnInfoList)
        {
            Statistic statistic = configuration.SelectedStatistic;
            Metric metric = configuration.SelectedMetric;

            var waitstatsSubQuery = string.Empty;
            var statsViewName = QueryGeneratorUtils.GetStatsViewName(metric);
            var statsAlias = QueryGeneratorUtils.GetStatsViewAlias(metric);

            if (metric.Equals(Metric.WaitTime))
            {
                waitstatsSubQuery = QueryWaitStatsQueryGenerator.GetWaitStatsTableExpression(
                    statsViewName, statisticList: null,
                    includeReplicaGroupId: configuration.IsQDSROAvailable,
                    includeQueryExecutionLastWaitTime: false, addWithClause: true);
            }

            var replicaFilter = configuration.IsQDSROAvailable ? $"    AND {statsAlias}.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            // Construct the query based on these columns
            string query = QueryTemplates.GenerateHighVariationQueryTemplate(
                waitstatsSubQuery,                                                      //{0}
                QueryGeneratorUtils.RowsToReturnString(configuration.ReturnAllQueries), //{1}
                GetFinalSelects(metric, statistic, statsAlias, out columnInfoList),     //{2}
                statsViewName,                                                          //{3}
                statsAlias,                                                             //{4}
                QueryGeneratorUtils.ParameterIntervalEndTime,                           //{5}
                QueryGeneratorUtils.ParameterIntervalStartTime,                         //{6}
                configuration.MinNumberOfQueryPlans,                                    //{7}
                "count_executions",                                                     //{8}
                replicaFilter).Trim();                                                  //{9}

            return Utils.AppendOrderBy(query, orderByColumn, isDescending: descending);
        }
        #endregion

        #region Detailed Summary

        /// <summary>
        /// This method generates a dynamic query for high variation detailed summary for wait stats.
        /// Template used : QueryTemplates.HighVariationDetailedQueryWithWaitStatsTemplate
        /// </summary>
        /// <param name="availableMetrics">Available metrics for a given database context</param>
        /// <param name="configuration">HighVariation Configuration.</param>
        /// <param name="orderByColumn">The current sorting column</param>
        /// <param name="descending">Whether we want to sort by ascending or descending</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the HighVariationPane to help keep track of the order of column</param>
        /// <returns></returns>
        internal static string HighVariationDetailedSummaryWithWaitStats(
            IList<Metric> availableMetrics,
            HighVariationConfiguration configuration,
            ColumnInfo orderByColumn,
            bool descending,
            out IList<ColumnInfo> columnInfoList)
        {
            var waitTimeMetric = Metric.WaitTime;
            var durationMetric = Metric.Duration;
            var waitstatsViewName = QueryGeneratorUtils.GetStatsViewName(waitTimeMetric);
            var waitstatsViewAlias = QueryGeneratorUtils.GetStatsViewAlias(waitTimeMetric);
            var runtimestatsViewName = QueryGeneratorUtils.GetStatsViewName(durationMetric);
            var runtimestatsViewAlias = QueryGeneratorUtils.GetStatsViewAlias(durationMetric);

            var waitstatsMetricList = new List<Metric> { Metric.WaitTime };
            var runtimestatsMetricList = availableMetrics.Except(new[] { Metric.ExecutionCount, Metric.WaitTime }).ToList();
            var statsMetricList = availableMetrics.Except(new[] { Metric.ExecutionCount }).ToList();

            var waitstatsSubQuery = QueryWaitStatsQueryGenerator.GetWaitStatsTableExpression(
                waitstatsViewName,
                statisticList: new List<Statistic> { Statistic.Avg, Statistic.Stdev },
                includeReplicaGroupId: configuration.IsQDSROAvailable,
                includeQueryExecutionLastWaitTime: false, addWithClause: false, addSeparator: true);

            var replicaFilter = configuration.IsQDSROAvailable ? $" AND A.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            // Construct the query based on these columns
            string query = QueryTemplates.GenerateHighVariationDetailedQueryWithWaitStatsTemplate(
                waitstatsSubQuery,
                GetCteStatement(configuration, waitstatsMetricList, waitTimeMetric, false, waitstatsViewName, waitstatsViewAlias, QueryGeneratorUtils.ParameterIntervalStartTime, QueryGeneratorUtils.ParameterIntervalEndTime, out _),
                GetCteStatement(configuration, runtimestatsMetricList, durationMetric, false, runtimestatsViewName, runtimestatsViewAlias, QueryGeneratorUtils.ParameterIntervalStartTime, QueryGeneratorUtils.ParameterIntervalEndTime, out _),
                QueryGeneratorUtils.RowsToReturnString(configuration.ReturnAllQueries),
                GetDetailFinalSelects(configuration.SelectedStatistic, statsMetricList, "A", "B", out columnInfoList),
                configuration.MinNumberOfQueryPlans,
                replicaFilter);

            return Utils.AppendOrderBy(query, orderByColumn, isDescending: descending);
        }

        /// <summary>
        /// This method generates a dynamic query for high variation detailed summary.
        /// Template used : QueryTemplates.HighVariationDetailedQueryTemplate
        /// </summary>
        /// <param name="availableMetrics">Available metrics for a given database context</param>
        /// <param name="configuration">HighVariation Configuration.</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the HighVariationPane to help keep track of the order of column</param>
        /// <returns></returns>
        public static string HighVariationDetailedSummary(IList<Metric> availableMetrics, HighVariationConfiguration configuration, out IList<ColumnInfo> columnInfoList)
            => HighVariationDetailedSummary(availableMetrics, configuration, orderByColumn: null, descending: true, out columnInfoList);

        /// <summary>
        /// This method generates a dynamic query for high variation detailed summary.
        /// Template used : QueryTemplates.HighVariationDetailedQueryTemplate
        /// </summary>
        /// <param name="availableMetrics">Available metrics for a given database context</param>
        /// <param name="configuration">HighVariation Configuration.</param>
        /// <param name="orderByColumn">The current sorting column</param>
        /// <param name="descending">Whether we want to sort by ascending or descending</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the HighVariationPane to help keep track of the order of column</param>
        /// <returns></returns>
        public static string HighVariationDetailedSummary(
            IList<Metric> availableMetrics,
            HighVariationConfiguration configuration,
            ColumnInfo orderByColumn,
            bool descending,
            out IList<ColumnInfo> columnInfoList)
        {
            // If detailed summary requested for WaitTime
            if (availableMetrics.Contains(Metric.WaitTime))
            {
                return HighVariationDetailedSummaryWithWaitStats(availableMetrics, configuration, orderByColumn, descending, out columnInfoList);
            }

            var selectedMetric = Metric.Duration;
            var runtimestatsViewName = QueryGeneratorUtils.GetStatsViewName(selectedMetric);
            var runtimestatsViewAlias = QueryGeneratorUtils.GetStatsViewAlias(selectedMetric);

            var runtimestatsMetricList = availableMetrics.Except(new[] { Metric.ExecutionCount, Metric.WaitTime }).ToList();

            var replicaFilter = configuration.IsQDSROAvailable ? $" AND {runtimestatsViewAlias}.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            // Construct the query based on these columns
            string query = QueryTemplates.GenerateHighVariationDetailedQueryTemplate(
                GetCteStatement(configuration, runtimestatsMetricList, selectedMetric, true, runtimestatsViewName, runtimestatsViewAlias, QueryGeneratorUtils.ParameterIntervalStartTime, QueryGeneratorUtils.ParameterIntervalEndTime, out columnInfoList),
                configuration.MinNumberOfQueryPlans,
                runtimestatsViewAlias,
                replicaFilter);

            return Utils.AppendOrderBy(query, orderByColumn, isDescending: descending);
        }

        #endregion

        #region Private Helpers

        ///  <summary>
        ///  The final select block of the HighVariation query
        ///  Specifies the list of information we want to retrieve to display in the HighVariation grid/chart report.
        ///  </summary>
        ///  <param name="metric">The metric of interest for the query</param>
        ///  <param name="statistic">The statistic of interest for the query</param>
        /// <param name="statsAlias"></param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the HighVariationPane to help keep track of the order of column</param>
        ///  <returns>The select block text and an ordered list of ColumnInfo</returns>
        private static string GetFinalSelects(Metric metric, Statistic statistic, string statsAlias, out IList<ColumnInfo> columnInfoList)
        {
            columnInfoList = new List<ColumnInfo>();

            var builder = new StringBuilder();

            // Construct the following columns (in this specific order):
            // 1. Query ID
            // 2. Object ID
            // 3. Object Name
            // 4. Query Text
            // 5. Std. Dev
            // 7. Avg Metric
            // 8. Variation Metric (Only if selected Statistic is Variation)
            // 9. Execution Count
            // 10. Number of Plans
            QueryIdColumnInfo queryIdColumn = new QueryIdColumnInfo();
            ObjectIdColumnInfo objectIdColumn = new ObjectIdColumnInfo();
            ObjectNameColumnInfo objectNameColumn = new ObjectNameColumnInfo();
            QueryTextColumnInfo queryTextColumn = new QueryTextColumnInfo();
            StatisticMetricColumnInfo stdDevMetricColumn = new StatisticMetricColumnInfo(Statistic.Stdev, metric);
            StatisticMetricColumnInfo avgMetricColumn = new StatisticMetricColumnInfo(Statistic.Avg, metric);
            StatisticMetricColumnInfo variationMetricColumn = new StatisticMetricColumnInfo(Statistic.Variation, metric);
            columnInfoList.Add(queryIdColumn);
            columnInfoList.Add(objectIdColumn);
            columnInfoList.Add(objectNameColumn);
            columnInfoList.Add(queryTextColumn);
            columnInfoList.Add(stdDevMetricColumn);
            columnInfoList.Add(avgMetricColumn);
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    p.query_id {0},", queryIdColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    q.object_id {0},", objectIdColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    ISNULL(OBJECT_NAME(q.object_id),'') {0},", objectNameColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    qt.{0} {0},", queryTextColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    {0} {1},", QueryGeneratorUtils.GetRuntimeStatsSummary(stdDevMetricColumn.Statistic, stdDevMetricColumn.Metric, statsAlias), stdDevMetricColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    {0} {1},", QueryGeneratorUtils.GetRuntimeStatsSummary(avgMetricColumn.Statistic, avgMetricColumn.Metric, statsAlias), avgMetricColumn.GetQueryColumnLabel()));

            if (statistic.Equals(Statistic.Variation))
            {
                columnInfoList.Add(variationMetricColumn);
                builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    {0} {1},", QueryGeneratorUtils.GetRuntimeStatsSummary(variationMetricColumn.Statistic, variationMetricColumn.Metric, statsAlias), variationMetricColumn.GetQueryColumnLabel()));
            }

            ExecutionCountColumnInfo execCountColumn = new ExecutionCountColumnInfo();
            columnInfoList.Add(execCountColumn);
            builder.AppendLine(QueryTemplates.GetExecutionCountText(metric, statsAlias));

            NumPlansColumnInfo numPlansColumn = new NumPlansColumnInfo();
            columnInfoList.Add(numPlansColumn);
            builder.AppendLine(QueryTemplates.GetPlanCountText());

            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// This method generates a dynamic query.
        /// Template used : QueryTemplates.HighVariationCteTemplate
        /// </summary>
        /// <param name="configuration">HighVariation Configuration</param>
        /// <param name="availableMetrics">List of supported metrics</param>
        /// <param name="selectedMetric">Selected Metric</param>
        /// <param name="addTopClause">Top(x) clause required</param>
        /// <param name="statsTableName">stats table name (wait stats or runtime stats)</param>
        /// <param name="statsTableAlias">stats table alias (wait stats or runtime stats)</param>
        /// <param name="startTimeParameter">start time parameter(start_time or first_execution_time)</param>
        /// <param name="endTimeParameter">start time parameter(end_time or last_execution_time)</param>
        /// <param name="columnInfoList">List of ColumnInfo to help keep track of the order of column</param>
        /// <returns></returns>
        private static string GetCteStatement(HighVariationConfiguration configuration, IList<Metric> availableMetrics, Metric selectedMetric, bool addTopClause,
            string statsTableName, string statsTableAlias, string startTimeParameter, string endTimeParameter, out IList<ColumnInfo> columnInfoList)
        {
            var statistic = configuration.SelectedStatistic;
            StringBuilder metrics = new StringBuilder();
            columnInfoList = new List<ColumnInfo>
            {
                new QueryIdColumnInfo(),
                new ObjectIdColumnInfo(),
                new ObjectNameColumnInfo(),
                new QueryTextColumnInfo()
            };

            // Retrieve all metrics from the list.
            foreach (Metric metric in availableMetrics)
            {
                var column = new StatisticMetricColumnInfo(statistic, metric);
                metrics.AppendLine(
                    string.Format(CultureInfo.InvariantCulture,
                    @"    {0} {1}_{2},",
                        QueryGeneratorUtils.GetRuntimeStatsSummary(statistic, metric, statsTableAlias),
                        StatisticUtils.QueryString(statistic),
                        MetricUtils.QueryString(metric)));
                columnInfoList.Add(column);
            }

            columnInfoList.Add(new ExecutionCountColumnInfo());
            metrics.AppendLine(QueryTemplates.GetExecutionCountText(selectedMetric, statsTableAlias));

            columnInfoList.Add(new NumPlansColumnInfo());
            metrics.AppendLine(QueryTemplates.GetPlanCountText());

            if (configuration.IsQDSROAvailable)
            {
                metrics.AppendLine($", {statsTableAlias}.replica_group_id");
            }

            var replicaFilter = configuration.IsQDSROAvailable ? $"    AND {statsTableAlias}.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            var replicaGroupBy = configuration.IsQDSROAvailable ? $"   , {statsTableAlias}.replica_group_id " : string.Empty;

            return QueryTemplates.GenerateHighVariationCteTemplate(
                addTopClause ? QueryGeneratorUtils.RowsToReturnString(configuration.ReturnAllQueries) : string.Empty,
                metrics.ToString().TrimEnd(),
                statsTableName,
                statsTableAlias,
                endTimeParameter,
                startTimeParameter,
                replicaFilter,
                replicaGroupBy);
        }

        /// <summary>
        /// This method generates a dynamic query.
        /// </summary>
        /// <param name="statistic">Selected Statistic</param>
        /// <param name="availableMetrics">List of supported metrics</param>
        /// <param name="table1">Table 1 contains columns from runtime stats</param>
        /// <param name="table2">Table 2 contains columns from wait stats (Wait time for now)</param>
        /// <param name="columnInfoList">List of ColumnInfo to help keep track of the order of column</param>
        /// <returns></returns>
        private static string GetDetailFinalSelects(
            Statistic statistic,
            IList<Metric> availableMetrics, string table1, string table2,
            out IList<ColumnInfo> columnInfoList)
        {
            // Construct the following columns (in this specific order):
            // 1. Query ID
            // 2. Parent Object ID
            // 3. Parent Object Name
            // 4. Query Text
            // 5. [Given Statistic] [Metric] Column
            // ...........................
            // ......[REPEATED FOR].......
            // ......[ALL METRICES].......
            // ...........................
            // ...........................
            // 6. Execution Count
            // 7. Number of Plans

            columnInfoList = new List<ColumnInfo>();
            var builder = new StringBuilder();

            // Construct new QueryID, ObjectID, ObjectName and QueryText columns
            QueryIdColumnInfo queryIdColumn = new QueryIdColumnInfo();
            ObjectIdColumnInfo objectIdColumn = new ObjectIdColumnInfo();
            ObjectNameColumnInfo objectNameColumn = new ObjectNameColumnInfo();
            QueryTextColumnInfo queryTextColumn = new QueryTextColumnInfo();
            columnInfoList.Add(queryIdColumn);
            columnInfoList.Add(objectIdColumn);
            columnInfoList.Add(objectNameColumn);
            columnInfoList.Add(queryTextColumn);

            builder.AppendLine(string.Format(@"    {0}.query_id {1},", table1, queryIdColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(@"    {0}.object_id {1},", table1, objectIdColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(@"    {0}.object_name {1},", table1, objectNameColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(@"    {0}.query_sql_text {1},", table1, queryTextColumn.GetQueryColumnLabel()));

            // For detailed summary, we return the following for each metric:
            // Calculated regression, recent metric statistic and historical metric statistic
            // Retrieve for all metric except for "ExecutionCount"
            foreach (Metric metric in availableMetrics)
            {
                var column = new StatisticMetricColumnInfo(statistic, metric);
                columnInfoList.Add(column);
                if (metric.Equals(Metric.WaitTime))
                {
                    builder.AppendLine(string.Format(@"    ISNULL({0}.{1},0) {1},", table2, column.GetQueryColumnLabel()));
                    continue;
                }
                builder.AppendLine(string.Format(@"    {0}.{1} {1},", table1, column.GetQueryColumnLabel()));
            }

            // Construct new ExecutionCount and NumberOfPlans columns
            ExecutionCountColumnInfo execCountColumn = new ExecutionCountColumnInfo();
            NumPlansColumnInfo numPlansColumn = new NumPlansColumnInfo();
            columnInfoList.Add(execCountColumn);
            columnInfoList.Add(numPlansColumn);
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    {0}.{1} {1},", table1, execCountColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    {0}.{1} {1}", table1, numPlansColumn.GetQueryColumnLabel()));

            return builder.ToString().TrimEnd();
        }

        #endregion
    }
}
