//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.SqlTools.SqlCore.QueryDataStore.Common;

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.OverallResourceConsumption
{
    /// <summary>
    /// Class which generates the queries for the Overall Resource Consumption View
    /// </summary>
    public static class OverallResourceConsumptionQueryGenerator
    {

        //constants used in the queries
        private static readonly string BucketStart = "bucket_start";
        private static readonly string BucketEnd = "bucket_end";

        /// <summary>
        /// Generate a SQL query to collect the time history statistics for all of the overall metrics in a single Query.
        /// Template Used : QueryTemplates.OverallResourceConsumptionTemplate
        /// Query generated from this helper function is also presented to clients so indentation is really important to maintain.
        /// </summary>
        /// <param name="availableMetrics">Currently supported Metrics</param>
        /// <param name="configuration">Currently selected configurations</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to help keep track of the order of column</param>
        /// <returns></returns>
        public static string GenerateQuery(IList<Metric> availableMetrics, OverallResourceConsumptionConfiguration configuration, out IList<ColumnInfo> columnInfoList)
            => GenerateQuery(availableMetrics, configuration, orderByColumn: null, descending: true, out columnInfoList);

        /// <summary>
        /// Generate a SQL query to collect the time history statistics for all of the overall metrics in a single Query.
        /// Template Used : QueryTemplates.OverallResourceConsumptionTemplate
        /// Query generated from this helper function is also presented to clients so indentation is really important to maintain.
        /// </summary>
        /// <param name="availableMetrics">Currently supported Metrics</param>
        /// <param name="configuration">Currently selected configurations</param>
        /// <param name="orderByColumn">Results to be sorted by this column</param>
        /// <param name="descending">sort result to be Asc or Desc</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to help keep track of the order of column</param>
        /// <returns></returns>
        public static string GenerateQuery(
            IList<Metric> availableMetrics,
            OverallResourceConsumptionConfiguration configuration,
            ColumnInfo orderByColumn,
            bool descending,
            out IList<ColumnInfo> columnInfoList)
        {
            BucketInterval bucketInterval = configuration.SelectedBucketInterval;
            if (bucketInterval == BucketInterval.Automatic)
            {
                bucketInterval = BucketIntervalUtils.CalculateGoodSubInterval(configuration.SpecifiedTimeInterval.TimeSpan);
                System.Diagnostics.Debug.Assert(bucketInterval != BucketInterval.Automatic);
            }

            var timeIntervalSpecification = BucketIntervalUtils.DateFunctionIntervalString(bucketInterval);
            var waitStatsSubQuery = string.Empty;
            var waitStatsAlias = string.Empty;
            StringBuilder columnNames;

            string allMetricsSubQuery = GeListOfColumnsWithoutWaitStats(availableMetrics, out columnInfoList, out columnNames);

            if (availableMetrics.Contains(Metric.WaitTime))
            {
                waitStatsSubQuery = GenerateWaitStatsSubQuery(timeIntervalSpecification, configuration.IsQDSROAvailable);
                waitStatsAlias = ", WaitStats";
                columnInfoList.Add(new StatisticMetricColumnInfo(Statistic.Total, Metric.WaitTime));
                columnNames.Append(string.Format(CultureInfo.InvariantCulture, @"    total_{0},", MetricUtils.QueryString(Metric.WaitTime)));
            }

            columnInfoList.Add(new BucketStartTimeColumnInfo());
            columnInfoList.Add(new BucketEndTimeColumnInfo());

            var replicaFilter = configuration.IsQDSROAvailable ? $"    AND rs.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            // Determine if local time grouping is needed
            // Apply SWITCHOFFSET if DisplayTimeKind is Local, for any bucketInterval 
            // that uses rs.last_execution_time for DATEDIFF.
            var timeSourceForGrouping = QueryStoreCommonConfiguration.DisplayTimeKind == DateTimeKind.Local
                ? $"SWITCHOFFSET(rs.last_execution_time, DATEPART(tz, {QueryGeneratorUtils.ParameterIntervalStartTime}))"
                : "rs.last_execution_time";

            var queryBuilder = new StringBuilder();

            queryBuilder.Append(QueryTemplates.GenerateOverallResourceConsumptionTemplate(
                timeIntervalSpecification,                      // 0
                QueryGeneratorUtils.ParameterIntervalStartTime, // 1
                QueryGeneratorUtils.ParameterIntervalEndTime,   // 2
                waitStatsSubQuery,                              // 3
                allMetricsSubQuery,                             // 4
                BucketStart,                                    // 5
                BucketEnd,                                      // 6
                columnNames.ToString().TrimEnd(),               // 7
                waitStatsAlias,                                 // 8
                replicaFilter,                                  // 9
                timeSourceForGrouping));                        // 10


            string query = queryBuilder.ToString().TrimEnd();

            query = Utils.AppendOrderBy(query, orderByColumn, isDescending: descending);
            query += Environment.NewLine + "OPTION (MAXRECURSION 0)";

            return query;
        }

        #region Private Helpers

        /// <summary>
        /// This method returns list of all Total_[selected metrics] columns 
        /// </summary>
        /// <param name="availableMetrics">Currently selected Metrics</param>
        /// <param name="columnInfoList"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        private static string GeListOfColumnsWithoutWaitStats(
            IEnumerable<Metric> availableMetrics,
            out IList<ColumnInfo> columnInfoList,
            out StringBuilder columnNames)
        {
            columnInfoList = new List<ColumnInfo>();

            StringBuilder columnList = new StringBuilder();
            columnNames = new StringBuilder();

            // Retrieve for all metrics
            foreach (Metric metric in availableMetrics)
            {
                // WaitStat is handled separately
                if (metric.Equals(Metric.WaitTime))
                {
                    continue;
                }

                columnList.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    {0} as total_{1},",
                    QueryGeneratorUtils.GetRuntimeStatsSummary(Statistic.Total, metric, "rs"),
                    MetricUtils.QueryString(metric)));
                columnNames.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    total_{0},",
                    MetricUtils.QueryString(metric)));
                columnInfoList.Add(new StatisticMetricColumnInfo(Statistic.Total, metric));
            }

            return columnList.ToString().TrimEnd();
        }

        /// <summary>
        /// This generates subquery for wait stats
        /// Template Used: QueryTemplate.OverallResourceConsumptionWaitStatsTemplate
        /// </summary>
        /// <param name="timeIntervalSpecification"></param>
        /// <param name="IsQDSROAvailable"></param>
        /// <returns></returns>
        private static string GenerateWaitStatsSubQuery(string timeIntervalSpecification, bool IsQDSROAvailable)
        {
            var replicaFilter = IsQDSROAvailable ? $"    AND ws.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}" : string.Empty;

            return QueryTemplates.GenerateOverallResourceConsumptionWaitStatsTemplate(
                timeIntervalSpecification,
                QueryGeneratorUtils.ParameterIntervalStartTime,
                QueryGeneratorUtils.ParameterIntervalEndTime,
                replicaFilter);
        }

        #endregion
    }
}