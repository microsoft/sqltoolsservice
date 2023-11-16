//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlServer.Management.QueryStoreModel.ForcedPlanQueries;
using Microsoft.SqlServer.Management.QueryStoreModel.HighVariation;
using Microsoft.SqlServer.Management.QueryStoreModel.OverallResourceConsumption;
using Microsoft.SqlServer.Management.QueryStoreModel.PlanSummary;
using Microsoft.SqlServer.Management.QueryStoreModel.RegressedQueries;
using Microsoft.SqlServer.Management.QueryStoreModel.TopResourceConsumers;
using Microsoft.SqlServer.Management.QueryStoreModel.TrackedQueries;

#nullable enable

namespace Microsoft.SqlTools.SqlCore.Performance
{
    public static class QueryStoreQueryGenerator
    {
        internal static QueryStoreMetricFetcher MetricFetcher = new QueryStoreMetricFetcher();

        /* 
         * General process is to:
         * 1. Call the unordered query generator to get the list of columns
         * 2. Select the intended ColumnInfo for sorting
         * 3. Call the ordered query generator to get the actual query
         * 4. Prepend any necessary TSQL parameters to the generated query
         * 5. Return the query text
         */

        #region Top Resource Consumers report

        public static string GetTopResourceConsumersSummaryReportQuery(TopResourceConsumersConfiguration config, string? orderByColumnId = null, bool descending = true)
        {
            TopResourceConsumersQueryGenerator.TopResourceConsumersSummary(config, out IList<ColumnInfo> columns);
            ColumnInfo orderByColumn = GetOrderByColumn(orderByColumnId, columns);

            string query = TopResourceConsumersQueryGenerator.TopResourceConsumersSummary(config, orderByColumn, descending, out _);

            Dictionary<string, object> sqlParams = new()
            {
                [QueryGeneratorUtils.ParameterIntervalStartTime] = config.TimeInterval.StartDateTimeOffset,
                [QueryGeneratorUtils.ParameterIntervalEndTime] = config.TimeInterval.EndDateTimeOffset
            };

            if (!config.ReturnAllQueries)
            {
                sqlParams[QueryGeneratorUtils.ParameterResultsRowCount] = config.TopQueriesReturned;
            }

            query = PrependSqlParameters(query, sqlParams);

            return query;
        }

        public static string GetTopResourceConsumersDetailedSummaryReportQuery(TopResourceConsumersConfiguration config, SqlConnection connection, string? orderByColumnId = null, bool descending = true)
        {
            TopResourceConsumersQueryGenerator.TopResourceConsumersDetailedSummary(MetricFetcher.GetAvailableMetrics(connection), config, out IList<ColumnInfo> columns);
            ColumnInfo orderByColumn = GetOrderByColumn(orderByColumnId, columns);

            string query = TopResourceConsumersQueryGenerator.TopResourceConsumersDetailedSummary(MetricFetcher.GetAvailableMetrics(connection), config, orderByColumn, descending, out _);

            Dictionary<string, object> sqlParams = new()
            {
                [QueryGeneratorUtils.ParameterIntervalStartTime] = config.TimeInterval.StartDateTimeOffset,
                [QueryGeneratorUtils.ParameterIntervalEndTime] = config.TimeInterval.EndDateTimeOffset
            };

            if (!config.ReturnAllQueries)
            {
                sqlParams[QueryGeneratorUtils.ParameterResultsRowCount] = config.TopQueriesReturned;
            }

            query = PrependSqlParameters(query, sqlParams);

            return query;
        }

        #endregion

        #region Forced Plans report

        public static string GetForcedPlanQueriesReportQuery(ForcedPlanQueriesConfiguration config, string? orderByColumnId = null, bool descending = true)
        {
            ForcedPlanQueriesQueryGenerator.ForcedPlanQueriesSummary(config, out IList<ColumnInfo> columns);
            ColumnInfo orderByColumn = GetOrderByColumn(orderByColumnId, columns);

            string query = ForcedPlanQueriesQueryGenerator.ForcedPlanQueriesSummary(config, orderByColumn, descending, out IList<ColumnInfo> _);

            if (!config.ReturnAllQueries)
            {
                query = PrependSqlParameters(query, new() { [QueryGeneratorUtils.ParameterResultsRowCount] = config.TopQueriesReturned.ToString() });
            }

            return query;
        }

        #endregion

        #region Tracked Queries report

        public static string GetTrackedQueriesReportQuery(string querySearchText)
        {
            string query = QueryIDSearchQueryGenerator.GetQuery();

            query = PrependSqlParameters(query, new() { [QueryIDSearchQueryGenerator.QuerySearchTextParameter] = querySearchText });

            return query;
        }

        #endregion

        #region High Variation Queries report

        public static string GetHighVariationQueriesSummaryReportQuery(HighVariationConfiguration config, string? orderByColumnId = null, bool descending = true)
        {
            HighVariationQueryGenerator.HighVariationSummary(config, out IList<ColumnInfo> columns);
            ColumnInfo orderByColumn = GetOrderByColumn(orderByColumnId, columns);

            string query = HighVariationQueryGenerator.HighVariationSummary(config, orderByColumn, descending, out _);

            Dictionary<string, object> sqlParams = new()
            {
                [QueryGeneratorUtils.ParameterIntervalStartTime] = config.TimeInterval.StartDateTimeOffset,
                [QueryGeneratorUtils.ParameterIntervalEndTime] = config.TimeInterval.EndDateTimeOffset
            };

            if (!config.ReturnAllQueries)
            {
                sqlParams[QueryGeneratorUtils.ParameterResultsRowCount] = config.TopQueriesReturned;
            }

            query = PrependSqlParameters(query, sqlParams);

            return query;
        }

        public static string GetHighVariationQueriesDetailedSummaryReportQuery(HighVariationConfiguration config, SqlConnection connection, string? orderByColumnId = null, bool descending = true)
        {
            IList<Metric> availableMetrics = MetricFetcher.GetAvailableMetrics(connection);
            HighVariationQueryGenerator.HighVariationDetailedSummary(availableMetrics, config, out IList<ColumnInfo> columns);
            ColumnInfo orderByColumn = GetOrderByColumn(orderByColumnId, columns);

            string query = HighVariationQueryGenerator.HighVariationDetailedSummary(availableMetrics, config, orderByColumn, descending, out _);

            Dictionary<string, object> sqlParams = new()
            {
                [QueryGeneratorUtils.ParameterIntervalStartTime] = config.TimeInterval.StartDateTimeOffset,
                [QueryGeneratorUtils.ParameterIntervalEndTime] = config.TimeInterval.EndDateTimeOffset
            };

            if (!config.ReturnAllQueries)
            {
                sqlParams[QueryGeneratorUtils.ParameterResultsRowCount] = config.TopQueriesReturned;
            }

            query = PrependSqlParameters(query, sqlParams);

            return query;
        }

        #endregion

        #region Overall Resource Consumption report

        public static string GetOverallResourceConsumptionReportQuery(OverallResourceConsumptionConfiguration config, SqlConnection connection)
        {
            string query = OverallResourceConsumptionQueryGenerator.GenerateQuery(MetricFetcher.GetAvailableMetrics(connection), config, out _);

            Dictionary<string, object> sqlParams = new()
            {
                [QueryGeneratorUtils.ParameterIntervalStartTime] = config.SpecifiedTimeInterval.StartDateTimeOffset,
                [QueryGeneratorUtils.ParameterIntervalEndTime] = config.SpecifiedTimeInterval.EndDateTimeOffset
            };

            query = PrependSqlParameters(query, sqlParams);

            return query;
        }

        #endregion

        #region Regressed Queries report

        public static string GetRegressedQueriesSummaryReportQuery(RegressedQueriesConfiguration config)
        {
            string query = RegressedQueriesQueryGenerator.RegressedQuerySummary(config, out _);

            Dictionary<string, object> sqlParams = new()
            {
                [RegressedQueriesQueryGenerator.ParameterRecentStartTime] = config.TimeIntervalRecent.StartDateTimeOffset,
                [RegressedQueriesQueryGenerator.ParameterRecentEndTime] = config.TimeIntervalRecent.EndDateTimeOffset,
                [RegressedQueriesQueryGenerator.ParameterHistoryStartTime] = config.TimeIntervalHistory.StartDateTimeOffset,
                [RegressedQueriesQueryGenerator.ParameterHistoryEndTime] = config.TimeIntervalHistory.EndDateTimeOffset,
                [RegressedQueriesQueryGenerator.ParameterMinExecutionCount] = config.MinExecutionCount
            };

            if (!config.ReturnAllQueries)
            {
                sqlParams[QueryGeneratorUtils.ParameterResultsRowCount] = config.TopQueriesReturned;
            }

            query = PrependSqlParameters(query, sqlParams);


            return query;
        }

        public static string GetRegressedQueriesDetailedSummaryReportQuery(RegressedQueriesConfiguration config, SqlConnection connection)
        {
            string query = RegressedQueriesQueryGenerator.RegressedQueryDetailedSummary(MetricFetcher.GetAvailableMetrics(connection), config, out _);

            Dictionary<string, object> sqlParams = new()
            {
                [RegressedQueriesQueryGenerator.ParameterRecentStartTime] = config.TimeIntervalRecent.StartDateTimeOffset,
                [RegressedQueriesQueryGenerator.ParameterRecentEndTime] = config.TimeIntervalRecent.EndDateTimeOffset,
                [RegressedQueriesQueryGenerator.ParameterHistoryStartTime] = config.TimeIntervalHistory.StartDateTimeOffset,
                [RegressedQueriesQueryGenerator.ParameterHistoryEndTime] = config.TimeIntervalHistory.EndDateTimeOffset,
                [RegressedQueriesQueryGenerator.ParameterMinExecutionCount] = config.MinExecutionCount
            };

            if (!config.ReturnAllQueries)
            {
                sqlParams[QueryGeneratorUtils.ParameterResultsRowCount] = config.TopQueriesReturned;
            }

            query = PrependSqlParameters(query, sqlParams);

            return query;
        }

        #endregion

        #region Plan Summary report

        public static string GetPlanSummaryChartViewQuery(PlanSummaryConfiguration config)
        {
            BucketInterval bucketInterval = BucketInterval.Hour;

            // if interval is specified then select a 'good' interval
            if (config.UseTimeInterval)
            {
                TimeSpan duration = config.TimeInterval.TimeSpan;
                bucketInterval = BucketIntervalUtils.CalculateGoodSubInterval(duration);
            }

            string query = PlanSummaryQueryGenerator.PlanSummaryChartView(config, bucketInterval, out _);

            Dictionary<string, object> sqlParams = new()
            {
                [QueryGeneratorUtils.ParameterQueryId] = config.QueryId
            };

            if (config.UseTimeInterval)
            {
                sqlParams[QueryGeneratorUtils.ParameterIntervalStartTime] = config.TimeInterval.StartDateTimeOffset;
                sqlParams[QueryGeneratorUtils.ParameterIntervalEndTime] = config.TimeInterval.EndDateTimeOffset;
            }

            query = PrependSqlParameters(query, sqlParams);

            return query;
        }

        public static string GetPlanSummaryGridViewQuery(PlanSummaryConfiguration config, string? orderByColumnId = null, bool descending = true)
        {
            PlanSummaryQueryGenerator.PlanSummaryGridView(config, out IList<ColumnInfo> columns);
            ColumnInfo orderByColumn = GetOrderByColumn(orderByColumnId, columns);

            string query = PlanSummaryQueryGenerator.PlanSummaryGridView(config, orderByColumn, descending, out _);

            Dictionary<string, object> sqlParams = new()
            {
                [QueryGeneratorUtils.ParameterQueryId] = config.QueryId
            };

            if (config.UseTimeInterval)
            {
                sqlParams[QueryGeneratorUtils.ParameterIntervalStartTime] = config.TimeInterval.StartDateTimeOffset;
                sqlParams[QueryGeneratorUtils.ParameterIntervalEndTime] = config.TimeInterval.EndDateTimeOffset;
            }

            query = PrependSqlParameters(query, sqlParams);

            return query;
        }

        public static string GetForcedPlanQuery(long queryId, long planId)
        {

            string query = PlanSummaryQueryGenerator.GetForcedPlanQuery();
            Dictionary<string, object> sqlParams = new()
            {
                [QueryGeneratorUtils.ParameterQueryId] = queryId,
                [QueryGeneratorUtils.ParameterPlanId] = planId,
            };

            query = PrependSqlParameters(query, sqlParams);

            return query;
        }

        #endregion

        #region Helpers

        private static ColumnInfo GetOrderByColumn(string? orderByColumnId, IList<ColumnInfo> columnInfoList)
            => orderByColumnId != null ? columnInfoList.First(col => col.GetQueryColumnLabel() == orderByColumnId) : columnInfoList[0];

        /// <summary>
        /// Prepends declarations and definitions of <paramref name="sqlParams"/> to <paramref name="query"/>
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sqlParams"></param>
        /// <returns></returns>
        private static string PrependSqlParameters(string query, Dictionary<string, object> sqlParams)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string key in sqlParams.Keys)
            {
                sb.AppendLine($"DECLARE {key} {GetTSqlRepresentation(sqlParams[key])};");
            }

            sb.AppendLine();
            sb.AppendLine(query);

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Converts an object (that would otherwise be set as a SqlParameter value) to an entirely TSQL representation.
        /// Only s the same subset of object types that Query Store query generators use:
        /// int, long, string, and DateTimeOffset
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns>data type and value portions of a parameter declaration, in the form "INT = 999"</returns>
        public static string GetTSqlRepresentation(object paramValue)
        {
            switch (paramValue)
            {
                case int i:
                    return $"INT = {i}";
                case long l:
                    return $"BIGINT = {l}";
                case string s:
                    return $"NVARCHAR(max) = N'{s.Replace("'", "''")}'";
                case DateTimeOffset dto:
                    return $"DATETIMEOFFSET = '{dto.ToString("O", CultureInfo.InvariantCulture)}'"; // "O" = ISO 8601 standard datetime format
                default:
                    Debug.Fail($"Unhandled TSQL parameter type: '{paramValue.GetType()}'");
                    return $"= {paramValue}";
            }
        }

        #endregion
    }

    public class QueryStoreMetricFetcher
    {
        public virtual IList<Metric> GetAvailableMetrics(SqlConnection connection) => QdsMetadataMapper.GetAvailableMetrics(connection);
    }
}