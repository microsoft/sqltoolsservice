//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlServer.Management.QueryStoreModel.WaitStats;

namespace Microsoft.SqlServer.Management.QueryStoreModel.PlanSummary
{
    /// <summary>
    /// Class used to generate the queries required for query store UI
    /// </summary>
    public class PlanSummaryQueryGenerator
    {
        #region Plan Summary Chart View

        /// <summary>
        /// base string for PlanSummaryChartView, needs to be formatted with user input
        /// </summary>
        private const string PlanChartQueryTiming =
            @"
        AND NOT ({2}.first_execution_time > {0} OR {2}.last_execution_time < {1})";

        /// <summary>
        /// Builds up a parameterized query string based upon the current metric / statistic combination
        /// Template Used:QueryTemplate.PlanChartSummaryTemplate
        ///
        /// The Query_id is parameterized as @query_id
        ///
        /// The columns of the result set are :
        ///     plan_id
        ///     execution_type
        ///     count_executions
        ///     is_plan_forced
        ///     bucket_start
        ///     bucket_end
        ///     avg_{Metric}
        ///     max_{Metric}
        ///     min_{Metric}
        ///     stdev_{Metric}
        ///     total_{Metric}
        ///
        ///     NOTE that the constants PlanChartQuery_Column{Name} set of constants provide help for indexes and must be kept
        ///     in sync with any changes to the query itself
        /// </summary>
        /// <param name="configuration">Configuration for plan summary pane. Contains information such as QueryID and SelectedMetric</param>
        /// <param name="timeIntervalBucket">The time bucket used for the plan summary query</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to PlanSummaryChart to help keep track of the order of column</param>
        /// <returns></returns>
        public static string PlanSummaryChartView(PlanSummaryConfiguration configuration, BucketInterval timeIntervalBucket, out IList<ColumnInfo> columnInfoList)
        {
            var metric = configuration.SelectedMetric;
            var waitstatsSubQuery = string.Empty;
            var statsAlias = QueryGeneratorUtils.GetStatsViewAlias(metric);
            var statsTableName = QueryGeneratorUtils.GetStatsViewName(metric);

            // if the configuration constrains the time interval we need to add these constraints
            string timingConstraints = configuration.UseTimeInterval ?
                string.Format(CultureInfo.InvariantCulture,
                    PlanChartQueryTiming,
                    QueryGeneratorUtils.ParameterIntervalEndTime,
                    QueryGeneratorUtils.ParameterIntervalStartTime,
                   statsAlias)
                : string.Empty;

            // Metric execution count requires a special query since most of the statistics do not make sense for it.
            if (metric.Equals(Metric.ExecutionCount))
            {
                return PlanSummaryChartViewForExecutionCount(timingConstraints, timeIntervalBucket, out columnInfoList, configuration);
            }

            columnInfoList = new List<ColumnInfo>();

            // Construct the following columns (in this specific order):
            // Plan ID
            // Plan Forced
            // Execution Type
            // Execution Count
            // Bucket Start
            // Bucket End
            // Average Metric
            // Max Metric
            // Min Metric
            // Standard Deviation Metric
            // Variation Metric
            // Total Metric
            PlanIdColumnInfo planIdColumn = new PlanIdColumnInfo();
            PlanForcedColumnInfo planForcedColumn = new PlanForcedColumnInfo();
            ExecutionTypeColumnInfo execTypeColumnInfo = new ExecutionTypeColumnInfo();
            ExecutionCountColumnInfo execCountColumn = new ExecutionCountColumnInfo();
            BucketStartTimeColumnInfo bucketStartColumn = new BucketStartTimeColumnInfo();
            BucketEndTimeColumnInfo bucketEndColumn = new BucketEndTimeColumnInfo();
            StatisticMetricColumnInfo avgMetricColumn = new StatisticMetricColumnInfo(Statistic.Avg, metric);
            StatisticMetricColumnInfo maxMetricColumn = new StatisticMetricColumnInfo(Statistic.Max, metric);
            StatisticMetricColumnInfo minMetricColumn = new StatisticMetricColumnInfo(Statistic.Min, metric);
            StatisticMetricColumnInfo stdevMetricColumn = new StatisticMetricColumnInfo(Statistic.Stdev, metric);
            StatisticMetricColumnInfo variationMetricColumn = new StatisticMetricColumnInfo(Statistic.Variation, metric);
            StatisticMetricColumnInfo totalMetricColumn = new StatisticMetricColumnInfo(Statistic.Total, metric);

            // Add these columns to the column info list
            columnInfoList.Add(planIdColumn);
            columnInfoList.Add(planForcedColumn);
            columnInfoList.Add(execTypeColumnInfo);
            columnInfoList.Add(execCountColumn);
            columnInfoList.Add(bucketStartColumn);
            columnInfoList.Add(bucketEndColumn);
            columnInfoList.Add(avgMetricColumn);
            columnInfoList.Add(maxMetricColumn);
            columnInfoList.Add(minMetricColumn);
            columnInfoList.Add(stdevMetricColumn);
            columnInfoList.Add(variationMetricColumn);
            columnInfoList.Add(totalMetricColumn);

            if (metric.Equals(Metric.WaitTime))
            {
                waitstatsSubQuery = QueryWaitStatsQueryGenerator.GetWaitStatsTableExpression(statsTableName,
                     statisticList: new[] { Statistic.Avg, Statistic.Min, Statistic.Max, Statistic.Stdev, Statistic.Total },
                     includeReplicaGroupId: configuration.IsQDSROAvailable, includeQueryExecutionLastWaitTime: true, addWithClause: false, addSeparator: true);
            }

            var replicaFilter = configuration.IsQDSROAvailable ? $"            AND {statsAlias}.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            return QueryTemplates.GeneratePlanChartSummaryTemplate(
                configuration.ReplicaGroupId,
                MetricUtils.QueryString(metric),
                QueryGeneratorUtils.ParameterQueryId,
                BucketIntervalUtils.DateFunctionIntervalString(timeIntervalBucket),
                QueryGeneratorUtils.GetRuntimeStatsSummary(Statistic.Avg, metric, statsAlias),
                QueryGeneratorUtils.GetRuntimeStatsSummary(Statistic.Max, metric, statsAlias),
                QueryGeneratorUtils.GetRuntimeStatsSummary(Statistic.Min, metric, statsAlias),
                QueryGeneratorUtils.GetRuntimeStatsSummary(Statistic.Stdev, metric, statsAlias),
                QueryGeneratorUtils.GetRuntimeStatsSummary(Statistic.Variation, metric, statsAlias),
                QueryGeneratorUtils.GetRuntimeStatsSummary(Statistic.Total, metric, statsAlias),
                timingConstraints,
                QueryGeneratorUtils.ParameterIntervalStartTime, waitstatsSubQuery, statsAlias, statsTableName, QueryTemplates.GetExecutionCountText(metric, statsAlias),
                configuration.ReplicaGroupId == ReplicaGroup.Primary.ToLong() ? replicaFilter : QueryGeneratorUtils.ParameterReplicaGroupId);
        }

        /// <summary>
        /// Similar to PlanSummaryChartView.
        /// Template Used: QueryTemplate.PlanChartSummaryTemplateForExecutionCount
        /// </summary>
        /// <param name="timingConstraints"></param>
        /// <param name="timeIntervalBucket"></param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to PlanSummaryChart to help keep track of the order of column</param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        internal static string PlanSummaryChartViewForExecutionCount(
            string timingConstraints,
            BucketInterval timeIntervalBucket,
            out IList<ColumnInfo> columnInfoList,
            PlanSummaryConfiguration configuration)
        {
            columnInfoList = new List<ColumnInfo>();

            // Construct the following columns (in this specific order):
            // Plan ID
            // Plan Forced
            // Execution Type
            // Execution Count
            // Bucket Start
            // Bucket End
            var planIdColumn = new PlanIdColumnInfo();
            var planForcedColumn = new PlanForcedColumnInfo();
            var execTypeColumnInfo = new ExecutionTypeColumnInfo();
            var execCountColumn = new ExecutionCountColumnInfo();
            var bucketStartColumn = new BucketStartTimeColumnInfo();
            var bucketEndColumn = new BucketEndTimeColumnInfo();

            // Add these columns to the column info list
            columnInfoList.Add(planIdColumn);
            columnInfoList.Add(planForcedColumn);
            columnInfoList.Add(execTypeColumnInfo);
            columnInfoList.Add(execCountColumn);
            columnInfoList.Add(bucketStartColumn);
            columnInfoList.Add(bucketEndColumn);

            var replicaFilter = configuration.IsQDSROAvailable ? $"        AND rs.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            return QueryTemplates.GeneratePlanChartSummaryTemplateForExecutionCount(
                BucketIntervalUtils.DateFunctionIntervalString(timeIntervalBucket),
                QueryGeneratorUtils.ParameterQueryId,
                timingConstraints,
                planIdColumn.GetQueryColumnLabel(),
                planForcedColumn.GetQueryColumnLabel(),
                execCountColumn.GetQueryColumnLabel(),
                bucketStartColumn.GetQueryColumnLabel(),
                bucketEndColumn.GetQueryColumnLabel(),
                QueryGeneratorUtils.ParameterIntervalStartTime,
                execTypeColumnInfo.GetQueryColumnLabel(),
                replicaFilter);
        }
        #endregion

        #region Forced Plan Query

        /// <summary>
        /// Method to gather the SQL text for a query to get list of forced plans
        /// Template Used: QueryTemplate.PlanQueryBase
        /// </summary>
        /// <returns></returns>
        public static string GetForcedPlanQuery(
            bool runForPrimary)
        {
            if (runForPrimary)
            {
                return QueryTemplates.GeneratePlanQueryBasePrimaryTemplate(
                    QueryGeneratorUtils.ParameterQueryId,
                    QueryGeneratorUtils.ParameterPlanId);
            }
            else
            {
                return QueryTemplates.GeneratePlanQueryBaseSecondaryTemplate(
                    QueryGeneratorUtils.ParameterQueryId,
                    QueryGeneratorUtils.ParameterPlanId,
                    QueryGeneratorUtils.ParameterReplicaGroupId);
            }
        }

        #endregion Forced Plan Query

        #region Plan Summary Grid View

        /// <summary>
        /// PlanSummaryChartView
        /// Template Used: QueryTemplate.PlanGridSummaryTemplate
        /// </summary>
        /// <param name="configuration">Configuration for plan summary pane. Contains information such as QueryID and SelectedMetric</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the TopResourceConsumersPane to help keep track of the order of column</param>
        /// <returns></returns>
        public static string PlanSummaryGridView(PlanSummaryConfiguration configuration, out IList<ColumnInfo> columnInfoList)
            => PlanSummaryGridView(configuration, orderByColumn: null, descending: true, out columnInfoList);

        /// <summary>
        /// PlanSummaryChartView
        /// Template Used: QueryTemplate.PlanGridSummaryTemplate
        /// </summary>
        /// <param name="configuration">Configuration for plan summary pane. Contains information such as QueryID and SelectedMetric</param>
        /// <param name="orderByColumn">The current sorting column</param>
        /// <param name="descending">Whether we want the results in ascending or descending order</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the TopResourceConsumersPane to help keep track of the order of column</param>
        /// <returns></returns>
        public static string PlanSummaryGridView(PlanSummaryConfiguration configuration, ColumnInfo orderByColumn, bool descending, out IList<ColumnInfo> columnInfoList)
        {
            columnInfoList = new List<ColumnInfo>();
            Metric metric = configuration.SelectedMetric;
            var waitstatsSubQuery = string.Empty;
            var statsAlias = QueryGeneratorUtils.GetStatsViewAlias(metric);
            var statsTableName = QueryGeneratorUtils.GetStatsViewName(metric);

            // if the configuration constrains the time interval we need to add these constraints
            string timingConstraints = configuration.UseTimeInterval ?
                string.Format(CultureInfo.InvariantCulture,
                    PlanChartQueryTiming,
                    QueryGeneratorUtils.ParameterIntervalEndTime,
                    QueryGeneratorUtils.ParameterIntervalStartTime,
                    statsAlias)
                : string.Empty;

            // We need to return a special query for when metric is Execution Count
            if (metric.Equals(Metric.ExecutionCount))
            {
                return PlanSummaryGridViewForExecutionCount(orderByColumn, descending, ref columnInfoList, configuration);
            }

            // Construct the following columns (in this specific order):
            // Plan ID
            // Plan Forced
            // Execution Type
            // Execution Count
            // Min Metric
            // Max Metric
            // Average Metric
            // Standard Deviation Metric
            // Variation Metric
            // Last Metric
            // Total Metric
            // First Execution Time
            // Last Execution Time
            PlanIdColumnInfo planIdColumn = new PlanIdColumnInfo();
            PlanForcedColumnInfo planForcedColumn = new PlanForcedColumnInfo();
            ExecutionTypeColumnInfo execTypeColumnInfo = new ExecutionTypeColumnInfo();
            ExecutionCountColumnInfo execCountColumn = new ExecutionCountColumnInfo();
            StatisticMetricColumnInfo minMetricColumn = new StatisticMetricColumnInfo(Statistic.Min, metric);
            StatisticMetricColumnInfo maxMetricColumn = new StatisticMetricColumnInfo(Statistic.Max, metric);
            StatisticMetricColumnInfo avgMetricColumn = new StatisticMetricColumnInfo(Statistic.Avg, metric);
            StatisticMetricColumnInfo stdevMetricColumn = new StatisticMetricColumnInfo(Statistic.Stdev, metric);
            StatisticMetricColumnInfo variationMetricColumn = new StatisticMetricColumnInfo(Statistic.Variation, metric);
            StatisticMetricColumnInfo lastMetricColumn = new StatisticMetricColumnInfo(Statistic.Last, metric);
            StatisticMetricColumnInfo totalMetricColumn = new StatisticMetricColumnInfo(Statistic.Total, metric);
            FirstExecTimeColumnInfo firstExecTimeColumn = new FirstExecTimeColumnInfo();
            LastExecTimeColumnInfo lastExecTimeColumn = new LastExecTimeColumnInfo();

            // Add these columns to the column info list
            columnInfoList.Add(planIdColumn);
            columnInfoList.Add(planForcedColumn);
            columnInfoList.Add(execTypeColumnInfo);
            columnInfoList.Add(execCountColumn);
            columnInfoList.Add(minMetricColumn);
            columnInfoList.Add(maxMetricColumn);
            columnInfoList.Add(avgMetricColumn);
            columnInfoList.Add(stdevMetricColumn);
            columnInfoList.Add(variationMetricColumn);
            columnInfoList.Add(lastMetricColumn);
            columnInfoList.Add(totalMetricColumn);
            columnInfoList.Add(firstExecTimeColumn);
            columnInfoList.Add(lastExecTimeColumn);

            if (metric.Equals(Metric.WaitTime))
            {
                waitstatsSubQuery = QueryWaitStatsQueryGenerator.GetWaitStatsTableExpression(statsTableName,
                    statisticList: new[] { Statistic.Avg, Statistic.Min, Statistic.Max, Statistic.Stdev, Statistic.Total, Statistic.Last },
                    includeReplicaGroupId: configuration.IsQDSROAvailable,
                    includeQueryExecutionLastWaitTime: true, addWithClause: false, addSeparator: true);
            }

            // Form the final query based on these columns
            string query = QueryTemplates.GeneratePlanGridSummaryTemplate(
                MetricUtils.QueryString(metric),
                QueryGeneratorUtils.ParameterQueryId,
                planIdColumn.GetQueryColumnLabel(),
                planForcedColumn.GetQueryColumnLabel(),
                execCountColumn.GetQueryColumnLabel(),
                QueryGeneratorUtils.GetRuntimeStatsSummary(Statistic.Min, configuration.SelectedMetric, statsAlias),
                minMetricColumn.GetQueryColumnLabel(),
                QueryGeneratorUtils.GetRuntimeStatsSummary(Statistic.Max, configuration.SelectedMetric, statsAlias),
                maxMetricColumn.GetQueryColumnLabel(),
                QueryGeneratorUtils.GetRuntimeStatsSummary(Statistic.Avg, configuration.SelectedMetric, statsAlias),
                avgMetricColumn.GetQueryColumnLabel(),
                QueryGeneratorUtils.GetRuntimeStatsSummary(Statistic.Stdev, configuration.SelectedMetric, statsAlias),
                stdevMetricColumn.GetQueryColumnLabel(),
                QueryGeneratorUtils.GetRuntimeStatsSummary(Statistic.Variation, configuration.SelectedMetric, statsAlias),
                variationMetricColumn.GetQueryColumnLabel(),
                lastMetricColumn.GetQueryColumnLabel(),
                QueryGeneratorUtils.GetRuntimeStatsSummary(Statistic.Total, configuration.SelectedMetric, statsAlias),
                totalMetricColumn.GetQueryColumnLabel(),
                firstExecTimeColumn.GetQueryColumnLabel(),
                lastExecTimeColumn.GetQueryColumnLabel(),
                timingConstraints,
                QueryGeneratorUtils.ParameterIntervalStartTime,
                execTypeColumnInfo.GetQueryColumnLabel(),
                waitstatsSubQuery,
                statsTableName,
                statsAlias,
                QueryTemplates.GetExecutionCountText(metric, statsAlias),
                QueryGeneratorUtils.ParameterReplicaGroupId);

            return query = Utils.AppendOrderBy(query, orderByColumn, isDescending: descending);
        }

        /// <summary>
        /// Get the plan summary query when metric is ExecutionCount.
        /// Template Used: QueryTemplate.PlanGridSummaryTemplateForExecutionCount
        /// Statistics do not make sense for execution count so we will not include them in the query.
        /// </summary>
        /// <param name="orderByColumn"></param>
        /// <param name="descending"></param>
        /// <param name="columnInfoList"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private static string PlanSummaryGridViewForExecutionCount(ColumnInfo orderByColumn, bool descending, ref IList<ColumnInfo> columnInfoList, PlanSummaryConfiguration configuration)
        {
            // Construct the following columns (in this specific order):
            // 1. Plan ID
            // 2. Plan Forced
            // 3. Execution Count
            // 4. First Execution Time
            // 5. Last Execution Time
            PlanIdColumnInfo planIdColumn = new PlanIdColumnInfo();
            PlanForcedColumnInfo planForcedColumn = new PlanForcedColumnInfo();
            ExecutionTypeColumnInfo execTypeColumnInfo = new ExecutionTypeColumnInfo();
            ExecutionCountColumnInfo execCountColumn = new ExecutionCountColumnInfo();
            FirstExecTimeColumnInfo firstExecTimeColumn = new FirstExecTimeColumnInfo();
            LastExecTimeColumnInfo lastExecTimeColumn = new LastExecTimeColumnInfo();

            // Add these columns to the column info list
            columnInfoList.Add(planIdColumn);
            columnInfoList.Add(planForcedColumn);
            columnInfoList.Add(execTypeColumnInfo);
            columnInfoList.Add(execCountColumn);
            columnInfoList.Add(firstExecTimeColumn);
            columnInfoList.Add(lastExecTimeColumn);

            var replicaFilter = configuration.IsQDSROAvailable ? $" AND rs.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            // Form the final query based on these columns
            string query = QueryTemplates.GeneratePlanGridSummaryTemplateForExecutionCount(
                configuration.ReplicaGroupId,
                planIdColumn.GetQueryColumnLabel(),
                planForcedColumn.GetQueryColumnLabel(),
                QueryGeneratorUtils.GetRuntimeStatsSummary(Statistic.Total, Metric.ExecutionCount, "rs"),
                execCountColumn.GetQueryColumnLabel(),
                firstExecTimeColumn.GetQueryColumnLabel(),
                lastExecTimeColumn.GetQueryColumnLabel(),
                QueryGeneratorUtils.ParameterQueryId,
                execTypeColumnInfo.GetQueryColumnLabel(),
                configuration.ReplicaGroupId == ReplicaGroup.Primary.ToLong() ? replicaFilter : QueryGeneratorUtils.ParameterReplicaGroupId);

            return Utils.AppendOrderBy(query, orderByColumn, isDescending: descending);
        }

        #endregion
    }
}
