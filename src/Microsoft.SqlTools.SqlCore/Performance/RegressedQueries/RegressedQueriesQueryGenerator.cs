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
using Microsoft.SqlTools.SqlCore.Performance.WaitStats;

namespace Microsoft.SqlTools.SqlCore.Performance.RegressedQueries
{
    /// <summary>
    /// Class used to generate the queries required for query store UI
    /// </summary>
    public class RegressedQueriesQueryGenerator
    {
        public static readonly string ParameterResultsRowCount = "@results_row_count";
        public static readonly string ParameterRecentStartTime = "@recent_start_time";
        public static readonly string ParameterRecentEndTime = "@recent_end_time";
        public static readonly string ParameterHistoryStartTime = "@history_start_time";
        public static readonly string ParameterHistoryEndTime = "@history_end_time";
        public static readonly string ParameterMinExecutionCount = "@min_exec_count";

        private const string RecentTimeIntervalCalculationTemplate = @"ROUND(recent.{0}_{1}, 2)";
        private const string HistoryTimeIntervalCalculationTemplate = @"ROUND(hist.{0}_{1}, 2)";

        #region Regressed Query Summary

        /// <summary>
        /// Query used to populate the regressed queries table.
        /// The query is divided in 4 parts
        /// 1st part - select metric and statistic columns from history time period and name it hist
        ///             example of column returned from here -
        ///             ROUND(CONVERT(float, SUM(rs.avg_duration*rs.count_executions))*0.001,2) total_duration
        ///             actual value is multiplied by 0.001 ( 0.001 is a multiplication factor which is constant for each
        ///             Metric (duration in this case) refer Metric.cs for more details) and rounded to two decimal points.
        /// 2nd part - select metric and statistic columns from recent time period and name it recent
        ///             example of column returned from here -
        ///             ROUND(CONVERT(float, SUM(rs.avg_logical_io_writes*rs.count_executions))*8,2) total_logical_io_writes
        ///             actual value is multiplied by 8 (which is a constant and different for each Metric (logical writes in this case)
        ///              refer Metric.cs for more details) and rounded to two decimal points.
        /// 3rd part - join the previous two result set (hist and recent) apply formulas and create "results" data set
        ///             example -
        ///             ROUND(CONVERT(float, recent.total_logical_io_writes/recent.count_executions-hist.total_logical_io_writes/hist.count_executions)*(recent.count_executions), 2) additional_logical_io_writes_workload,
        /// 4th part - select columns from "results" data set. These are the final set of required columns.
        /// RegressedQueryDetailedSummary returns a similar query as this only with more columns
        /// </summary>
        /// <param name="configuration">Regressed Queries Configuration.</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the RegressedQueriesPane to help keep track of the order of column</param>
        /// <returns>The query text</returns>
        public static string RegressedQuerySummary(RegressedQueriesConfiguration configuration, out IList<ColumnInfo> columnInfoList)
            => RegressedQuerySummary(configuration, orderByColumn: null, descending: true, out columnInfoList);

        /// <summary>
        /// Query used to populate the regressed queries table.
        /// The query is divided in 4 parts
        /// 1st part - select metric and statistic columns from history time period and name it hist
        ///             example of column returned from here -
        ///             ROUND(CONVERT(float, SUM(rs.avg_duration*rs.count_executions))*0.001,2) total_duration
        ///             actual value is multiplied by 0.001 ( 0.001 is a multiplication factor which is constant for each
        ///             Metric (duration in this case) refer Metric.cs for more details) and rounded to two decimal points.
        /// 2nd part - select metric and statistic columns from recent time period and name it recent
        ///             example of column returned from here -
        ///             ROUND(CONVERT(float, SUM(rs.avg_logical_io_writes*rs.count_executions))*8,2) total_logical_io_writes
        ///             actual value is multiplied by 8 (which is a constant and different for each Metric (logical writes in this case)
        ///              refer Metric.cs for more details) and rounded to two decimal points.
        /// 3rd part - join the previous two result set (hist and recent) apply formulas and create "results" data set
        ///             example -
        ///             ROUND(CONVERT(float, recent.total_logical_io_writes/recent.count_executions-hist.total_logical_io_writes/hist.count_executions)*(recent.count_executions), 2) additional_logical_io_writes_workload,
        /// 4th part - select columns from "results" data set. These are the final set of required columns.
        /// RegressedQueryDetailedSummary returns a similar query as this only with more columns
        /// </summary>
        /// <param name="configuration">Regressed Queries Configuration.</param>
        /// <param name="orderByColumn">The order by column</param>
        /// <param name="descending">Whether we want to sort by ascending or descending</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the RegressedQueriesPane to help keep track of the order of column</param>
        /// <returns>The query text</returns>
        public static string RegressedQuerySummary(
            RegressedQueriesConfiguration configuration,
            ColumnInfo orderByColumn,
            bool descending,
            out IList<ColumnInfo> columnInfoList)
        {
            var metric = configuration.SelectedMetric;
            var availableMetrics = new List<Metric> { configuration.SelectedMetric };

            var waitStatsSubQuery = string.Empty;
            var statsViewName = QueryGeneratorUtils.GetStatsViewName(metric);
            var statsAlias = QueryGeneratorUtils.GetStatsViewAlias(metric);

            if (metric.Equals(Metric.WaitTime))
            {
                waitStatsSubQuery = QueryWaitStatsQueryGenerator.GetWaitStatsTableExpression(
                    statsViewName,
                    statisticList: null,
                    includeReplicaGroupId: configuration.IsQDSROAvailable, includeQueryExecutionLastWaitTime: false, addWithClause: false, addSeparator: true, endTime: ParameterHistoryEndTime, startTime: ParameterHistoryStartTime);
            }

            string query = QueryTemplates.GenerateRegressedQueryTemplate(
                waitStatsSubQuery,
                GetCteStatement(configuration, availableMetrics, metric, statsViewName, statsAlias, ParameterHistoryStartTime, ParameterHistoryEndTime),
                GetCteStatement(configuration, availableMetrics, metric, statsViewName, statsAlias, ParameterRecentStartTime, ParameterRecentEndTime),
                GetFinalSelects(configuration, availableMetrics, out columnInfoList),
                GetResultStatement(configuration, availableMetrics, MetricUtils.QueryString(Metric.ExecutionCount)),
                ParameterMinExecutionCount,
                configuration.MinNumberOfQueryPlans,
                GenerateFilterClause(new StatisticMetricRegressionColumnInfo(configuration.SelectedStatistic, configuration.SelectedMetric).GetQueryColumnLabel()));

            query = Utils.AppendOrderBy(query, orderByColumn, isDescending: descending);
            query += Environment.NewLine + "OPTION (MERGE JOIN)";

            return query;
        }

        #endregion

        #region Regressed Query Detailed Summary

        /// <summary>
        /// Query used to populate the detailed regressed queries table.
        /// The query is divided in 4 parts
        /// 1st part - select metric and statistic columns from history time period and name it hist
        ///             example of column returned from here -
        ///             ROUND(CONVERT(float, SUM(rs.avg_duration*rs.count_executions))*0.001,2) total_duration
        ///             actual value is multiplied by 0.001 ( 0.001 is a multiplication factor which is constant for each
        ///             Metric (duration in this case) refer Metric.cs for more details) and rounded to two decimal points.
        /// 2nd part - select metric and statistic columns from recent time period and name it recent
        ///             example of column returned from here -
        ///             ROUND(CONVERT(float, SUM(rs.avg_logical_io_writes*rs.count_executions))*8,2) total_logical_io_writes
        ///             actual value is multiplied by 8 (which is a constant and different for each Metric (logical writes in this case)
        ///              refer Metric.cs for more details) and rounded to two decimal points.
        /// 3rd part - join the previous two result set (hist and recent) apply formulas and create "results" data set
        ///             example -
        ///             ROUND(CONVERT(float, recent.total_logical_io_writes/recent.count_executions-hist.total_logical_io_writes/hist.count_executions)*(recent.count_executions), 2) additional_logical_io_writes_workload,
        /// 4th part - select columns from "results" data set. These are the final set of required columns.
        /// RegressedQuerySummary returns a similar query as this only with fewer columns
        /// </summary>
        /// <param name="availableMetrics">Available metrics for a given database context</param>
        /// <param name="configuration">Regressed Queries Configuration.</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the RegressedQueriesPane to help keep track of the order of column</param>
        /// <returns>Query text and ColumnInfoList. ColumnInfoList must match the query's column order.</returns>
        public static string RegressedQueryDetailedSummary(IList<Metric> availableMetrics, RegressedQueriesConfiguration configuration, out IList<ColumnInfo> columnInfoList)
            => RegressedQueryDetailedSummary(availableMetrics, configuration, orderByColumn: null, descending: true, out columnInfoList);

        /// <summary>
        /// Query used to populate the detailed regressed queries table.
        /// The query is divided in 4 parts
        /// 1st part - select metric and statistic columns from history time period and name it hist
        ///             example of column returned from here -
        ///             ROUND(CONVERT(float, SUM(rs.avg_duration*rs.count_executions))*0.001,2) total_duration
        ///             actual value is multiplied by 0.001 ( 0.001 is a multiplication factor which is constant for each
        ///             Metric (duration in this case) refer Metric.cs for more details) and rounded to two decimal points.
        /// 2nd part - select metric and statistic columns from recent time period and name it recent
        ///             example of column returned from here -
        ///             ROUND(CONVERT(float, SUM(rs.avg_logical_io_writes*rs.count_executions))*8,2) total_logical_io_writes
        ///             actual value is multiplied by 8 (which is a constant and different for each Metric (logical writes in this case)
        ///              refer Metric.cs for more details) and rounded to two decimal points.
        /// 3rd part - join the previous two result set (hist and recent) apply formulas and create "results" data set
        ///             example -
        ///             ROUND(CONVERT(float, recent.total_logical_io_writes/recent.count_executions-hist.total_logical_io_writes/hist.count_executions)*(recent.count_executions), 2) additional_logical_io_writes_workload,
        /// 4th part - select columns from "results" data set. These are the final set of required columns.
        /// RegressedQuerySummary returns a similar query as this only with fewer columns
        /// </summary>
        /// <param name="availableMetrics">Available metrics for a given database context</param>
        /// <param name="configuration">Regressed Queries Configuration.</param>
        /// <param name="orderByColumn">The order by column</param>
        /// <param name="descending">Whether we want to sort by ascending or descending</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the RegressedQueriesPane to help keep track of the order of column</param>
        /// <returns>Query text and ColumnInfoList. ColumnInfoList must match the query's column order.</returns>
        public static string RegressedQueryDetailedSummary(
            IList<Metric> availableMetrics,
            RegressedQueriesConfiguration configuration,
            ColumnInfo orderByColumn,
            bool descending,
            out IList<ColumnInfo> columnInfoList)
        {
            if (availableMetrics.Contains(Metric.WaitTime))
            {
                return RegressedQueryDetailedSummaryWithWaitStats(availableMetrics, configuration, orderByColumn, descending,
                    out columnInfoList);
            }

            var selectedMetric = Metric.Duration;
            var runtimeStatsViewName = QueryGeneratorUtils.GetStatsViewName(selectedMetric);
            var runtimeStatsViewAlias = QueryGeneratorUtils.GetStatsViewAlias(selectedMetric);

            // We have separate template for waitStats
            availableMetrics.Remove(Metric.WaitTime);
            // We don't gather execution count stats for regressed queries because it doesn't make sense
            availableMetrics.Remove(Metric.ExecutionCount);

            string query = QueryTemplates.GenerateRegressedQueryTemplate(
                string.Empty,
                GetCteStatement(configuration, availableMetrics, selectedMetric, runtimeStatsViewName, runtimeStatsViewAlias, ParameterHistoryStartTime, ParameterHistoryEndTime),
                GetCteStatement(configuration, availableMetrics, selectedMetric, runtimeStatsViewName, runtimeStatsViewAlias, ParameterRecentStartTime, ParameterRecentEndTime),
                GetFinalSelects(configuration, availableMetrics, out columnInfoList),
                GetResultStatement(configuration, availableMetrics, MetricUtils.QueryString(Metric.ExecutionCount)),
                ParameterMinExecutionCount,
                configuration.MinNumberOfQueryPlans,
                string.Empty);

            query = Utils.AppendOrderBy(query, orderByColumn, isDescending: descending);
            query += Environment.NewLine + "OPTION (MERGE JOIN)";

            return query;
        }
        /// <summary>
        /// Query used to populate the detailed regressed queries table.
        /// The query is divided in 4 parts
        /// 1st part - select metric and statistic columns from history time period and name it hist
        ///             example of column returned from here -
        ///             ROUND(CONVERT(float, SUM(rs.avg_duration*rs.count_executions))*0.001,2) total_duration
        ///             actual value is multiplied by 0.001 ( 0.001 is a multiplication factor which is constant for each
        ///             Metric (duration in this case) refer Metric.cs for more details) and rounded to two decimal points.
        /// 2nd part - select metric and statistic columns from recent time period and name it recent
        ///             example of column returned from here -
        ///             ROUND(CONVERT(float, SUM(rs.avg_logical_io_writes*rs.count_executions))*8,2) total_logical_io_writes
        ///             actual value is multiplied by 8 (which is a constant and different for each Metric (logical writes in this case)
        ///              refer Metric.cs for more details) and rounded to two decimal points.
        /// 3rd part - join the previous two result set (hist and recent) apply formulas and create "results" data set
        ///             example -
        ///             ROUND(CONVERT(float, recent.total_logical_io_writes/recent.count_executions-hist.total_logical_io_writes/hist.count_executions)*(recent.count_executions), 2) additional_logical_io_writes_workload,
        /// 4th part - select columns from "results" data set. These are the final set of required columns.
        /// RegressedQuerySummary returns a similar query as this only with fewer columns
        /// </summary>
        /// <param name="availableMetrics">Available metrics for a given database context</param>
        /// <param name="configuration">Regressed Queries Configuration.</param>
        /// <param name="orderByColumn">The order by column</param>
        /// <param name="descending">Whether we want to sort by ascending or descending</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the RegressedQueriesPane to help keep track of the order of column</param>
        /// <returns>Query text and ColumnInfoList. ColumnInfoList must match the query's column order.</returns>
        internal static string RegressedQueryDetailedSummaryWithWaitStats(
            IList<Metric> availableMetrics,
            RegressedQueriesConfiguration configuration,
            ColumnInfo orderByColumn,
            bool descending,
            out IList<ColumnInfo> columnInfoList)
        {
            var waitMetric = Metric.WaitTime;
            var waitStatsViewName = QueryGeneratorUtils.GetStatsViewName(waitMetric);
            var waitStatsViewAlias = QueryGeneratorUtils.GetStatsViewAlias(waitMetric);
            var durationMetric = Metric.Duration;
            var runtimeStatsViewName = QueryGeneratorUtils.GetStatsViewName(durationMetric);
            var runtimeStatsViewAlias = QueryGeneratorUtils.GetStatsViewAlias(durationMetric);

            var waitStatsMetricList = new List<Metric> { Metric.WaitTime };
            var runtimeStatsMetricList = availableMetrics.Except(new[] { Metric.ExecutionCount, Metric.WaitTime }).ToList();
            var statsMetricList = availableMetrics.Except(new[] { Metric.ExecutionCount }).ToList();

            string waitStatsSubQuery = QueryWaitStatsQueryGenerator.GetWaitStatsTableExpression(
                waitStatsViewName,
                statisticList: null, includeReplicaGroupId: configuration.IsQDSROAvailable,
                includeQueryExecutionLastWaitTime: false, addWithClause: false, addSeparator: true, endTime: ParameterHistoryEndTime, startTime: ParameterHistoryStartTime);
            var executionCount = "wait_stats_count_executions";

            string query = QueryTemplates.GenerateRegressedQueryDetailWithWaitStatsTemplate(
                waitStatsSubQuery,
                GetCteStatement(configuration, waitStatsMetricList, waitMetric, waitStatsViewName, waitStatsViewAlias, ParameterHistoryStartTime, ParameterHistoryEndTime),
                GetCteStatement(configuration, runtimeStatsMetricList, durationMetric, runtimeStatsViewName, runtimeStatsViewAlias, ParameterHistoryStartTime, ParameterHistoryEndTime),
                GetCombinedColumnNames("other_hist", "wait_stats_hist", configuration.SelectedStatistic, statsMetricList),
                GetCteStatement(configuration, waitStatsMetricList, waitMetric, waitStatsViewName, waitStatsViewAlias, ParameterRecentStartTime, ParameterRecentEndTime),
                GetCteStatement(configuration, runtimeStatsMetricList, durationMetric, runtimeStatsViewName, runtimeStatsViewAlias, ParameterRecentStartTime, ParameterRecentEndTime),
                GetCombinedColumnNames("other_recent", "wait_stats_recent", configuration.SelectedStatistic, statsMetricList),
                GetFinalSelects(configuration, statsMetricList, out columnInfoList),
                GetResultStatement(configuration, statsMetricList, executionCount),
                ParameterMinExecutionCount,
                configuration.MinNumberOfQueryPlans);

            query = Utils.AppendOrderBy(query, orderByColumn, isDescending: descending);
            query += Environment.NewLine + "OPTION (MERGE JOIN)";

            return query;
        }

        #endregion Regressed Query Detailed Summary

        #region Helper Methods

        /// <summary>
        /// Returns the query text responsible for calculating regression
        /// There are two types of regression: percentage and additional workload
        /// Percentage = (recent metric - historical metric) / historical metric
        /// Additional workload = (average of recent metric - average of historical metric) * execution count of recent
        /// </summary>
        /// <param name="statistic"></param>
        /// <param name="metric"></param>
        /// <param name="executionCountPrefix"></param>
        /// <returns></returns>
        private static string GetRegressionCalculation(Statistic statistic, Metric metric, string executionCountPrefix)
        {
            // Use additional workload for regression if statistic is total.
            string query = statistic == Statistic.Total ?
                @"ROUND(CONVERT(float, recent.{0}_{1}/recent.{2}-hist.{0}_{1}/hist.{2})*(recent.{2}), 2)" :
                @"ROUND(CONVERT(float, recent.{0}_{1}-hist.{0}_{1})/NULLIF(hist.{0}_{1},0)*100.0, 2)";

            // substituting query identifiers
            return string.Format(CultureInfo.InvariantCulture, query, StatisticUtils.QueryString(statistic), MetricUtils.QueryString(metric), executionCountPrefix);
        }

        /// <summary>
        /// Returns the query text for recent/history time interval calculation for the given statistic and metric.
        /// Template used: RecentTimeIntervalCalculationTemplate or HistoryTimeIntervalCalculationTemplate
        /// </summary>
        /// <param name="statistic"></param>
        /// <param name="metric"></param>
        /// <param name="timeInterval"></param>
        /// <returns>Query String</returns>
        private static string GetTimeIntervalCalculation(Statistic statistic, Metric metric, ComparisonTimeInterval timeInterval)
        {
            switch (timeInterval)
            {
                case ComparisonTimeInterval.History:
                    return string.Format(CultureInfo.InvariantCulture, HistoryTimeIntervalCalculationTemplate, StatisticUtils.QueryString(statistic), MetricUtils.QueryString(metric));
                case ComparisonTimeInterval.Recent:
                    return string.Format(CultureInfo.InvariantCulture, RecentTimeIntervalCalculationTemplate, StatisticUtils.QueryString(statistic), MetricUtils.QueryString(metric));
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Generates a dynamic query based on configurations and other parameters
        /// Template used: QueryTemplate.RegressedQueryCteTemplate
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="availableMetrics"></param>
        /// <param name="selectedMetric"></param>
        /// <param name="statsTableName"></param>
        /// <param name="statsTableAlias"></param>
        /// <param name="startTimeParameter"></param>
        /// <param name="endTimeParameter"></param>
        /// <returns></returns>
        private static string GetCteStatement(RegressedQueriesConfiguration configuration, IList<Metric> availableMetrics,
            Metric selectedMetric, string statsTableName, string statsTableAlias, string startTimeParameter, string endTimeParameter)
        {
            var statistic = configuration.SelectedStatistic;
            StringBuilder metrics = new StringBuilder();

            // Retrieve all metrics from the list.
            foreach (Metric metric in availableMetrics)
            {
                metrics.AppendLine(
                    string.Format(CultureInfo.InvariantCulture,
                        @"    {0} {1}_{2},",
                        QueryGeneratorUtils.GetRuntimeStatsSummary(statistic, metric, statsTableAlias),
                        StatisticUtils.QueryString(statistic),
                        MetricUtils.QueryString(metric)));
            }

            metrics.AppendLine(QueryTemplates.GetExecutionCountText(selectedMetric, statsTableAlias));
            metrics.AppendLine(QueryTemplates.GetPlanCountText());

            var replicaFilter = configuration.IsQDSROAvailable ? $"    AND {statsTableAlias}.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            return QueryTemplates.GenerateRegressedQueryCteTemplate(
                metrics.ToString().TrimEnd(),
                statsTableName, statsTableAlias,
                endTimeParameter,
                startTimeParameter,
                replicaFilter);
        }

        ///  <summary>
        ///  Query used to retrieve the results block of the main regressed summaries query
        ///  Joins the results from recent and hist CTE
        ///
        ///  Blank spaces in front of the query texts are used to match formatting of the overall query
        ///  </summary>
        /// <param name="availableMetrics">available metrics for a given database context</param>
        /// <param name="configuration"></param>
        /// <param name="executionCountPrefix"></param>
        /// <returns></returns>
        private static string GetResultStatement(RegressedQueriesConfiguration configuration, IEnumerable<Metric> availableMetrics, string executionCountPrefix)
        {
            Statistic statistic = configuration.SelectedStatistic;
            var builder = new StringBuilder();

            // Retrieve all metrics except for "ExecutionCount" metric.
            foreach (Metric metric in availableMetrics)
            {
                builder.AppendLine(
                    string.Format(CultureInfo.InvariantCulture,
@"    {0} {1},
    {2} {4}_{5}_recent,
    {3} {4}_{5}_hist,",
                        GetRegressionCalculation(statistic, metric, executionCountPrefix),
                        new StatisticMetricRegressionColumnInfo(statistic, metric).GetQueryColumnLabel(),
                        GetTimeIntervalCalculation(statistic, metric, ComparisonTimeInterval.Recent),
                        GetTimeIntervalCalculation(statistic, metric, ComparisonTimeInterval.History),
                        StatisticUtils.QueryString(statistic),
                        MetricUtils.QueryString(metric)));
            }

            return builder.ToString().TrimEnd();
        }

        ///  <summary>
        ///  The final select block of the regressed queries query
        ///  Specifies the list of information we want to retrieve to display in the regressed queries table.
        ///
        ///  Blank spaces in front of the query texts are used to match formatting of the overall query
        ///  </summary>
        /// <param name="availableMetrics">available metrics for a given database context</param>
        /// <param name="configuration"></param>
        ///  <param name="columnInfoList">List of ColumnInfo passed back to the RegressedQueriesPane to help keep track of the order of column</param>
        ///  <returns></returns>
        private static string GetFinalSelects(
            RegressedQueriesConfiguration configuration,
            IList<Metric> availableMetrics,
            out IList<ColumnInfo> columnInfoList)
        {
            // Construct the following columns (in this specific order):
            // 1. Query ID
            // 2. Parent Object ID
            // 3. Parent Object Name
            // 4. Query Text
            // 5. [Statistic] [Metric] Regression
            // 6. [Statistic] [Metric] Recent
            // 7. [Statistic] [Metric] History
            // ...........................
            // ...........................
            // ...........................
            // ......[REPEATED FOR].......
            // ......[ALL METRICES].......
            // ...........................
            // ...........................
            // ...........................
            // ...........................
            // 8. Execution Count Recent
            // 9. Execution Count History
            // 10. Number of Plans

            var statistic = configuration.SelectedStatistic;
            columnInfoList = new List<ColumnInfo>();
            var builder = new StringBuilder();

            // Construct new QueryID, ObjectID, ObjectName and QueryText columns
            QueryIdColumnInfo queryIdColumn = new QueryIdColumnInfo();
            ObjectIdColumnInfo objectIdColumn = new ObjectIdColumnInfo();
            ObjectNameColumnInfo objectNameColumn = new ObjectNameColumnInfo();
            QueryTextColumnInfo queryTextColumn = new QueryTextColumnInfo();

            // Add QueryID and QueryText columns to the column info list
            columnInfoList.Add(queryIdColumn);
            columnInfoList.Add(objectIdColumn);
            columnInfoList.Add(objectNameColumn);
            columnInfoList.Add(queryTextColumn);

            // For detailed summary, we return the following for each metric:
            // Calculated regression, recent metric statistic and historical metric statistic
            // Retrieve for all metric except for "ExecutionCount"
            foreach (Metric metric in availableMetrics)
            {
                // Construct new Regression, StatisticMetricRecent and StatisticMetricHistory columns
                StatisticMetricRegressionColumnInfo regressionColumn = new StatisticMetricRegressionColumnInfo(statistic, metric);
                StatisticMetricTimeColumnInfo statisticMetricRecentColumn = new StatisticMetricTimeColumnInfo(statistic, metric, ComparisonTimeInterval.Recent);
                StatisticMetricTimeColumnInfo statisticMetricHistoryColumn = new StatisticMetricTimeColumnInfo(statistic, metric, ComparisonTimeInterval.History);

                // Add Regression, StatisticMetricRecent and StatisticMetricHistory columns to the query
                builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    results.{0} {0},", regressionColumn.GetQueryColumnLabel()));
                builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    results.{0} {0},", statisticMetricRecentColumn.GetQueryColumnLabel()));
                builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    results.{0} {0},", statisticMetricHistoryColumn.GetQueryColumnLabel()));

                // Add Regression, StatisticMetricRecent and StatisticMetricHistory columns to the column info list
                columnInfoList.Add(regressionColumn);
                columnInfoList.Add(statisticMetricRecentColumn);
                columnInfoList.Add(statisticMetricHistoryColumn);
            }

            // Construct new ExecutionCountRecent, ExecutionCountHistory and NumberOfPlans columns
            ExecutionCountColumnInfo execCountRecentColumn = new ExecutionCountColumnInfo(ComparisonTimeInterval.Recent);
            ExecutionCountColumnInfo execCountHistColumn = new ExecutionCountColumnInfo(ComparisonTimeInterval.History);
            NumPlansColumnInfo numPlansColumn = new NumPlansColumnInfo();

            // Add ExecutionCountRecent, ExecutionCountHistory and NumberOfPlans columns to the column info list
            columnInfoList.Add(execCountRecentColumn);
            columnInfoList.Add(execCountHistColumn);
            columnInfoList.Add(numPlansColumn);

            return QueryTemplates.GenerateRegressedQueryFinalSelectTemplate(
                QueryGeneratorUtils.RowsToReturnString(configuration.ReturnAllQueries),
                queryIdColumn.GetQueryColumnLabel(),
                objectIdColumn.GetQueryColumnLabel(),
                objectNameColumn.GetQueryColumnLabel(),
                queryTextColumn.GetQueryColumnLabel(),
                builder.ToString().TrimEnd(),
                execCountRecentColumn.GetQueryColumnLabel(),
                execCountHistColumn.GetQueryColumnLabel(),
                numPlansColumn.GetQueryColumnLabel()
                );
        }

        /// <summary>
        /// This generates filter clause for passed in column name
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        private static string GenerateFilterClause(string columnName)
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine(string.Empty);
            return b.Append(string.Format("WHERE {0} > 0", columnName)).ToString();
        }

        /// <summary>
        /// This creates a list of all the columns combined from wait stats and runtime stats.
        /// There can be cases where data is present in runtime stats(which is always)
        /// but not in wait stats (when there was 0 wait on the query) that's why ISNULL is used for wait time metric.
        /// </summary>
        /// <param name="table1"></param>
        /// <param name="table2"></param>
        /// <param name="statistic"></param>
        /// <param name="metrics"></param>
        /// <returns></returns>
        private static string GetCombinedColumnNames(string table1, string table2, Statistic statistic, List<Metric> metrics)
        {
            StringBuilder columns = new StringBuilder();

            foreach (var metric in metrics)
            {
                if (metric.Equals(Metric.WaitTime))
                {
                    columns.AppendLine(string.Format(@"    ISNULL({0}.{1}_{2}, 0) {1}_{2},", table2, StatisticUtils.QueryString(statistic), MetricUtils.QueryString(metric)));
                    continue;
                }
                columns.AppendLine(string.Format(@"    {0}.{1}_{2} {1}_{2},", table1, StatisticUtils.QueryString(statistic), MetricUtils.QueryString(metric)));
            }

            return columns.ToString().TrimEnd();
        }

        #endregion
    }
}
