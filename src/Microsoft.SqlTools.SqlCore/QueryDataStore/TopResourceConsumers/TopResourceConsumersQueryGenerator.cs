//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlServer.Management.QueryStoreModel.WaitStats;

namespace Microsoft.SqlServer.Management.QueryStoreModel.TopResourceConsumers
{
    /// <summary>
    /// Class used to generate the queries required for query store UI
    /// </summary>
    public class TopResourceConsumersQueryGenerator
    {
        #region Top Resource Consumers Summary

        /// <summary>
        /// Query used to populate the top resource consumer table.
        /// Template Used: QueryTemplate.TopResourceConsumersSummaryTemplate
        /// </summary>
        /// <param name="configuration">Top Resource Consumers Configuration.</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the TopResourceConsumersPane to help keep track of the order of column</param>
        /// <returns></returns>
        public static string TopResourceConsumersSummary(TopResourceConsumersConfiguration configuration, out IList<ColumnInfo> columnInfoList)
            => TopResourceConsumersSummary(configuration, orderByColumn: null, descending: true, out columnInfoList);

        /// <summary>
        /// Query used to populate the top resource consumer table.
        /// Template Used: QueryTemplate.TopResourceConsumersSummaryTemplate
        /// </summary>
        /// <param name="configuration">Top Resource Consumers Configuration.</param>
        /// <param name="orderByColumn">The current sorting column</param>
        /// <param name="descending">Whether we want to sort by ascending or descending</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the TopResourceConsumersPane to help keep track of the order of column</param>
        /// <returns></returns>
        public static string TopResourceConsumersSummary(
            TopResourceConsumersConfiguration configuration,
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
                    statsViewName,
                    statisticList: null,
                    includeReplicaGroupId: configuration.IsQDSROAvailable, includeQueryExecutionLastWaitTime: false, addWithClause: true, addSeparator: false);
            }

            var replicaFilter = configuration.IsQDSROAvailable ? $"    AND {statsAlias}.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            // Construct the query based on these columns
            string query = QueryTemplates.GenerateTopResourceConsumersSummaryTemplate(
                waitstatsSubQuery,
                QueryGeneratorUtils.RowsToReturnString(configuration.ReturnAllQueries),
                GetFinalSelects(metric, statistic, statsAlias, out columnInfoList),
                statsViewName,
                statsAlias,
                QueryGeneratorUtils.ParameterIntervalEndTime,
                QueryGeneratorUtils.ParameterIntervalStartTime,
                configuration.MinNumberOfQueryPlans,
                replicaFilter).Trim();

            return Utils.AppendOrderBy(query, orderByColumn, isDescending: descending);
        }

        #endregion

        #region Detailed Summary

        /// <summary>
        /// Query used to populate the Top Resoure Consumers table.
        /// Template Used: QueryTemplate.TopResourceConsumersDetailSummaryWithWaitStatsTemplate
        /// </summary>
        /// <param name="availableMetrics">available metrics for a given database context</param>
        /// <param name="configuration">Top Resource Consumers Configuration.</param>
        /// <param name="orderByColumn">The current sorting column</param>
        /// <param name="descending">Whether we want to sort by ascending or descending</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the TopResoureConsumersPane to help keep track of the order of column</param>
        /// <returns></returns>
        internal static string TopResourceConsumersDetailedSummaryWithWaitStats(
            IList<Metric> availableMetrics,
            TopResourceConsumersConfiguration configuration,
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
                statisticList: new List<Statistic>() { Statistic.Avg, Statistic.Stdev },
                includeReplicaGroupId: configuration.IsQDSROAvailable, includeQueryExecutionLastWaitTime: false, addWithClause: false, addSeparator: true);

            IList<ColumnInfo> ignore;

            // Create a temporary configuration for CTE because we always want CTE to return all the rows.
            var trcConfigurationForCte = (TopResourceConsumersConfiguration)configuration.Clone();
            trcConfigurationForCte.ReturnAllQueries = true;
            trcConfigurationForCte.IsQDSROAvailable = configuration.IsQDSROAvailable;

            // Construct CTE for wait time statistics
            var top_wait_stats = GetCteStatement(trcConfigurationForCte, waitstatsMetricList, waitTimeMetric, waitstatsViewName, waitstatsViewAlias,
                QueryGeneratorUtils.ParameterIntervalStartTime, QueryGeneratorUtils.ParameterIntervalEndTime, out ignore);

            // Construct CTE for all other statistics
            var top_other_stats = GetCteStatement(trcConfigurationForCte, runtimestatsMetricList, durationMetric, runtimestatsViewName, runtimestatsViewAlias,
                QueryGeneratorUtils.ParameterIntervalStartTime, QueryGeneratorUtils.ParameterIntervalEndTime, out ignore);

            var replicaFilter = configuration.IsQDSROAvailable ? $" AND A.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            // Construct the query based on these columns
            string query = QueryTemplates.GenerateTopResourceConsumersDetailSummaryWithWaitStatsTemplate(
                waitstatsSubQuery,                                                          // {0}
                top_wait_stats,                                                             // {1}
                top_other_stats,                                                            // {2}
                QueryGeneratorUtils.RowsToReturnString(configuration.ReturnAllQueries),     // {3}
                GetDetailFinalSelects(configuration.SelectedStatistic, statsMetricList, "A", "B", out columnInfoList),
                configuration.MinNumberOfQueryPlans,                                       // {5}
                replicaFilter);                                                            // {6}

            return Utils.AppendOrderBy(query, orderByColumn, isDescending: descending);
        }

        /// <summary>
        /// Query used to populate the Top Resoure Consumers table.
        /// Template Used: QueryTemplates.TopResourceConsumersDetailSummaryTemplate
        /// </summary>
        /// <param name="availableMetrics">available metrics for a given database context</param>
        /// <param name="configuration">Top Resource Consumers Configuration.</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the TopResourceConsumersPane to help keep track of the order of column</param>
        /// <returns></returns>
        public static string TopResourceConsumersDetailedSummary(
            IList<Metric> availableMetrics,
            TopResourceConsumersConfiguration configuration,
            out IList<ColumnInfo> columnInfoList)
            => TopResourceConsumersDetailedSummary(availableMetrics, configuration, orderByColumn: null, descending: true, out columnInfoList);

        /// <summary>
        /// Query used to populate the Top Resoure Consumers table.
        /// Template Used: QueryTemplates.TopResourceConsumersDetailSummaryTemplate
        /// </summary>
        /// <param name="availableMetrics">available metrics for a given database context</param>
        /// <param name="configuration">Top Resource Consumers Configuration.</param>
        /// <param name="orderByColumn">The current sorting column</param>
        /// <param name="descending">Whether we want to sort by ascending or descending</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the TopResoureConsumersPane to help keep track of the order of column</param>
        /// <returns></returns>
        public static string TopResourceConsumersDetailedSummary(
            IList<Metric> availableMetrics,
            TopResourceConsumersConfiguration configuration,
            ColumnInfo orderByColumn,
            bool descending,
            out IList<ColumnInfo> columnInfoList)
        {
            if (availableMetrics.Contains(Metric.WaitTime))
            {
                return TopResourceConsumersDetailedSummaryWithWaitStats(availableMetrics, configuration, orderByColumn, descending, out columnInfoList);
            }

            var durationMetric = Metric.Duration;
            var runtimestatsViewName = QueryGeneratorUtils.GetStatsViewName(durationMetric);
            var runtimestatsViewAlias = QueryGeneratorUtils.GetStatsViewAlias(durationMetric);

            var runtimestatsMetricList = availableMetrics.Except(new[] { Metric.ExecutionCount, Metric.WaitTime }).ToList();

            var replicaFilter = configuration.IsQDSROAvailable ? $" AND {runtimestatsViewAlias}.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            // Construct the query based on these columns
            string query = QueryTemplates.GenerateTopResourceConsumersDetailSummaryTemplate(
                GetCteStatement(configuration, runtimestatsMetricList, durationMetric, runtimestatsViewName, runtimestatsViewAlias, QueryGeneratorUtils.ParameterIntervalStartTime, QueryGeneratorUtils.ParameterIntervalEndTime, out columnInfoList),
                configuration.MinNumberOfQueryPlans,                                                       // {1}
                replicaFilter);                                                                            // {2}

            return Utils.AppendOrderBy(query, orderByColumn, isDescending: descending);
        }

        #endregion

        #region Private Helpers

        ///  <summary>
        ///  The final select block of the top resource consumers query
        ///  Specifies the list of information we want to retrieve to display in the top resource consumers table.
        ///
        ///  Blank spaces in front of the query texts are used to match formatting of the overall query
        ///  </summary>
        ///  <param name="metric">The metric of interest for the query</param>
        ///  <param name="statistic">The statistic of interest for the query</param>
        /// <param name="statsTableName">Stats table name of interest. e.g. runtimestats or wait stats</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the TopResourceConsumersPane to help keep track of the order of column</param>
        ///  <returns>The select block text and an ordered list of ColumnInfo</returns>
        private static string GetFinalSelects(Metric metric, Statistic statistic, string statsTableName, out IList<ColumnInfo> columnInfoList)
        {
            columnInfoList = new List<ColumnInfo>();

            var builder = new StringBuilder();

            // Construct the following columns (in this specific order):
            // 1. Query ID
            // 2. Object ID
            // 3. Object Name
            // 4. Query Text
            // 5. Statistic Metric (We do not want this column if metric is execution count)
            // 6. Execution Count
            // 7. Number of Plans
            QueryIdColumnInfo queryIdColumn = new QueryIdColumnInfo();
            ObjectIdColumnInfo objectIdColumn = new ObjectIdColumnInfo();
            ObjectNameColumnInfo objectNameColumn = new ObjectNameColumnInfo();
            QueryTextColumnInfo queryTextColumn = new QueryTextColumnInfo();
            columnInfoList.Add(queryIdColumn);
            columnInfoList.Add(objectIdColumn);
            columnInfoList.Add(objectNameColumn);
            columnInfoList.Add(queryTextColumn);
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    p.query_id {0},", queryIdColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    q.object_id {0},", objectIdColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    ISNULL(OBJECT_NAME(q.object_id),'') {0},", objectNameColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    qt.{0} {0},", queryTextColumn.GetQueryColumnLabel()));

            // We do not want have a statistic metric column if the metric is execution count.
            // It will be redundant with the other execution count column
            if (!metric.Equals(Metric.ExecutionCount))
            {
                bool extendedTooltipData = (Metric.WaitTime == metric && Statistic.Total == statistic);
                StatisticMetricColumnInfo statisticMetricColumn = new StatisticMetricColumnInfo(statistic, metric) { BindRuntimeData = extendedTooltipData };
                columnInfoList.Add(statisticMetricColumn);
                builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    {0} {1},", QueryGeneratorUtils.GetRuntimeStatsSummary(statistic, metric, statsTableName), statisticMetricColumn.GetQueryColumnLabel()));
            }

            ExecutionCountColumnInfo execCountColumn = new ExecutionCountColumnInfo();
            columnInfoList.Add(execCountColumn);
            builder.AppendLine(QueryTemplates.GetExecutionCountText(metric, statsTableName));

            NumPlansColumnInfo numPlansColumn = new NumPlansColumnInfo();
            columnInfoList.Add(numPlansColumn);
            builder.AppendLine(QueryTemplates.GetPlanCountText());

            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// Creates a dynamic sub query for TopResourceConsumers report
        /// Template Used: QueryTemplates.TopResourceConsumersCteTemplate
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="availableMetrics"></param>
        /// <param name="selectedMetric"></param>
        /// <param name="statsTableName"></param>
        /// <param name="statsTableAlias"></param>
        /// <param name="startTimeParameter"></param>
        /// <param name="endTimeParameter"></param>
        /// <param name="columnInfoList"></param>
        /// <returns></returns>
        private static string GetCteStatement(TopResourceConsumersConfiguration configuration, IList<Metric> availableMetrics, Metric selectedMetric,
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

            ExecutionCountColumnInfo execCountColumn = new ExecutionCountColumnInfo();
            columnInfoList.Add(execCountColumn);
            metrics.AppendLine(QueryTemplates.GetExecutionCountText(selectedMetric, statsTableAlias));

            NumPlansColumnInfo numPlansColumn = new NumPlansColumnInfo();
            columnInfoList.Add(numPlansColumn);
            metrics.AppendLine(QueryTemplates.GetPlanCountText());

            if (configuration.IsQDSROAvailable)
            {
                metrics.AppendLine($", {statsTableAlias}.replica_group_id");
            }

            var replicaFilter = configuration.IsQDSROAvailable ? $"    AND {statsTableAlias}.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            var replicaGroupBy = configuration.IsQDSROAvailable ? $"  , {statsTableAlias}.replica_group_id " : string.Empty;

            string query = QueryTemplates.GenerateTopResourceConsumersCteTemplate(
                QueryGeneratorUtils.RowsToReturnString(configuration.ReturnAllQueries),
                metrics.ToString().TrimEnd(),
                statsTableName,
                statsTableAlias,
                endTimeParameter,
                startTimeParameter,
                replicaFilter,
                replicaGroupBy);

            return query;
        }

        /// <summary>
        /// The final select block of the detail top resource consumers query
        /// Specifies the list of information we want to retrieve to display in the top resource consumers table.
        /// </summary>
        /// <param name="statistic">The statistic of interest for the query</param>
        /// <param name="availableMetrics">List of metrics of interest for the query</param>
        /// <param name="table1">RuntimeStats table reference</param>
        /// <param name="table2">WaitStats table reference</param>
        /// <param name="columnInfoList"></param>
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
            // 5. [Statistic] [Metric]
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

            // Construct ExecutionCount and NumberOfPlans columns
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
