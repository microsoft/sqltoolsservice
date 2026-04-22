//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.SqlCore.QueryDataStore.Controls;
using Microsoft.SqlTools.SqlCore.QueryDataStore.TopResourceConsumers;
using Microsoft.SqlTools.SqlCore.QueryDataStore.WaitStats;
using static System.FormattableString;

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.Common
{
    public static class Utils
    {
        public const int CreateKeywordLength = 6; /*CREATE length*/

        /// <summary>
        /// Forces a plan for the query using the sys.sp_query_store_force_plan stored procedure.
        /// </summary>
        /// <param name="queryId"></param>
        /// <param name="planId"></param>
        /// <param name="sqlConnection"></param>
        public static void ForcePlan(long queryId, long planId, SqlConnection sqlConnection) => ForcePlan(queryId, planId, QueryStoreConstants.ReplicaGroupIdUnavailable, sqlConnection);

        /// <summary>
        /// Forces a plan for the query using the sys.sp_query_store_force_plan stored procedure.
        /// </summary>
        /// <param name="queryId"></param>
        /// <param name="planId"></param>
        /// <param name="replicaGroupId"></param>
        /// <param name="sqlConnection"></param>
        public static void ForcePlan(long queryId, long planId, long replicaGroupId, SqlConnection sqlConnection)
        {
            Debug.Assert(queryId != QueryStoreConstants.InvalidQueryId, "ForcePlan failed with: queryId is invalid.");
            Debug.Assert(planId != QueryStoreConstants.InvalidPlanId, "ForcePlan failed with: planId is invalid.");
            Debug.Assert(sqlConnection != null, "ForcePlan failed with: sqlConnection is null");

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = sqlConnection;
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.CommandText = "sys.sp_query_store_force_plan";
                sqlCommand.Parameters.Add(new SqlParameter(QueryGeneratorUtils.ParameterQueryId, queryId));
                sqlCommand.Parameters.Add(new SqlParameter(QueryGeneratorUtils.ParameterPlanId, planId));
                if (replicaGroupId != QueryStoreConstants.ReplicaGroupIdUnavailable)
                {
                    sqlCommand.Parameters.Add(new SqlParameter(QueryGeneratorUtils.ParameterReplicaGroupId, replicaGroupId));
                }

                sqlCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Unforces a plan for the query using the sys.sp_query_store_unforce_plan stored procedure.
        /// </summary>
        /// <param name="queryId"></param>
        /// <param name="planId"></param>
        /// <param name="sqlConnection"></param>
        public static void UnforcePlan(long queryId, long planId, SqlConnection sqlConnection) => UnforcePlan(queryId, planId, QueryStoreConstants.ReplicaGroupIdUnavailable, sqlConnection);

        /// <summary>
        /// Unforces a plan for the query using the sys.sp_query_store_unforce_plan stored procedure.
        /// </summary>
        /// <param name="queryId"></param>
        /// <param name="planId"></param>
        /// <param name="replicaGroupId"></param>
        /// <param name="sqlConnection"></param>
        public static void UnforcePlan(long queryId, long planId, long replicaGroupId, SqlConnection sqlConnection)
        {
            Debug.Assert(queryId != QueryStoreConstants.InvalidQueryId, "UnforcePlan failed with: queryId is invalid.");
            Debug.Assert(planId != QueryStoreConstants.InvalidPlanId, "UnforcePlan failed with: planId is invalid.");
            Debug.Assert(replicaGroupId != QueryStoreConstants.InvalidReplicaGroupId, "UnforcePlan failed with: replicaGroupId is invalid.");
            Debug.Assert(sqlConnection != null, "UnforcePlan failed with: sqlConnection is null");

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = sqlConnection;
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.CommandText = "sys.sp_query_store_unforce_plan";
                sqlCommand.Parameters.Add(new SqlParameter(QueryGeneratorUtils.ParameterQueryId, queryId));
                sqlCommand.Parameters.Add(new SqlParameter(QueryGeneratorUtils.ParameterPlanId, planId));
                if (replicaGroupId != QueryStoreConstants.ReplicaGroupIdUnavailable)
                {
                    sqlCommand.Parameters.Add(new SqlParameter(QueryGeneratorUtils.ParameterReplicaGroupId, replicaGroupId));
                }

                sqlCommand.ExecuteNonQuery();
            }
        }

        public static string FormatDateTime(DateTimeOffset dateTime) => dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);

        /// <summary>
        /// Check if the database in the current sqlConnection is in READ_ONLY mode for query store.
        /// </summary>
        public static QueryStoreOperationalMode GetQueryStoreOperationalMode(SqlConnection sqlConnection)
        {
            Debug.Assert(sqlConnection != null);

            try
            {
                if (sqlConnection.State == ConnectionState.Closed)
                {
                    sqlConnection.Open();
                }

                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    sqlCommand.Connection = sqlConnection;
                    sqlCommand.CommandText = "SELECT actual_state, readonly_reason FROM sys.database_query_store_options";

                    QueryStoreOperationalMode operationalMode = new QueryStoreOperationalMode();

                    using (SqlDataReader dataReader = sqlCommand.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (dataReader.Read())
                        {
                            int actualState = Convert.ToInt32(dataReader["actual_state"]);

                            if (Enum.IsDefined(typeof(QueryStoreOperationalStatus), actualState))
                            {
                                operationalMode.OperationalStatus = (QueryStoreOperationalStatus)actualState;
                            }

                            operationalMode.ReadOnlyReason = (int)dataReader["readonly_reason"];
                        }
                    }

                    return operationalMode;
                }
            }
            catch (Exception)
            {
                Trace.TraceError("Exception occured while trying to determine if query store is read only");
                throw;
            }
        }

        /// <summary>
        /// Check if the database in the current sqlConnection is in READ_ONLY mode for query store.
        /// </summary>
        public static bool CheckIfQDSROAvailable(SqlConnection sqlConnection)
        {
            Debug.Assert(sqlConnection != null);

            // We don't do this check in the <see cref="ForcePlan"/> and <see cref="UnForcePlan"/> probably because
            // those methods are triggered by the user and they can retry in case of failure.
            if (sqlConnection.State == ConnectionState.Closed)
            {
                sqlConnection.Open();
            }

            using (SqlCommand sqlCommand = new SqlCommand())
            {
                sqlCommand.Connection = sqlConnection;
                sqlCommand.CommandText = @"
SELECT CASE WHEN EXISTS(
SELECT TOP 1 (1)
FROM sys.all_columns c
    JOIN sys.all_objects o
    ON c.object_id = o.object_id
WHERE
    c.name = 'replica_group_id'
    AND o.name = 'query_store_runtime_stats'
    AND o.schema_id = SCHEMA_ID('sys')
    AND o.is_ms_shipped = 1)
THEN 1 ELSE 0 END as ReplicaColumnExists;";

                using (SqlDataReader dataReader = sqlCommand.ExecuteReader())
                {
                    if (dataReader.Read())
                    {
                        return Convert.ToBoolean(dataReader["ReplicaColumnExists"]);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Check if the database in the current sqlConnection is in READ_ONLY mode for query store.
        /// </summary>
        private static string GetQueryStoreReadOnlyReasonString(int readOnlyReason)
        {

            int result = readOnlyReason;

            if ((result & (int)QueryStoreReadOnlyReason.StmtHashMapMemoryLimit) != 0)
            {
                return Resources.QueryStoreReadOnlyReason_StmtHashMapMemoryLimit;
            }

            if ((result & (int)QueryStoreReadOnlyReason.DiskSizeLimit) != 0)
            {
                return Resources.QueryStoreReadOnlyReason_DbReadOnly;
            }

            if ((result & (int)QueryStoreReadOnlyReason.DbInLogAcceptMode) != 0)
            {
                return Resources.QueryStoreReadOnlyReason_DbInLogAcceptMode;
            }

            if ((result & (int)QueryStoreReadOnlyReason.DbInEmergencyMode) != 0)
            {
                return Resources.QueryStoreReadOnlyReason_DbInEmergencyMode;
            }

            if ((result & (int)QueryStoreReadOnlyReason.DbInSingleUserMode) != 0)
            {
                return Resources.QueryStoreReadOnlyReason_DbInSingleUserMode;
            }

            if ((result & (int)QueryStoreReadOnlyReason.DbReadOnly) != 0)
            {
                return Resources.QueryStoreReadOnlyReason_DbReadOnly;
            }

            return string.Empty;
        }

        public static string GetQueryStoreReadOnlyWarning(QueryStoreOperationalMode operationalMode)
        {
            string readOnlyReason = GetQueryStoreReadOnlyReasonString(operationalMode.ReadOnlyReason);

            if (string.IsNullOrWhiteSpace(readOnlyReason))
            {
                return Resources.QueryStoreReadOnlyToolTip;
            }

            return string.Format(
                CultureInfo.CurrentUICulture,
                "{0}{1}{2}",
                Resources.QueryStoreReadOnlyToolTip,
                Environment.NewLine,
                readOnlyReason);
        }


        /// <summary>
        /// Retrieve the SQL query text for the provided query ID.
        /// </summary>
        /// <param name="queryId">Query ID that we want to search the query text for</param>
        /// <param name="sqlConnection">SQL Connection used to retrieve the query text</param>
        /// <param name="selectedTextCoordinates">This wraps the exact location(start column/line and end column/line) of query text under containing object(if any) else it would be null</param>
        /// <returns></returns>
        public static string GetQueryText(long queryId, SqlConnection sqlConnection, out SelectedTextCoordinates selectedTextCoordinates)
        {
            System.Diagnostics.Debug.Assert(sqlConnection != null);
            selectedTextCoordinates = null;

            try
            {
                if (sqlConnection.State == ConnectionState.Closed)
                {
                    sqlConnection.Open();
                }

                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    const string queryIdParameter = "@Query_ID";
                    const string objectIdParameter = "@Object_ID";
                    long objectId = 0;
                    string queryText = string.Empty;
                    string parentObject = string.Empty;

                    // First retrieve the object id and query text of the query
                    sqlCommand.Connection = sqlConnection;
                    sqlCommand.CommandText = string.Format(
                        CultureInfo.InvariantCulture,
                        @"SELECT q.object_id, qt.query_sql_text
                        FROM sys.query_store_query q, sys.query_store_query_text qt
                        WHERE q.query_id = {0} AND q.query_text_id = qt.query_text_id ",
                        queryIdParameter);
                    sqlCommand.Parameters.Add(new SqlParameter(queryIdParameter, queryId));

                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            objectId = reader.GetInt64(0);
                            queryText = reader.GetString(1);
                        }
                    }

                    //If objectid is not null, we will read the containing object. Else we will just display the query text
                    if (objectId == 0)
                    {
                        return queryText;
                    }

                    sqlCommand.CommandText = string.Format(
                        CultureInfo.InvariantCulture,
                        @"select definition from sys.sql_modules where object_id = {0}",
                        objectIdParameter);
                    sqlCommand.Parameters.Add(new SqlParameter(objectIdParameter, objectId));

                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        //If containing object doesn't exist anymore, just return the query text
                        if (!reader.Read())
                        {
                            return string.Join(Environment.NewLine, Resources.InvalidContainingObject, queryText);
                        }

                        // validate parent object still contains the query text.
                        //There could be a case when parent object is modified and no longer have the querytext.
                        parentObject = reader.GetString(0);

                        if (!parentObject.Contains(queryText))
                        {
                            return string.Join(Environment.NewLine, Resources.InvalidQueryUnderContainingObject, queryText);
                        }
                    }

                    //At this point We should have a valid parent object that contains the required query text
                    //Calculate the caret position
                    selectedTextCoordinates = CalculateCaretPosition(parentObject, queryText);

                    //Create Alter script for the parent object
                    return AlterFromCreateScript(parentObject);
                }
            }
            catch (Exception)
            {
                System.Diagnostics.Trace.TraceError("Exception occured while trying to retrieve query text");

                throw;
            }
        }

        /// <summary>
        /// This helper function expects a create script for procedure/trigger and generates an alter script for the same
        /// </summary>
        /// <param name="objectString">create script for the containing object</param>
        /// <returns></returns>
        internal static string AlterFromCreateScript(string objectString)
        {
            var location = objectString.IndexOf("create", StringComparison.InvariantCultureIgnoreCase);
            if (location < 0)
            {
                return objectString;
            }

            objectString = objectString.Remove(location, CreateKeywordLength);
            return objectString.Insert(location, "ALTER");
        }

        /// <summary>
        /// This method returns the SelectedTextCoordinates(starting and ending row/column for the given querytext under given parentObject text.
        /// This method expects querytext will always be present under parentObject. It's upto the caller to check pre-requisites.
        /// algorithm runs worst in order of n.
        /// </summary>
        /// <param name="parentObject">parent object contents</param>
        /// <param name="queryText">query text being searched</param>
        /// <returns></returns>
        public static SelectedTextCoordinates CalculateCaretPosition(string parentObject, string queryText)
        {
            var matched = 0;                        // no. of characters matched
            var startingPos = 0;                    // starting position of the query text under parent object
            var lineFeedsInsideQueryText = 0;       // no. linefeeds within query text. (query text can span over multiple lines)
            var lineFeeds = new HashSet<int>();     // hashset storing index position of line feeds encountered so far
            var indexPointerInParentObject = 0;     // this is to point characters in parent object

            while (indexPointerInParentObject < parentObject.Length)
            {
                if (matched == queryText.Length)
                {
                    // We have found the queryText under parentObject. indexPointerInParentObject should have the index to last character matched
                    break;
                }

                // This if-else block is storing linefeeds by searching NewLine characters from parent object
                if (Environment.NewLine.Length == 1)
                {
                    if (parentObject[indexPointerInParentObject] == '\n' && !lineFeeds.Contains(indexPointerInParentObject))
                    {
                        lineFeeds.Add(indexPointerInParentObject);
                        lineFeedsInsideQueryText++;
                    }
                }
                else
                {
                    if (parentObject[indexPointerInParentObject] == '\r' && indexPointerInParentObject < parentObject.Length - 1 && parentObject[indexPointerInParentObject + 1] == '\n' && !lineFeeds.Contains(indexPointerInParentObject + 1))
                    {
                        lineFeeds.Add(indexPointerInParentObject + 1);
                        lineFeedsInsideQueryText++;
                    }
                }

                // Increment matched counter
                if (parentObject[indexPointerInParentObject] == queryText[matched])
                {
                    matched++;
                    indexPointerInParentObject++;
                    continue;
                }

                // Reset counters when we don't have continuous match
                indexPointerInParentObject = startingPos + 1;
                matched = 0;
                lineFeedsInsideQueryText = 0;
                startingPos = indexPointerInParentObject;
            }

            // starting line should be total no. of linefeeds substract linefeeds within querytext
            var startLine = lineFeeds.Count - lineFeedsInsideQueryText;

            // starting column should be starting position of query text substract
            // index of last linefeed encountered before starting position substract 1 (to make it zero based)
            var startColumn = startingPos - lineFeeds.Reverse().Skip(lineFeedsInsideQueryText).First() - 1;

            // ending line should be no. of linefeeds encounter so far
            var endLine = lineFeeds.Count;

            // end column is the indexPointerInParentObject substract the index of last linefeed substract 1 (to make it zero based)
            var endColumn = indexPointerInParentObject - lineFeeds.Last() - 1;

            return new SelectedTextCoordinates
            {
                StartColumn = startColumn,
                StartLine = startLine,
                EndColumn = endColumn,
                EndLine = endLine
            };
        }

        /// <summary>
        /// Gets the showplan XML for the specified plan
        /// </summary>
        /// <param name="planId">The plan we want to retrieve the xml for.</param>
        /// <param name="sqlConnection">Sql Connection to execute the query in</param>
        /// <returns></returns>
        public static string GetShowPlanXML(long planId, SqlConnection sqlConnection)
        {
            string showPlanXml = string.Empty;

            try
            {
                if (sqlConnection.State == ConnectionState.Closed)
                {
                    sqlConnection.Open();
                }

                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = @"select query_plan from sys.query_store_plan where plan_id = @plan_id";
                    sqlCommand.Parameters.AddWithValue(QueryGeneratorUtils.ParameterPlanId, planId);

                    // finally ready to gather the data
                    using (SqlDataReader dataReader = sqlCommand.ExecuteReader())
                    {
                        if (dataReader.Read())
                        {
                            showPlanXml = dataReader.GetString(0);
                        }
                    }
                }
            }
            catch
            {
                System.Diagnostics.Trace.TraceError("Exception occured while retrieving the showplan xml");

                throw;
            }

            return showPlanXml;
        }

        /// <summary>
        /// Check if the string input is an integer
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsInputInteger(string input)
        {
            int num;
            return int.TryParse(input.Trim(), NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.CurrentUICulture, out num);
        }

        /// <summary>
        /// Parse string as integer.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int ParseInputInteger(string input) => int.Parse(input, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.CurrentUICulture);

        /// <summary>
        /// Check if the string input is a long
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsInputLong(string input)
        {
            long num;
            return long.TryParse(input.Trim(), NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.CurrentUICulture, out num);
        }

        /// <summary>
        /// Parse string as long.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static long ParseInputLong(string input) => long.Parse(input, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.CurrentUICulture);

        /// <summary>
        /// Utility method to fetch tooltip data per query for TopResourceConsuming query report
        /// </summary>
        /// <param name="sqlConnection"></param>
        /// <param name="configuration">Configuration instance for start and end date times</param>
        /// <param name="queryId">Query Id to fetch tooltip for</param>
        /// <returns></returns>
        public static IList<ResultSetMapping> GetExtendedToolTipForTopResourceConsumingQuery(SqlConnection sqlConnection, TopResourceConsumersConfiguration configuration, string queryId)
        {
            // Currently we only set tooltip data for Total WaitTime
            if (!configuration.SelectedMetric.Equals(Metric.WaitTime) || !configuration.SelectedStatistic.Equals(Statistic.Total))
            {
                return null;
            }

            var waitstats = new List<ResultSetMapping>();
            var waitCategory = new WaitCategoryDescColumnInfo();

            try
            {
                sqlConnection.Open();

                using (var sqlCommand = new SqlCommand(QueryWaitStatsQueryGenerator.TotalWaitTimePerWaitCategoryForQueryId(configuration), sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue(QueryGeneratorUtils.ParameterQueryId, queryId);
                    sqlCommand.Parameters.AddWithValue(QueryGeneratorUtils.ParameterIntervalStartTime, configuration.TimeInterval.StartDateTimeOffset);
                    sqlCommand.Parameters.AddWithValue(QueryGeneratorUtils.ParameterIntervalEndTime, configuration.TimeInterval.EndDateTimeOffset);
                    sqlCommand.Parameters.AddWithValue(QueryGeneratorUtils.ParameterReplicaGroupId, configuration.ReplicaGroupId);

                    using (var dataReader = sqlCommand.ExecuteReader())
                    {
                        if (dataReader.HasRows)
                        {
                            waitstats.Add(new ResultSetMapping
                            {
                                Category = waitCategory.GetLocalizedColumnHeader(),
                                Value = Resources.MetricOptionQueryWaitTime
                            });
                        }

                        while (dataReader.Read())
                        {
                            var category = dataReader.GetString(0);
                            var value = dataReader.GetInt64(1);

                            waitstats.Add(new ResultSetMapping
                            {
                                Category = string.Format(Resources.TotalWaitCategoryTime, category),
                                Value = value.ToString()
                            });
                        }
                    }
                }
            }
            catch (SqlException)
            {
                return null;
            }

            return waitstats;
        }

        /// <summary>
        /// Utility method to fetch tooltip data for wait category in QueryWaitStatistics report
        /// </summary>
        /// <param name="sqlConnection"></param>
        /// <param name="configuration">Configuration instance for start and end date times</param>
        /// <param name="waitcategoryId">Wait category Id to fetch tooltip for</param>
        /// <returns></returns>
        public static IList<ResultSetMapping> GetExtendedTooltipForAggWaitTimePerQueryForCategory(SqlConnection sqlConnection,
            QueryWaitStatsConfiguration configuration, string waitcategoryId)
        {
            // We only have extended tooltip data for PerCategory barchart view
            //
            if (!configuration.IsExtendedDataForToolTipAvailable)
            {
                return null;
            }
            var statisticMetricCol = new StatisticMetricColumnInfo(configuration.SelectedStatistic, configuration.SelectedMetric);
            var queryIdCol = new QueryIdColumnInfo();
            var waitstats = new List<ResultSetMapping>();

            try
            {
                sqlConnection.Open();

                using (var sqlCommand = new SqlCommand(QueryWaitStatsQueryGenerator.AggWaitTimePerQueryForWaitCategoryId(configuration), sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue(QueryGeneratorUtils.ParameterWaitCategoryId, waitcategoryId);
                    sqlCommand.Parameters.AddWithValue(QueryGeneratorUtils.ParameterIntervalStartTime, configuration.TimeInterval.StartDateTimeOffset);
                    sqlCommand.Parameters.AddWithValue(QueryGeneratorUtils.ParameterIntervalEndTime, configuration.TimeInterval.EndDateTimeOffset);
                    sqlCommand.Parameters.AddWithValue(QueryGeneratorUtils.ParameterResultsRowCount, QueryStoreConstants.MaxRecordsForWaitStatsPerQueryToolTip);
                    sqlCommand.Parameters.AddWithValue(QueryGeneratorUtils.ParameterReplicaGroupId, configuration.ReplicaGroupId);

                    using (var dataReader = sqlCommand.ExecuteReader())
                    {
                        if (dataReader.HasRows)
                        {
                            waitstats.Add(new ResultSetMapping
                            {
                                Category = Resources.QueryIDSearchColumnHeaderQueryID,
                                Value = statisticMetricCol.GetLocalizedColumnHeaderWithUnits()
                            });
                        }

                        var queryIdColId = dataReader.GetOrdinal(queryIdCol.GetQueryColumnLabel());
                        var statColId = dataReader.GetOrdinal(statisticMetricCol.GetQueryColumnLabel());

                        while (dataReader.Read())
                        {
                            var category = dataReader.GetInt64(queryIdColId);
                            var value = dataReader.GetDouble(statColId);

                            waitstats.Add(new ResultSetMapping
                            {
                                Category = category.ToString(),
                                Value = value.ToString()
                            });
                        }
                    }
                }
            }
            catch (SqlException)
            {

            }

            return waitstats;
        }

        /// <summary>
        /// Appends an "ORDER BY" clause to the passed in query.
        /// </summary>
        /// <param name="query">The query fragment to conditionally append an "ORDER BY" clause</param>
        /// <param name="orderByColumn">The info for the column to order on; null means no "ORDER BY" is added to the clause.</param>
        /// <param name="subqueryAlias">The CTE alias, when there is a need to disambiguate the 'orderByColumn'. Ignored when 'orderByColumn' is null.</param>
        /// <param name="isDescending">False when ascending order is desired; true otherwise. Default is true.</param>
        /// <returns>The passed in query with, possibly, the "ORDER BY" clause appended to it.</returns>
        internal static string AppendOrderBy(string query, ColumnInfo orderByColumn, string subqueryAlias = null, bool isDescending = true)
        {
            var orderByStr = (subqueryAlias != null ? $"{subqueryAlias}." : "") + orderByColumn?.GetQueryColumnLabel();
            var descOrAscStr = isDescending ? "DESC" : "ASC";

            return
                orderByColumn != null
                ? query + Environment.NewLine + Invariant($"ORDER BY {orderByStr} {descOrAscStr}")
                : query;
        }
    }
}
