//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Globalization;

namespace Microsoft.SqlTools.SqlCore.Performance.Common
{
    /// <summary>
    /// This lists all the query templates used for various reports under query store
    /// </summary>
    internal class QueryTemplates
    {
        #region Regressed Queries

        /// <summary>
        /// Template used for Regressed Queries Sumary Report (Wait stats and runtime stats) + RQ detail report (when wait stats are not exposed) 
        /// </summary>
        /// <param name="waitStatsSubQuery">Table expression for wait stats (only required when wait stat is selected metric)</param>
        /// <param name="historyTimeIntervalCte">Result set for history time interval. Refer RegressedQueryCteTemplate</param>
        /// <param name="recentTimeIntervalCte">Result set for recen time interval. Refer RegressedQueryCteTemplate</param>
        /// <param name="finalSelectColumns">Column to select from combined history and recent data set. Refer RegressedQueryFinalSelectTemplate</param>
        /// <param name="resultStatement">Combined columns to create combined history and recent dataset.</param>
        /// <param name="parameterMinExecutionCount">Filter on execution count</param>
        /// <param name="minNumberOfQueryPlans">Filter on plan count</param>
        /// <param name="additionalFilter">Additional Filter clause on column(can be empty based on selected configuration)</param>
        internal static string GenerateRegressedQueryTemplate(
            string waitStatsSubQuery,
            string historyTimeIntervalCte,
            string recentTimeIntervalCte,
            string finalSelectColumns,
            string resultStatement,
            string parameterMinExecutionCount,
            int minNumberOfQueryPlans,
            string additionalFilter) =>
$@"WITH {waitStatsSubQuery}
hist AS
(
{historyTimeIntervalCte}
),
recent AS
(
{recentTimeIntervalCte}
)
{finalSelectColumns}
FROM
(
SELECT
    hist.query_id query_id,
    q.object_id object_id,
    qt.query_sql_text query_sql_text,
{resultStatement}
    recent.count_executions count_executions_recent,
    hist.count_executions count_executions_hist
FROM hist
    JOIN recent ON hist.query_id = recent.query_id
    JOIN sys.query_store_query q ON q.query_id = hist.query_id
    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
WHERE
    recent.count_executions >= {parameterMinExecutionCount}
) AS results
JOIN
(
SELECT
    p.query_id query_id,
    COUNT(distinct p.plan_id) num_plans
FROM sys.query_store_plan p
GROUP BY p.query_id
HAVING COUNT(distinct p.plan_id) >= {minNumberOfQueryPlans}
) AS queries ON queries.query_id = results.query_id{additionalFilter}";

        /// <summary>
        /// Template used for Regressed Queries Detail Report when wait time stats are exposed
        /// </summary>
        /// <param name="waitStatsSubQuery">Table expression for wait stats</param>
        /// <param name="historyWaittimeIntervalCte">Result set for history time interval (for wait stats). Refer RegressedQueryCteTemplate</param>
        /// <param name="historyRuntimeIntervalCte">Result set for history time interval (for runtime stats). Refer RegressedQueryCteTemplate</param>
        /// <param name="combinedHistoryColumns">Set of columns for combined history data set</param>
        /// <param name="recentWaittimeIntervalCte">Result set for recent time interval (for wait stats). Refer RegressedQueryCteTemplate</param>
        /// <param name="recentRuntimetimeTimeIntervalCte">Result set for recent time interval (for runtime stats). Refer RegressedQueryCteTemplate</param>
        /// <param name="combinedRecentColumns">Set of columns for combined recent data set. [Separate data set is required because wait stats and runtime stats are fetched from separate views]</param>
        /// <param name="combinedHistoryRecentColumns">Column to select from combined history and recent data set. Refer RegressedQueryFinalSelectTemplate</param>
        /// <param name="combinedColumns">Combined columns to create combined (history and recent) dataset.</param>
        /// <param name="parameterMinExecutionCount">Filter on execution count</param>
        /// <param name="minNumberOfQueryPlans">Filter on plan count</param>
        internal static string GenerateRegressedQueryDetailWithWaitStatsTemplate(
            string waitStatsSubQuery,
            string historyWaittimeIntervalCte,
            string historyRuntimeIntervalCte,
            string combinedHistoryColumns,
            string recentWaittimeIntervalCte,
            string recentRuntimetimeTimeIntervalCte,
            string combinedRecentColumns,
            string combinedHistoryRecentColumns,
            string combinedColumns,
            string parameterMinExecutionCount,
            int minNumberOfQueryPlans) =>
$@"WITH
{waitStatsSubQuery}
wait_stats_hist AS
(
{historyWaittimeIntervalCte}
),
other_hist AS
(
{historyRuntimeIntervalCte}
),
hist AS
(
SELECT
    other_hist.query_id,
{combinedHistoryColumns}
    other_hist.count_executions,
    wait_stats_hist.count_executions wait_stats_count_executions,
    other_hist.num_plans
FROM other_hist
    LEFT JOIN wait_stats_hist ON wait_stats_hist.query_id = other_hist.query_id
),
wait_stats_recent AS
(
{recentWaittimeIntervalCte}
),
other_recent AS
(
{recentRuntimetimeTimeIntervalCte}
),
recent AS
(
SELECT
    other_recent.query_id,
{combinedRecentColumns}
    other_recent.count_executions,
    wait_stats_recent.count_executions wait_stats_count_executions,
    other_recent.num_plans
FROM other_recent
    LEFT JOIN wait_stats_recent ON wait_stats_recent.query_id = other_recent.query_id
)
{combinedHistoryRecentColumns}
FROM
(
SELECT
    hist.query_id query_id,
    q.object_id object_id,
    qt.query_sql_text query_sql_text,
{combinedColumns}
    recent.count_executions count_executions_recent,
    hist.count_executions count_executions_hist
FROM hist
    JOIN recent ON hist.query_id = recent.query_id
    JOIN sys.query_store_query q ON q.query_id = hist.query_id
    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
WHERE
    recent.count_executions >= {parameterMinExecutionCount}
) AS results
JOIN
(
SELECT
    p.query_id query_id,
    COUNT(distinct p.plan_id) num_plans
FROM sys.query_store_plan p
GROUP BY p.query_id
HAVING COUNT(distinct p.plan_id) >= {minNumberOfQueryPlans}
) AS queries ON queries.query_id = results.query_id";

        /// <summary>
        /// This creates a list of columns to select.
        /// </summary>
        /// <param name="rowsToReturn">Return top results</param>
        /// <param name="queryIdColumnName">display name for query Id</param>
        /// <param name="objectIdColumnName">display name for object id</param>
        /// <param name="objectNameColumnName">display name for object name</param>
        /// <param name="queryTextIdColumnName">display name for query sql text</param>
        /// <param name="selectedMetricColumnNames">Column names for selected metric and statistic</param>
        /// <param name="execCountRecentColumn">execution count recent</param>
        /// <param name="execCountHistoryColumn">execution count history</param>
        /// <param name="numPlansColumn">Number of plans</param>
        internal static string GenerateRegressedQueryFinalSelectTemplate(
            string rowsToReturn,
            string queryIdColumnName,
            string objectIdColumnName,
            string objectNameColumnName,
            string queryTextIdColumnName,
            string selectedMetricColumnNames,
            string execCountRecentColumn,
            string execCountHistoryColumn,
            string numPlansColumn) =>
$@"SELECT {rowsToReturn}
    results.query_id {queryIdColumnName},
    results.object_id {objectIdColumnName},
    ISNULL(OBJECT_NAME(results.object_id),'') {objectNameColumnName},
    results.query_sql_text {queryTextIdColumnName},
{selectedMetricColumnNames}
    ISNULL(results.{execCountRecentColumn}, 0) {execCountRecentColumn},
    ISNULL(results.{execCountHistoryColumn}, 0) {execCountHistoryColumn},
    queries.num_plans {numPlansColumn}";

        /// <summary>
        /// This template produces sub queries for fetching data set based on selected configurations between given time interval
        /// </summary>
        /// <param name="metrics">Randomly generated list of columns based on configuration</param>
        /// <param name="statsTableName">Underlying view/table to fetch data from</param>
        /// <param name="statsTableAlias">Table/view alias</param>
        /// <param name="endTimeParameter">Time interval end time</param>
        /// <param name="startTimeParameter">Time interval start time</param>
        /// <param name="replicaFilter">Replica filter</param>
        internal static string GenerateRegressedQueryCteTemplate(
            string metrics,
            string statsTableName,
            string statsTableAlias,
            string endTimeParameter,
            string startTimeParameter,
            string replicaFilter) =>
$@"SELECT
    p.query_id query_id,
{metrics}
FROM {statsTableName} {statsTableAlias}
    JOIN sys.query_store_plan p ON p.plan_id = {statsTableAlias}.plan_id
WHERE
    NOT ({statsTableAlias}.first_execution_time > {endTimeParameter} OR {statsTableAlias}.last_execution_time < {startTimeParameter})
{replicaFilter}
GROUP BY p.query_id";

        #endregion

        #region Overall Resource Consumption

        /// <summary>
        /// Template used for generating query for ORC for both runtime and wait stats
        /// </summary>
        /// <param name="timeIntervalSpecification">Time interval specification</param>
        /// <param name="parameterIntervalStartTime">Start time parameter</param>
        /// <param name="parameterIntervalEndTime">End time parameter</param>
        /// <param name="waitStatsSubQuery">Wait stats sub query. Refer OverallResourceConsumptionWaitStatsTemplate</param>
        /// <param name="allMetricsSubQuery">List of columns without wait stats</param>
        /// <param name="BucketStart">Bucket start string</param>
        /// <param name="BucketEnd">Bucket end string</param>
        /// <param name="columnNames">List of combined column names</param>
        /// <param name="waitStatsAlias">wait stats table/view alias</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        /// <param name="timeSourceForGrouping">The time expression to use for grouping (e.g. rs.last_execution_time or SWITCHOFFSET(rs.last_execution_time, ...))</param>
        internal static string GenerateOverallResourceConsumptionTemplate(
            string timeIntervalSpecification,
            string parameterIntervalStartTime,
            string parameterIntervalEndTime,
            string waitStatsSubQuery,
            string allMetricsSubQuery,
            string BucketStart,
            string BucketEnd,
            string columnNames,
            string waitStatsAlias,
            string replicaFilter,
            string timeSourceForGrouping) =>
$@"WITH DateGenerator AS
(
SELECT CAST({parameterIntervalStartTime} AS DATETIME) DatePlaceHolder
UNION ALL
SELECT  DATEADD({timeIntervalSpecification}, 1, DatePlaceHolder)
FROM    DateGenerator
WHERE   DATEADD({timeIntervalSpecification}, 1, DatePlaceHolder) < {parameterIntervalEndTime}
), {waitStatsSubQuery}
UnionAll AS
(
SELECT
{allMetricsSubQuery}
    TODATETIMEOFFSET(DATEADD({timeIntervalSpecification}, ((DATEDIFF({timeIntervalSpecification}, 0, {timeSourceForGrouping}))), 0), DATEPART(tz, @interval_start_time)) as {BucketStart},
    TODATETIMEOFFSET(DATEADD({timeIntervalSpecification}, (1 + (DATEDIFF({timeIntervalSpecification}, 0, {timeSourceForGrouping}))), 0), DATEPART(tz, @interval_start_time)) as {BucketEnd}
FROM sys.query_store_runtime_stats rs
WHERE 
    NOT (rs.first_execution_time > {parameterIntervalEndTime} OR rs.last_execution_time < {parameterIntervalStartTime})
{replicaFilter}
GROUP BY DATEDIFF({timeIntervalSpecification}, 0, {timeSourceForGrouping})
)
SELECT 
{columnNames}
    {BucketStart},
    {BucketEnd}
FROM
(
SELECT *, ROW_NUMBER() OVER (PARTITION BY {BucketStart} ORDER BY {BucketStart}, total_duration DESC) AS RowNumber
FROM UnionAll {waitStatsAlias}
) as UnionAllResults
WHERE UnionAllResults.RowNumber = 1
";

        /// <summary>
        /// Sub query to creare waittime stats table expression.
        /// </summary>
        /// <param name="timeIntervalSpecification">Time interval function</param>
        /// <param name="parameterIntervalStartTime">Start time parameter</param>
        /// <param name="parameterIntervalEndTime">End time parameter</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        internal static string GenerateOverallResourceConsumptionWaitStatsTemplate(
            string timeIntervalSpecification,
            string parameterIntervalStartTime,
            string parameterIntervalEndTime,
            string replicaFilter) =>
$@"WaitStats AS
(
SELECT
    ROUND(CONVERT(float, SUM(ws.total_query_wait_time_ms))*1,2) total_query_wait_time
FROM sys.query_store_wait_stats ws
    JOIN sys.query_store_runtime_stats_interval itvl ON itvl.runtime_stats_interval_id = ws.runtime_stats_interval_id
WHERE
    NOT (itvl.start_time > {parameterIntervalEndTime} OR itvl.end_time < {parameterIntervalStartTime})
{replicaFilter}
GROUP BY DATEDIFF({timeIntervalSpecification}, 0, itvl.end_time)
),";

        #endregion region

        #region HighVariation Queries

        /// <summary>
        /// This creates query for High Variation summary for both runtime and wait stats.
        /// </summary>
        /// <param name="waitstatsSubQuery">Table Expression for waittime stats. Refer WaitStatsViewTemplate</param>
        /// <param name="rowsToReturn">Top results returned</param>
        /// <param name="finalSelect">List of columns to select from wait and runtime stats</param>
        /// <param name="statsViewName">underlying table/view name</param>
        /// <param name="statsAlias">table/view alias</param>
        /// <param name="parameterIntervalEndTime">end time parameter</param>
        /// <param name="parameterIntervalStartTime">start time parameter</param>
        /// <param name="minNumberOfQueryPlans">Filter on plan count</param>
        /// <param name="execCountColumnName">count_execution column name (filter is required to only show variations if execution count is > 1)</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        internal static string GenerateHighVariationQueryTemplate(
            string waitstatsSubQuery,
            string rowsToReturn,
            string finalSelect,
            string statsViewName,
            string statsAlias,
            string parameterIntervalEndTime,
            string parameterIntervalStartTime,
            int minNumberOfQueryPlans,
            string execCountColumnName,
            string replicaFilter) =>
$@"{waitstatsSubQuery}
SELECT {rowsToReturn}
{finalSelect}
FROM {statsViewName} {statsAlias}
    JOIN sys.query_store_plan p ON p.plan_id = {statsAlias}.plan_id
    JOIN sys.query_store_query q ON q.query_id = p.query_id
    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
WHERE
    NOT ({statsAlias}.first_execution_time > {parameterIntervalEndTime} OR {statsAlias}.last_execution_time < {parameterIntervalStartTime})
{replicaFilter}
GROUP BY p.query_id, qt.query_sql_text, q.object_id
HAVING COUNT(distinct p.plan_id) >= {minNumberOfQueryPlans} AND SUM({statsAlias}.{execCountColumnName}) > 1";

        /// <summary>
        /// This creates query for High Variation detail report when wait stats are exposed
        /// </summary>
        /// <param name="waitstatsSubQuery">Table Expression for waittime stats. Refer WaitStatsViewTemplate</param>
        /// <param name="waitStatscteStatement">wait stats variation data set</param>
        /// <param name="runtimeStatscteStatement">runtime stats variation data set</param>
        /// <param name="rowsToReturn">Top results returned</param>
        /// <param name="finalSelects">List of columns to select from wait and runtime stats</param>
        /// <param name="minNumberOfQueryPlans">Filter on plan count</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        internal static string GenerateHighVariationDetailedQueryWithWaitStatsTemplate(
            string waitstatsSubQuery,
            string waitStatscteStatement,
            string runtimeStatscteStatement,
            string rowsToReturn,
            string finalSelects,
            int minNumberOfQueryPlans,
            string replicaFilter) =>
$@"WITH {waitstatsSubQuery}
wait_stats_variation AS
(
{waitStatscteStatement}
),
other_stats_variation AS
(
{runtimeStatscteStatement}
)
SELECT {rowsToReturn}
{finalSelects}
FROM other_stats_variation A LEFT JOIN wait_stats_variation B on A.query_id = B.query_id and A.query_sql_text = B.query_sql_text and A.object_id = B.object_id
WHERE A.num_plans >= {minNumberOfQueryPlans} AND A.count_executions > 1{replicaFilter}";

        /// <summary>
        /// This creates query for High Variation detail report when wait stats are not exposed.
        /// Reusing HighVariationCteTemplate and adding filters as required.
        /// </summary>
        /// <param name="runtimeStatscteStatement">Refer HighVariationCteTemplat</param>
        /// <param name="minNumberOfQueryPlans">Filter on plan count</param>
        /// <param name="runtimestatsViewAlias">Alias for runtime stats view</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        internal static string GenerateHighVariationDetailedQueryTemplate(
            string runtimeStatscteStatement,
            int minNumberOfQueryPlans,
            string runtimestatsViewAlias,
            string replicaFilter) =>
$@"{runtimeStatscteStatement}
HAVING COUNT(distinct p.plan_id) >= {minNumberOfQueryPlans} AND SUM({runtimestatsViewAlias}.count_executions) > 1{replicaFilter}";

        /// <summary>
        /// Sub query to create high variation data set for selected configuration
        /// </summary>
        /// <param name="rowsToReturn">Top results returned</param>
        /// <param name="metrics">List of columns to select from wait/runtime stats</param>
        /// <param name="statsTableName">underlying table/view name</param>
        /// <param name="statsTableAlias">table/view alias</param>
        /// <param name="endTimeParameter">end time parameter</param>
        /// <param name="startTimeParameter">start time parameter</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        /// <param name="replicaGroupBy">GroupBy on replica group id</param>
        internal static string GenerateHighVariationCteTemplate(
            string rowsToReturn,
            string metrics,
            string statsTableName,
            string statsTableAlias,
            string endTimeParameter,
            string startTimeParameter,
            string replicaFilter,
            string replicaGroupBy) =>
$@"SELECT {rowsToReturn}
    p.query_id query_id,
    q.object_id object_id,
    ISNULL(OBJECT_NAME(q.object_id),'') object_name,
    qt.query_sql_text query_sql_text,
{metrics}
FROM {statsTableName} {statsTableAlias}
    JOIN sys.query_store_plan p ON p.plan_id = {statsTableAlias}.plan_id
    JOIN sys.query_store_query q ON q.query_id = p.query_id
    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
WHERE
    NOT ({statsTableAlias}.first_execution_time > {endTimeParameter} OR {statsTableAlias}.last_execution_time < {startTimeParameter})
{replicaFilter}
GROUP BY p.query_id, qt.query_sql_text, q.object_id {replicaGroupBy}";

        #endregion

        #region Top Resource Consumers

        /// <summary>
        /// This creates query for TRC summary for both runtime and wait stats.
        /// </summary>
        /// <param name="waitstatsSubQuery">Table Expression for waittime stats. Refer WaitStatsViewTemplate</param>
        /// <param name="rowsToReturn">Top results returned</param>
        /// <param name="finalSelect">List of columns to select from wait and runtime stats</param>
        /// <param name="statsViewName">underlying table/view name</param>
        /// <param name="statsAlias">table/view alias</param>
        /// <param name="parameterIntervalEndTime">end time parameter</param>
        /// <param name="parameterIntervalStartTime">start time parameter</param>
        /// <param name="minNumberOfQueryPlans">Filter on plan count</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        internal static string GenerateTopResourceConsumersSummaryTemplate(
            string waitstatsSubQuery,
            string rowsToReturn,
            string finalSelect,
            string statsViewName,
            string statsAlias,
            string parameterIntervalEndTime,
            string parameterIntervalStartTime,
            int minNumberOfQueryPlans,
            string replicaFilter) =>
$@"{waitstatsSubQuery}
SELECT {rowsToReturn}
{finalSelect}
FROM {statsViewName} {statsAlias}
    JOIN sys.query_store_plan p ON p.plan_id = {statsAlias}.plan_id
    JOIN sys.query_store_query q ON q.query_id = p.query_id
    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
WHERE
    NOT ({statsAlias}.first_execution_time > {parameterIntervalEndTime} OR {statsAlias}.last_execution_time < {parameterIntervalStartTime})
{replicaFilter}
GROUP BY p.query_id, qt.query_sql_text, q.object_id
HAVING COUNT(distinct p.plan_id) >= {minNumberOfQueryPlans}";

        /// <summary>
        /// This creates query for TRC detail report when wait time stats are exposed
        /// </summary>
        /// <param name="waitstatsSubQuery">Table Expression for waittime stats. Refer WaitStatsViewTemplate</param>
        /// <param name="topWaitStats">Sub query to get TRC on wait time stats</param>
        /// <param name="topOtherStats">Sub query to get TRC on run time stats</param>
        /// <param name="rowsToReturn">Top results returned</param>
        /// <param name="finalSelects">List of columns to select from wait and runtime stats combined</param>
        /// <param name="minNumberOfQueryPlans">Filter on plan count</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        internal static string GenerateTopResourceConsumersDetailSummaryWithWaitStatsTemplate(
            string waitstatsSubQuery,
            string topWaitStats,
            string topOtherStats,
            string rowsToReturn,
            string finalSelects,
            int minNumberOfQueryPlans,
            string replicaFilter) =>
$@"WITH {waitstatsSubQuery}
top_wait_stats AS
(
{topWaitStats}
),
top_other_stats AS
(
{topOtherStats}
)
SELECT {rowsToReturn}
{finalSelects}
FROM top_other_stats A LEFT JOIN top_wait_stats B on A.query_id = B.query_id and A.query_sql_text = B.query_sql_text and A.object_id = B.object_id
WHERE A.num_plans >= {minNumberOfQueryPlans}{replicaFilter}";

        /// <summary>
        /// This creates query for TRC detail report for runtime stats (when wait stats are not exposed)
        /// </summary>
        /// <param name="runtimeStatsCteStatement">Reuse TopResourceConsumersCteTemplate</param>
        /// <param name="minNumberOfQueryPlans">Filter on plan count</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        internal static string GenerateTopResourceConsumersDetailSummaryTemplate(
            string runtimeStatsCteStatement,
            int minNumberOfQueryPlans,
            string replicaFilter) =>
$@"{runtimeStatsCteStatement}
HAVING COUNT(distinct p.plan_id) >= {minNumberOfQueryPlans}{replicaFilter}";

        /// <summary>
        /// Creates sub query to get TRC based on configuration selected
        /// </summary>
        /// <param name="rowsToReturn">Top results returned</param>
        /// <param name="metrics">List of columns to select from wait and runtime stats</param>
        /// <param name="statsTableName">underlying table/view name</param>
        /// <param name="statsTableAlias">table/view alias</param>
        /// <param name="endTimeParameter">End time parameter</param>
        /// <param name="startTimeParameter">Start Time parameter</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        /// <param name="replicaGroupBy">GroupBy on replica group id</param>
        internal static string GenerateTopResourceConsumersCteTemplate(
            string rowsToReturn,
            string metrics,
            string statsTableName,
            string statsTableAlias,
            string endTimeParameter,
            string startTimeParameter,
            string replicaFilter,
            string replicaGroupBy) =>
$@"SELECT {rowsToReturn}
    p.query_id query_id,
    q.object_id object_id,
    ISNULL(OBJECT_NAME(q.object_id),'') object_name,
    qt.query_sql_text query_sql_text,
{metrics}
FROM {statsTableName} {statsTableAlias}
    JOIN sys.query_store_plan p ON p.plan_id = {statsTableAlias}.plan_id
    JOIN sys.query_store_query q ON q.query_id = p.query_id
    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
WHERE
    NOT ({statsTableAlias}.first_execution_time > {endTimeParameter} OR {statsTableAlias}.last_execution_time < {startTimeParameter})
{replicaFilter}
GROUP BY p.query_id, qt.query_sql_text, q.object_id {replicaGroupBy}";

        #endregion region

        #region Wait Stats

        /// <summary>
        /// Query to create table expression for wait time stats.
        /// Wait time stats and runtime stats are stored under different table structure underneath.
        /// So it's not straightforward to join both the data sets.
        /// This query creates a table expression that exposes the wait stats data in a similar
        /// table structure as runtime stats so they both can be combined/joined together easily.
        /// </summary>
        /// <param name="summary">Dynamically generated list of columns based on selected configuration</param>
        /// <param name="endTime">End time parameter</param>
        /// <param name="startTime">Stat time parameter</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        /// <param name="extraGroupBy">Extra group by clause</param>
        internal static string GenerateWaitStatsViewTemplateGroupedByPlanIdIntervalIdWaitCategory(
            string summary,
            string endTime,
            string startTime,
            string replicaFilter,
            string extraGroupBy) =>
$@"SELECT
    ws.plan_id plan_id,
    ws.wait_category,
{summary}
    CAST(ROUND(SUM(ws.total_query_wait_time_ms/ws.avg_query_wait_time_ms),0) AS BIGINT) count_executions,
    MAX(itvl.end_time) last_execution_time,
    MIN(itvl.start_time) first_execution_time
FROM sys.query_store_wait_stats ws
    JOIN sys.query_store_runtime_stats_interval itvl ON itvl.runtime_stats_interval_id = ws.runtime_stats_interval_id
WHERE
    NOT (itvl.start_time > {endTime} OR itvl.end_time < {startTime})
{replicaFilter}
GROUP BY ws.plan_id, ws.runtime_stats_interval_id, ws.wait_category{extraGroupBy}";

        /// <summary>
        /// Query to create table expression for wait time stats.
        /// Wait time stats and runtime stats are stored under different table structure underneath.
        /// So it's not straightforward to join both the data sets.
        /// This query creates a table expression that exposes the wait stats data in a similar
        /// table structure as runtime stats so they both can be combined/joined together easily.
        /// </summary>
        /// <param name="summary">Dynamically generated list of columns based on selected configuration</param>
        /// <param name="endTime">End time parameter</param>
        /// <param name="startTime">Stat time parameter</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        /// <param name="extraGroupBy">Extra group by clause</param>
        internal static string GenerateWaitStatsViewTemplateIncludeLastQueryExecutionWaitTime(
            string summary,
            string endTime,
            string startTime,
            string replicaFilter,
            string extraGroupBy) =>
$@"SELECT
    ws.plan_id plan_id,
    ws.execution_type,
{summary}
    CAST(ROUND(SUM(ws.total_query_wait_time_ms/ws.avg_query_wait_time_ms),0) AS BIGINT) count_executions,
    MAX(itvl.end_time) last_execution_time,
    MIN(itvl.start_time) first_execution_time
    FROM
    (
    SELECT *, LAST_VALUE(last_query_wait_time_ms) OVER (order by plan_id, runtime_stats_interval_id, execution_type, wait_category) last_query_wait_time
    FROM sys.query_store_wait_stats
{replicaFilter}
    )
AS ws
    JOIN sys.query_store_runtime_stats_interval itvl ON itvl.runtime_stats_interval_id = ws.runtime_stats_interval_id
WHERE NOT (itvl.start_time > {endTime} OR itvl.end_time < {startTime})
GROUP BY ws.plan_id, ws.runtime_stats_interval_id, ws.execution_type, ws.wait_category{extraGroupBy}";

        /// <summary>
        /// This creates a query that returns total wait time for each wait category for
        /// a given queryId between a given time interval.
        /// </summary>
        /// <param name="parameterQueryId">query Id</param>
        /// <param name="parameterIntervalEndTime">End time parameter</param>
        /// <param name="parameterIntervalStartTime">start time parameter</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        internal static string GenerateTotalWaitTimePerWaitCategoryForQueryIdTemplate(
            string parameterQueryId,
            string parameterIntervalEndTime,
            string parameterIntervalStartTime,
            string replicaFilter) =>
$@"SELECT
    ws.wait_category_desc WaitCategory,
    SUM(ws.total_query_wait_time_ms) WaitTime
FROM sys.query_store_wait_stats ws
    JOIN sys.query_store_plan p ON p.plan_id = ws.plan_id
    JOIN sys.query_store_runtime_stats_interval itvl ON itvl.runtime_stats_interval_id = ws.runtime_stats_interval_id
WHERE
    NOT (itvl.start_time > {parameterIntervalEndTime} OR itvl.end_time < {parameterIntervalStartTime})
{replicaFilter}
AND p.query_id = {parameterQueryId}
GROUP BY wait_category_desc
ORDER BY WaitTime Desc";

        /// <summary>
        /// This creates a query that returns aggregated wait time for each wait category
        /// between a given time interval
        /// </summary>
        /// <param name="summary">dynamically generated aggregation on selected statistic</param>
        /// <param name="parameterIntervalEndTime">end time parameter</param>
        /// <param name="parameterIntervalStartTime">start time parameter</param>
        /// <param name="rowsToReturn">number of TOP N rows to return</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        internal static string GenerateAggWaitTimePerWaitCategoryTemplate(
            string summary,
            string parameterIntervalEndTime,
            string parameterIntervalStartTime,
            string rowsToReturn,
            string replicaFilter) =>
$@"SELECT {rowsToReturn}
    ws.wait_category wait_category,
    ws.wait_category_desc wait_category_desc,
{summary}
    CAST(ROUND(SUM(ws.total_query_wait_time_ms/ws.avg_query_wait_time_ms),0) AS BIGINT) count_executions
FROM sys.query_store_wait_stats ws
    JOIN sys.query_store_runtime_stats_interval itvl ON itvl.runtime_stats_interval_id = ws.runtime_stats_interval_id
WHERE
    NOT (itvl.start_time > {parameterIntervalEndTime} OR itvl.end_time < {parameterIntervalStartTime})
{replicaFilter}
GROUP BY ws.wait_category, wait_category_desc";

        /// <summary>
        /// This creates a query that returns aggregated wait time per query id for a given wait category
        /// between a given time interval
        /// </summary>
        /// <param name="summary">dynamically generated aggregation on selected statistic</param>
        /// <param name="parameterIntervalEndTime">end time parameter</param>
        /// <param name="parameterIntervalStartTime">start time parameter</param>
        /// <param name="parameterWaitCategoryId">wait_category id</param>
        /// <param name="RowsToReturn">no. of rows returned</param>
        /// <param name="replicaFilter">Filter on replica group id</param>
        internal static string GenerateAggWaitTimePerQueryForWaitCategoryIdTemplate(
            string summary,
            string parameterIntervalEndTime,
            string parameterIntervalStartTime,
            string parameterWaitCategoryId,
            string RowsToReturn,
            string replicaFilter) =>
$@"SELECT {RowsToReturn}
    p.query_id query_id,
    q.object_id object_id,
    ISNULL(OBJECT_NAME(q.object_id),'') object_name,
    qt.query_sql_text query_sql_text,
{summary}
    CAST(ROUND(SUM(ws.total_query_wait_time_ms/ws.avg_query_wait_time_ms),0) AS BIGINT) count_executions
FROM sys.query_store_wait_stats ws
    JOIN sys.query_store_plan p on p.plan_id = ws.plan_id
    JOIN sys.query_store_query q ON q.query_id = p.query_id
    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
    JOIN sys.query_store_runtime_stats_interval itvl ON itvl.runtime_stats_interval_id = ws.runtime_stats_interval_id
WHERE
    NOT (itvl.start_time > {parameterIntervalEndTime} OR itvl.end_time < {parameterIntervalStartTime}) AND ws.wait_category = {parameterWaitCategoryId}
{replicaFilter}
GROUP BY p.query_id, qt.query_sql_text, q.object_id";

        #endregion

        #region Plan Summary Templates

        /// <summary>
        /// Helper method to fill values in PlanChartSummaryTemplate
        /// </summary>
        internal static string GeneratePlanQueryBaseTemplate(
            long replicaGroupId,
            string parameterQueryId,
            string parameterPlanId) => "";

        /// <summary>
        /// Creates a query to return query plan and plan forced bool flag for the given queryId and planId
        /// </summary>
        /// <param name="parameterQueryId">queryId</param>
        /// <param name="parameterPlanId">planId</param>
        internal static string GeneratePlanQueryBasePrimaryTemplate(
            string parameterQueryId,
            string parameterPlanId) =>
$@"SELECT
    p.is_forced_plan,
    p.query_plan
FROM
    sys.query_store_plan p
WHERE
    p.query_id = {parameterQueryId}
    AND p.plan_id = {parameterPlanId}";

        /// <summary>
        /// Creates a query to return query plan and plan forced bool flag for the given queryId and planId
        /// </summary>
        /// <param name="parameterQueryId">queryId</param>
        /// <param name="parameterPlanId">planId</param>
        /// <param name="replicaGroupId">replica group id</param>
        internal static string GeneratePlanQueryBaseSecondaryTemplate(
            string parameterQueryId,
            string parameterPlanId,
            string replicaGroupId) =>
$@"SELECT
    CONVERT(BIT, ISNULL(pfl.plan_forcing_location_id,0)) as is_forced_plan,
    p.query_plan
FROM
    sys.query_store_plan p
    LEFT OUTER JOIN sys.query_store_plan_forcing_locations pfl ON pfl.plan_id = p.plan_id
WHERE
    p.query_id = {parameterQueryId}
    AND p.plan_id = {parameterPlanId}
    AND (pfl.replica_group_id = {replicaGroupId} OR pfl.plan_forcing_location_id IS NULL)";

        #region PlanChartSummary

        /// <summary>
        /// Creates query to get data for chart view plan summary.
        /// </summary>
        private const string PlanChartSummaryPrimaryTemplate =
@"WITH {11}
    bucketizer as 
    (
        SELECT
            {12}.plan_id as plan_id,
            {12}.execution_type as execution_type,
        {14}
            DATEADD({2}, ((DATEDIFF({2}, 0, {12}.last_execution_time))),0 ) as bucket_start,
            DATEADD({2}, (1 + (DATEDIFF({2}, 0, {12}.last_execution_time))), 0) as bucket_end,
            {3} as avg_{0},
            {4} as max_{0},
            {5} as min_{0},
            {6} as stdev_{0},
            {7} as variation_{0},
            {8} as total_{0}
        FROM
            {13} {12}
            JOIN sys.query_store_plan p ON p.plan_id = {12}.plan_id
        WHERE
            p.query_id = {1}{9}
{15}
        GROUP BY
            {12}.plan_id,
            {12}.execution_type,
            DATEDIFF({2}, 0, {12}.last_execution_time)
    ),
    is_forced as
    (
        SELECT is_forced_plan, plan_id
          FROM sys.query_store_plan
    )
SELECT b.plan_id as plan_id,
    is_forced_plan,
    execution_type,
    count_executions,
    SWITCHOFFSET(bucket_start, DATEPART(tz, {10})) AS bucket_start,
    SWITCHOFFSET(bucket_end, DATEPART(tz, {10})) AS bucket_end,
    avg_{0},
    max_{0},
    min_{0},
    stdev_{0},
    variation_{0},
    total_{0}
FROM bucketizer b
JOIN is_forced f ON f.plan_id = b.plan_id";

        /// <summary>
        /// Creates query to get data for chart view plan summary.
        /// </summary>
        private const string PlanChartSummarySecondaryTemplate =
@"WITH {11}
    bucketizer as 
    (
        SELECT
            {12}.plan_id as plan_id,
            {12}.execution_type as execution_type,
        {14}
            DATEADD({2}, ((DATEDIFF({2}, 0, {12}.last_execution_time))),0 ) as bucket_start,
            DATEADD({2}, (1 + (DATEDIFF({2}, 0, {12}.last_execution_time))), 0) as bucket_end,
            {3} as avg_{0},
            {4} as max_{0},
            {5} as min_{0},
            {6} as stdev_{0},
            {7} as variation_{0},
            {8} as total_{0}
        FROM
            {13} {12}
            JOIN sys.query_store_plan p ON p.plan_id = {12}.plan_id
        WHERE
            p.query_id = {1}{9}
            AND {12}.replica_group_id = {15}
        GROUP BY
            {12}.plan_id,
            {12}.execution_type,
            DATEDIFF({2}, 0, {12}.last_execution_time)
    ),
    forcing_decisions as
    (
        SELECT plan_forcing_location_id, plan_id
          FROM sys.query_store_plan_forcing_locations
          WHERE replica_group_id = {15}
    )
SELECT b.plan_id as plan_id,
    CONVERT(BIT, ISNULL(plan_forcing_location_id,0)) as is_forced_plan,
    execution_type,
    count_executions,
    SWITCHOFFSET(bucket_start, DATEPART(tz, {10})) AS bucket_start,
    SWITCHOFFSET(bucket_end, DATEPART(tz, {10})) AS bucket_end,
    avg_{0},
    max_{0},
    min_{0},
    stdev_{0},
    variation_{0},
    total_{0}
FROM bucketizer b
LEFT OUTER JOIN forcing_decisions f ON f.plan_id = b.plan_id";

        /// <summary>
        /// Helper method to fill values in PlanChartSummaryTemplate
        /// </summary>
        internal static string GeneratePlanChartSummaryTemplate(long replicaGroupId, params object[] args)
        {
            if (replicaGroupId == ReplicaGroup.Primary.ToLong())
            {
                return string.Format(PlanChartSummaryPrimaryTemplate, args);
            }
            else
            {
                return string.Format(PlanChartSummarySecondaryTemplate, args);
            }
        }

        /// <summary>
        /// Creates query to get data for chart view plan summary when metric is Execution count
        /// </summary>
        internal static string GeneratePlanChartSummaryTemplateForExecutionCount(
            string dateFunctionInterval,
            string parameterQueryId,
            string timingConstraints,
            string planIdColumn,
            string planForcedColumn,
            string execCountColumn,
            string bucketStartColumn,
            string bucketEndColumn,
            string parameterIntervalStartTime,
            string execTypeColumnInfo,
            string replicaFilter) =>
$@"WITH bucketizer as (
    SELECT
        rs.plan_id as plan_id,
        rs.{execTypeColumnInfo} as {execTypeColumnInfo},
        SUM(rs.count_executions) as count_executions,
        DATEADD({dateFunctionInterval}, ((DATEDIFF({dateFunctionInterval}, 0, rs.last_execution_time))),0 ) as bucket_start,
        DATEADD({dateFunctionInterval}, (1 + (DATEDIFF({dateFunctionInterval}, 0, rs.last_execution_time))), 0) as bucket_end
      FROM
        sys.query_store_runtime_stats rs
        JOIN sys.query_store_plan p ON p.plan_id = rs.plan_id
      WHERE
        p.query_id = {parameterQueryId}{timingConstraints}
{replicaFilter}
      GROUP BY
        rs.plan_id,
        rs.{execTypeColumnInfo},
        DATEDIFF({dateFunctionInterval}, 0, rs.last_execution_time)
    ),
    is_forced as
    (
        SELECT is_forced_plan, plan_id
          FROM sys.query_store_plan
    )
SELECT b.plan_id as {planIdColumn},
    is_forced_plan as {planForcedColumn},
    {execTypeColumnInfo} as {execTypeColumnInfo},
    count_executions as {execCountColumn},
    SWITCHOFFSET(bucket_start, DATEPART(tz, {parameterIntervalStartTime})) as {bucketStartColumn},
    SWITCHOFFSET(bucket_end, DATEPART(tz, {parameterIntervalStartTime})) as {bucketEndColumn}
FROM bucketizer b
JOIN is_forced f ON f.plan_id = b.plan_id";

        #endregion

        #region PlanGridSummary

        /// <summary>
        /// Creates query to get data for grid view plan summary.
        /// </summary>
        internal static string GeneratePlanGridSummaryTemplate(
            string metric,
            string parameterQueryId,
            string planIdColumn,
            string planForcedColumn,
            string execCountColumn,
            string minStatsSummary,
            string minMetricColumn,
            string maxStatsSummary,
            string maxMetricColumn,
            string avgStatsSummary,
            string avgMetricColumn,
            string stdevStatsSummary,
            string stdevMetricColumn,
            string variationStatsSummary,
            string variationMetricColumn,
            string lastMetricColumn,
            string totalStatsSummary,
            string totalMetricColumn,
            string firstExecTimeColumn,
            string lastExecTimeColumn,
            string timingConstraints,
            string parameterIntervalStartTime,
            string execTypeColumnInfo,
            string waitstatsSubQuery,
            string statsTableName,
            string statsAlias,
            string executionCountText,
            string parameterReplicaGroupId) =>
$@"WITH {waitstatsSubQuery}
    last_table AS
    (
        SELECT
            p.plan_id plan_id,
            first_value({statsAlias}.last_{metric}) OVER (PARTITION BY p.plan_id ORDER BY {statsAlias}.last_execution_time DESC) last_value
        FROM
            {statsTableName} {statsAlias}
        JOIN
            sys.query_store_plan p ON p.plan_id = {statsAlias}.plan_id
        WHERE
            p.query_id = {parameterQueryId}
    )
SELECT p.{planIdColumn},
    MAX(CONVERT(int, p.is_forced_plan)) {planForcedColumn},
    SUM(distinct {statsAlias}.{execTypeColumnInfo}) {execTypeColumnInfo},
{executionCountText}
    ROUND({minStatsSummary}, 2) {minMetricColumn},
    ROUND({maxStatsSummary}, 2) {maxMetricColumn},
    ROUND({avgStatsSummary}, 2) {avgMetricColumn},
    ROUND({stdevStatsSummary}, 2) {stdevMetricColumn},
    ROUND({variationStatsSummary}, 2) {variationMetricColumn},
    ROUND(max(l.last_value), 2) {lastMetricColumn},
    ROUND({totalStatsSummary}, 2) {totalMetricColumn},
    SWITCHOFFSET(MIN({statsAlias}.{firstExecTimeColumn}), DATEPART(tz, {parameterIntervalStartTime})) {firstExecTimeColumn},
    SWITCHOFFSET(MAX({statsAlias}.{lastExecTimeColumn}), DATEPART(tz, {parameterIntervalStartTime})) {lastExecTimeColumn}
FROM
    {statsTableName} {statsAlias}
JOIN
    sys.query_store_plan p ON p.plan_id = {statsAlias}.plan_id
JOIN
    last_table l ON p.plan_id = l.plan_id
WHERE p.query_id = {parameterQueryId}{timingConstraints}
GROUP BY p.plan_id, {statsAlias}.execution_type";

        /// <summary>
        /// Creates query to get data for grid view plan summary when metric is Execution count
        /// </summary>
        private const string PlanGridSummaryPrimaryTemplateForExecutionCount =
@"SELECT p.{0},
    MAX(CONVERT(int, p.is_forced_plan)) {1},
    SUM(distinct rs.{7}) {7},
    ROUND({2}, 2) {3},
    MIN(rs.{4}) {4},
    MAX(rs.{5}) {5}
FROM
    sys.query_store_runtime_stats rs
JOIN
    sys.query_store_plan p ON p.plan_id = rs.plan_id
WHERE p.query_id = {6}
{8}
GROUP BY p.plan_id, rs.execution_type";

        /// <summary>
        /// Creates query to get data for grid view plan summary when metric is Execution count
        /// </summary>
        private const string PlanGridSummarySecondaryTemplateForExecutionCount =
@"SELECT p.{0},
    MAX(CASE WHEN pf.plan_forcing_location_id IS NOT NULL THEN 1 ELSE 0 END) {1},
    SUM(distinct rs.{7}) {7},
    ROUND({2}, 2) {3},
    MIN(rs.{4}) {4},
    MAX(rs.{5}) {5}
FROM
    sys.query_store_runtime_stats rs
JOIN
    sys.query_store_plan p ON p.plan_id = rs.plan_id
LEFT OUTER JOIN
    sys.query_store_plan_forcing_locations pf on pf.plan_id = rs.plan_id
WHERE p.query_id = {6}
{8}
GROUP BY p.plan_id, rs.execution_type";

        /// <summary>
        /// Helper method to fill values in PlanGridSummaryTemplateForExecutionCount
        /// </summary>
        internal static string GeneratePlanGridSummaryTemplateForExecutionCount(long replicaGroupId, params object[] args)
        {
            if (replicaGroupId == ReplicaGroup.Primary.ToLong())
            {
                return string.Format(PlanGridSummaryPrimaryTemplateForExecutionCount, args);
            }
            else
            {
                return string.Format(PlanGridSummarySecondaryTemplateForExecutionCount, args);
            }
        }

        #endregion

        #endregion

        #region Helper Templates

        /// <summary>
        /// Returns t-sql string for selecting Execution Count
        /// </summary>
        /// <param name="metric">The metric of interest for the query</param>
        /// <param name="statsTableName">Stats table name of interest. e.g. runtimestats or wait stats</param>
        /// <returns></returns>
        internal static string GetExecutionCountText(Metric metric, string statsTableName)
        {
            // Execution count for wait time is calculated differently. Since excutions are recorded per wait category and the resultset is
            // grouped by category. Execution is just the max across the set but not the sum.
            return string.Format(CultureInfo.InvariantCulture, @"    {2}({1}.count_executions) {0},",
                new ExecutionCountColumnInfo().GetQueryColumnLabel(),
                statsTableName,
                metric.Equals(Metric.WaitTime) ? "MAX" : "SUM");
        }

        /// <summary>
        /// Returns t-sql string for selecting Plan Count
        /// </summary>
        /// <returns></returns>
        internal static string GetPlanCountText() => string.Format(CultureInfo.InvariantCulture, @"    COUNT(distinct p.plan_id) {0}", new NumPlansColumnInfo().GetQueryColumnLabel());

        #endregion
    }
}
