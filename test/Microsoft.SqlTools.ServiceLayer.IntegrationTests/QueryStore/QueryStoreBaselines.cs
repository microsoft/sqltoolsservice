//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.QueryStore
{
    internal static class QueryStoreBaselines
    {
        public const string HandleGetTopResourceConsumersSummaryReportRequest =
@"DECLARE @interval_start_time DATETIMEOFFSET = '2023-06-10T12:34:56.0000000-07:00';
DECLARE @interval_end_time DATETIMEOFFSET = '2023-06-17T12:34:56.0000000-07:00';

With wait_stats AS
(
SELECT
    ws.plan_id plan_id,
    ws.wait_category,
    ROUND(CONVERT(float, SUM(ws.total_query_wait_time_ms)/SUM(ws.total_query_wait_time_ms/ws.avg_query_wait_time_ms))*1,2) avg_query_wait_time,
    ROUND(CONVERT(float, MIN(ws.min_query_wait_time_ms))*1,2) min_query_wait_time,
    ROUND(CONVERT(float, MAX(ws.max_query_wait_time_ms))*1,2) max_query_wait_time,
    ROUND(CONVERT(float, SQRT( SUM(ws.stdev_query_wait_time_ms*ws.stdev_query_wait_time_ms*(ws.total_query_wait_time_ms/ws.avg_query_wait_time_ms))/SUM(ws.total_query_wait_time_ms/ws.avg_query_wait_time_ms)))*1,2) stdev_query_wait_time,
    ROUND(CONVERT(float, SUM(ws.total_query_wait_time_ms))*1,2) total_query_wait_time,
    CAST(ROUND(SUM(ws.total_query_wait_time_ms/ws.avg_query_wait_time_ms),0) AS BIGINT) count_executions,
    MAX(itvl.end_time) last_execution_time,
    MIN(itvl.start_time) first_execution_time
FROM sys.query_store_wait_stats ws
    JOIN sys.query_store_runtime_stats_interval itvl ON itvl.runtime_stats_interval_id = ws.runtime_stats_interval_id
WHERE NOT (itvl.start_time > @interval_end_time OR itvl.end_time < @interval_start_time)
GROUP BY ws.plan_id, ws.runtime_stats_interval_id, ws.wait_category
)
SELECT 
    p.query_id query_id,
    q.object_id object_id,
    ISNULL(OBJECT_NAME(q.object_id),'') object_name,
    qt.query_sql_text query_sql_text,
    ROUND(CONVERT(float, SQRT( SUM(ws.stdev_query_wait_time*ws.stdev_query_wait_time*ws.count_executions)/NULLIF(SUM(ws.count_executions), 0)))*1,2) stdev_query_wait_time,
    MAX(ws.count_executions) count_executions,
    COUNT(distinct p.plan_id) num_plans
FROM wait_stats ws
    JOIN sys.query_store_plan p ON p.plan_id = ws.plan_id
    JOIN sys.query_store_query q ON q.query_id = p.query_id
    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
WHERE NOT (ws.first_execution_time > @interval_end_time OR ws.last_execution_time < @interval_start_time)
GROUP BY p.query_id, qt.query_sql_text, q.object_id
HAVING COUNT(distinct p.plan_id) >= 1
ORDER BY query_id DESC";

        public const string HandleGetTopResourceConsumersDetailedSummaryReportRequest =
@"DECLARE @interval_start_time DATETIMEOFFSET = '2023-06-10T12:34:56.0000000-07:00';
DECLARE @interval_end_time DATETIMEOFFSET = '2023-06-17T12:34:56.0000000-07:00';

With wait_stats AS
(
SELECT
    ws.plan_id plan_id,
    ws.wait_category,
    ROUND(CONVERT(float, SUM(ws.total_query_wait_time_ms)/SUM(ws.total_query_wait_time_ms/ws.avg_query_wait_time_ms))*1,2) avg_query_wait_time,
    ROUND(CONVERT(float, SQRT( SUM(ws.stdev_query_wait_time_ms*ws.stdev_query_wait_time_ms*(ws.total_query_wait_time_ms/ws.avg_query_wait_time_ms))/SUM(ws.total_query_wait_time_ms/ws.avg_query_wait_time_ms)))*1,2) stdev_query_wait_time,
    CAST(ROUND(SUM(ws.total_query_wait_time_ms/ws.avg_query_wait_time_ms),0) AS BIGINT) count_executions,
    MAX(itvl.end_time) last_execution_time,
    MIN(itvl.start_time) first_execution_time
FROM sys.query_store_wait_stats ws
    JOIN sys.query_store_runtime_stats_interval itvl ON itvl.runtime_stats_interval_id = ws.runtime_stats_interval_id
WHERE NOT (itvl.start_time > @interval_end_time OR itvl.end_time < @interval_start_time)
GROUP BY ws.plan_id, ws.runtime_stats_interval_id, ws.wait_category
),
top_wait_stats AS
(
SELECT 
    p.query_id query_id,
    q.object_id object_id,
    ISNULL(OBJECT_NAME(q.object_id),'') object_name,
    qt.query_sql_text query_sql_text,
    ROUND(CONVERT(float, SQRT( SUM(ws.stdev_query_wait_time*ws.stdev_query_wait_time*ws.count_executions)/NULLIF(SUM(ws.count_executions), 0)))*1,2) stdev_query_wait_time,
    MAX(ws.count_executions) count_executions,
    COUNT(distinct p.plan_id) num_plans
FROM wait_stats ws
    JOIN sys.query_store_plan p ON p.plan_id = ws.plan_id
    JOIN sys.query_store_query q ON q.query_id = p.query_id
    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
WHERE NOT (ws.first_execution_time > @interval_end_time OR ws.last_execution_time < @interval_start_time)
GROUP BY p.query_id, qt.query_sql_text, q.object_id
),
top_other_stats AS
(
SELECT 
    p.query_id query_id,
    q.object_id object_id,
    ISNULL(OBJECT_NAME(q.object_id),'') object_name,
    qt.query_sql_text query_sql_text,
    ROUND(CONVERT(float, SQRT( SUM(rs.stdev_clr_time*rs.stdev_clr_time*rs.count_executions)/NULLIF(SUM(rs.count_executions), 0)))*0.001,2) stdev_clr_time,
    ROUND(CONVERT(float, SQRT( SUM(rs.stdev_cpu_time*rs.stdev_cpu_time*rs.count_executions)/NULLIF(SUM(rs.count_executions), 0)))*0.001,2) stdev_cpu_time,
    ROUND(CONVERT(float, SQRT( SUM(rs.stdev_dop*rs.stdev_dop*rs.count_executions)/NULLIF(SUM(rs.count_executions), 0)))*1,0) stdev_dop,
    ROUND(CONVERT(float, SQRT( SUM(rs.stdev_duration*rs.stdev_duration*rs.count_executions)/NULLIF(SUM(rs.count_executions), 0)))*0.001,2) stdev_duration,
    ROUND(CONVERT(float, SQRT( SUM(rs.stdev_logical_io_reads*rs.stdev_logical_io_reads*rs.count_executions)/NULLIF(SUM(rs.count_executions), 0)))*8,2) stdev_logical_io_reads,
    ROUND(CONVERT(float, SQRT( SUM(rs.stdev_logical_io_writes*rs.stdev_logical_io_writes*rs.count_executions)/NULLIF(SUM(rs.count_executions), 0)))*8,2) stdev_logical_io_writes,
    ROUND(CONVERT(float, SQRT( SUM(rs.stdev_log_bytes_used*rs.stdev_log_bytes_used*rs.count_executions)/NULLIF(SUM(rs.count_executions), 0)))*0.0009765625,2) stdev_log_bytes_used,
    ROUND(CONVERT(float, SQRT( SUM(rs.stdev_query_max_used_memory*rs.stdev_query_max_used_memory*rs.count_executions)/NULLIF(SUM(rs.count_executions), 0)))*8,2) stdev_query_max_used_memory,
    ROUND(CONVERT(float, SQRT( SUM(rs.stdev_physical_io_reads*rs.stdev_physical_io_reads*rs.count_executions)/NULLIF(SUM(rs.count_executions), 0)))*8,2) stdev_physical_io_reads,
    ROUND(CONVERT(float, SQRT( SUM(rs.stdev_rowcount*rs.stdev_rowcount*rs.count_executions)/NULLIF(SUM(rs.count_executions), 0)))*1,0) stdev_rowcount,
    ROUND(CONVERT(float, SQRT( SUM(rs.stdev_tempdb_space_used*rs.stdev_tempdb_space_used*rs.count_executions)/NULLIF(SUM(rs.count_executions), 0)))*8,2) stdev_tempdb_space_used,
    SUM(rs.count_executions) count_executions,
    COUNT(distinct p.plan_id) num_plans
FROM sys.query_store_runtime_stats rs
    JOIN sys.query_store_plan p ON p.plan_id = rs.plan_id
    JOIN sys.query_store_query q ON q.query_id = p.query_id
    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
WHERE NOT (rs.first_execution_time > @interval_end_time OR rs.last_execution_time < @interval_start_time)
GROUP BY p.query_id, qt.query_sql_text, q.object_id
)
SELECT 
    A.query_id query_id,
    A.object_id object_id,
    A.object_name object_name,
    A.query_sql_text query_sql_text,
    A.stdev_clr_time stdev_clr_time,
    A.stdev_cpu_time stdev_cpu_time,
    A.stdev_dop stdev_dop,
    A.stdev_duration stdev_duration,
    A.stdev_logical_io_reads stdev_logical_io_reads,
    A.stdev_logical_io_writes stdev_logical_io_writes,
    A.stdev_log_bytes_used stdev_log_bytes_used,
    A.stdev_query_max_used_memory stdev_query_max_used_memory,
    A.stdev_physical_io_reads stdev_physical_io_reads,
    A.stdev_rowcount stdev_rowcount,
    A.stdev_tempdb_space_used stdev_tempdb_space_used,
    ISNULL(B.stdev_query_wait_time,0) stdev_query_wait_time,
    A.count_executions count_executions,
    A.num_plans num_plans
FROM top_other_stats A LEFT JOIN top_wait_stats B on A.query_id = B.query_id and A.query_sql_text = B.query_sql_text and A.object_id = B.object_id
WHERE A.num_plans >= 1
ORDER BY query_id DESC";

        public const string HandleGetForcedPlanQueriesReportRequest = @"";
        public const string HandleGetTrackedQueriesReportRequest = @"";
        public const string HandleGetHighVariationQueriesSummaryReportRequest = @"";
        public const string HandleGetHighVariationQueriesDetailedSummaryReportRequest = @"";
        public const string HandleGetOverallResourceConsumptionReportRequest = @"";
        public const string HandleGetRegressedQueriesSummaryReportRequest = @"";
        public const string HandleGetRegressedQueriesDetailedSummaryReportRequest = @"";
        public const string HandleGetPlanSummaryChartViewRequest = @"";
        public const string HandleGetPlanSummaryGridViewRequest = @"";
    }
}
