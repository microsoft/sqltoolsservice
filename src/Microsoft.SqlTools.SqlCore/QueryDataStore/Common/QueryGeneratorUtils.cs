//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.SqlServer.Management.QueryStoreModel.Common
{
    /// <summary>
    /// A collection of commonly used methods to promote code sharing and maintainability
    /// </summary>
    public class QueryGeneratorUtils
    {
        public const string ParameterQueryId = "@query_id";
        public const string ParameterPlanId = "@plan_id";
        public const string ParameterResultsRowCount = "@results_row_count";
        public const string ParameterIntervalStartTime = "@interval_start_time";
        public const string ParameterIntervalEndTime = "@interval_end_time";
        public const string ParameterWaitCategoryId = "@wait_category";
        public const string ParameterReplicaGroupId = "@replica_group_id";
        public const string ConversionTemplate = @"ROUND({0}*{1},{2})";

        /// <summary>
        /// Common code from QueryGenerators, returns a column definition for the given metric and statistic in sys.query_store_runtime_stats
        /// Output when Statistic = Average and Metric = Logical Read would be
        /// ROUND(CONVERT(float, SUM(rs.avg_logical_io_reads*rs.count_executions))*8,2) total_logical_io_reads
        /// There are 3 part
        /// 1st part - SUM(rs.avg_logical_io_reads*rs.count_executions) - This calculation is based on the selected Statistic (Average here)
        /// and would be different for Min, Max and std dev.
        /// 2nd part - CONVERT(float, (calculation from part1))*8 - Here we convert 1st part to float and multiply it with a multiplication factor (8 here)
        /// multiplication factor is a constant for each Metric (Logical Read in this case) refer Metric.cs for more details.
        /// 3rd part - ROUND((from part2),2) - This is simply rounding output to 2 decimal points
        /// </summary>
        /// <param name="statistic"></param>
        /// <param name="metric"></param>
        /// <param name="statsTableName"> the name given for sys.query_store_runtime_stats or sys.query_store_wait_stats
        /// in the query this column will be added to </param>
        /// <returns></returns>
        public static string GetRuntimeStatsSummary(Statistic statistic, Metric metric, string statsTableName)
        {
            // For the special case of execution count metric, we do not form the query string based on the statistic.
            // Instead we always just return a sum of the execution count of sys.query_store_runtime_stats.
            if (metric.Equals(Metric.ExecutionCount))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "CONVERT(float, SUM({0}.{1}))",
                    statsTableName,
                    MetricUtils.QueryString(metric));
            }

            string summary;
            switch (statistic)
            {
                case Statistic.Avg:
                case Statistic.Min:
                case Statistic.Max:
                case Statistic.Stdev:
                    summary = StatisticUtils.GetAggregationFormulaForRuntimeStats(statistic,
                        StatisticUtils.QueryString(statistic), MetricUtils.QueryString(metric), statsTableName);
                    break;
                case Statistic.Total: // For statistic total, we want to use the avg of each time interval to calculate the total
                    summary = StatisticUtils.GetAggregationFormulaForRuntimeStats(statistic,
                        StatisticUtils.QueryString(Statistic.Avg), MetricUtils.QueryString(metric), statsTableName);
                    break;
                case Statistic.Variation:
                    summary = StatisticUtils.GetAggregationFormulaForRuntimeStats(statistic,
                        StatisticUtils.QueryString(Statistic.Stdev), MetricUtils.QueryString(metric), statsTableName, StatisticUtils.QueryString(Statistic.Avg));
                    return summary;
                default:
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidArgumentMessage, StatisticUtils.LocalizedString(statistic), MetricUtils.LocalizedString(metric)));
            }

            //We need to convert units for each metric but not if statistic is Variation
            return string.Format(CultureInfo.InvariantCulture, ConversionTemplate, summary, metric.GetConversionFactor(), metric.GetRoundOffPoints());
        }

        /// <summary>
        /// Returns aggregation t-sql for selected statistic over wait_time from sys.query_store_wait_stats.
        /// Refer comments from GetRuntimeStatsSummary for more details.
        /// </summary>
        /// <param name="statistic"></param>
        /// <param name="statsTableName"> the name given for sys.query_store_runtime_stats or sys.query_store_wait_stats
        /// in the query this column will be added to </param>
        /// <returns></returns>
        public static string GetWaitStatsSummary(Statistic statistic, string statsTableName)
        {
            string summary;
            switch (statistic)
            {
                case Statistic.Avg:
                case Statistic.Min:
                case Statistic.Max:
                case Statistic.Stdev:
                case Statistic.Total:
                case Statistic.Last:
                    summary = StatisticUtils.GetAggregationFormulaForWaitStats(statistic, statsTableName);
                    break;
                default:
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                        Resources.InvalidArgumentMessage, StatisticUtils.LocalizedString(statistic), MetricUtils.LocalizedString(Metric.WaitTime)));
            }

            return string.Format(CultureInfo.InvariantCulture, ConversionTemplate, summary, Metric.WaitTime.GetConversionFactor(), Metric.WaitTime.GetRoundOffPoints());
        }

        /// <summary>
        /// This method retuns the view name to use when creating queries to fetch stats information.
        /// We currently support two different views sys.query_store_runtime_stats and
        /// sys.query_store_wait_stats. All metrics are available in sys.query_store_runtime_stats
        /// except WaitTime. For wait time we use internal name "wait_stats" which is used to construct queries.
        /// </summary>
        /// <param name="currentMetric"></param>
        /// <returns></returns>
        public static string GetStatsViewName(Metric currentMetric) => currentMetric.Equals(Metric.WaitTime) ? "wait_stats" : "sys.query_store_runtime_stats";

        /// <summary>
        /// This method returns the alias we use for viewnames while we construct dynamic queries.
        /// </summary>
        /// <param name="currentMetric"></param>
        /// <returns></returns>
        internal static string GetStatsViewAlias(Metric currentMetric) => currentMetric.Equals(Metric.WaitTime) ? "ws" : "rs";

        /// <summary>
        /// If returnAllQueries is True, we create dynamic Top(x) syntax
        /// </summary>
        /// <param name="returnAllQueries"></param>
        /// <returns></returns>
        internal static string RowsToReturnString(bool returnAllQueries) => !returnAllQueries ? string.Format(CultureInfo.InvariantCulture, "TOP ({0})", ParameterResultsRowCount) : string.Empty;
    }
}
