//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.SqlTools.SqlCore.Performance.Common;
using static System.FormattableString;

namespace Microsoft.SqlTools.SqlCore.Performance.ForcedPlanQueries
{
    /// <summary>
    /// Util Class used to generate the queries required for Forced Plan Queries report
    /// </summary>
    public class ForcedPlanQueriesQueryGenerator
    {
        #region Forced Plan Queries Summary

        /// <summary>
        /// The final select block of the Forced Plan Queries
        /// specifies the list of information we want to retrieve to display in the forced plan queries grid table.
        ///
        /// Blank spaces in front of the query texts are used to match formatting of the overall query
        /// </summary>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the ForcedPlanQueriesPane to help keep track of the order of column</param>
        /// <returns>The select block text and an ordered list of ColumnInfo</returns>
        private static string GetFinalSelects(out IList<ColumnInfo> columnInfoList)
        {
            columnInfoList = new List<ColumnInfo>();

            var builder = new StringBuilder();

            // Construct the following columns (in this specific order):
            // 1. Query ID
            // 2. Query Text
            // 3. Forced Plan Id
            // 4. Forced Plan Failure count
            // 5. Last compile start time
            // 6. Forced Plan Failure Description
            // 7. Number of Plans
            // 8. Last Query Execution Time
            // 9. Forced Plan Last Execution Time
            // 10. Object ID
            // 11. Object Name

            QueryIdColumnInfo queryIdColumn = new QueryIdColumnInfo();
            QueryTextColumnInfo queryTextColumn = new QueryTextColumnInfo();
            ForcedPlanIdColumnInfo forcedPlanId = new ForcedPlanIdColumnInfo();
            ForcedPlanFailureCountColumnInfo forcedPlanFailureCount = new ForcedPlanFailureCountColumnInfo();
            LastCompileStartTimeColumnInfo lastCompileStartTime = new LastCompileStartTimeColumnInfo();
            ForcedPlanFailureDescpColumnInfo forcedPlanFailureDescp = new ForcedPlanFailureDescpColumnInfo();
            NumPlansColumnInfo numPlansColumn = new NumPlansColumnInfo();
            LastQueryExecTimeColumnInfo lastQueryExecTime = new LastQueryExecTimeColumnInfo();
            LastForcedPlanExecTimeColumnInfo lastForcedPlanExecTime = new LastForcedPlanExecTimeColumnInfo();
            ObjectIdColumnInfo objectIdColumn = new ObjectIdColumnInfo();
            ObjectNameColumnInfo objectNameColumn = new ObjectNameColumnInfo();

            columnInfoList.Add(queryIdColumn);
            columnInfoList.Add(queryTextColumn);
            columnInfoList.Add(forcedPlanId);
            columnInfoList.Add(forcedPlanFailureCount);
            columnInfoList.Add(lastCompileStartTime);
            columnInfoList.Add(forcedPlanFailureDescp);
            columnInfoList.Add(numPlansColumn);
            columnInfoList.Add(lastQueryExecTime);
            columnInfoList.Add(lastForcedPlanExecTime);
            columnInfoList.Add(objectIdColumn);
            columnInfoList.Add(objectNameColumn);

            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    A.{0},", queryIdColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    A.{0},", queryTextColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    A.{0},", forcedPlanId.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    A.{0},", forcedPlanFailureCount.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    A.{0},", lastCompileStartTime.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    A.{0},", forcedPlanFailureDescp.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    B.{0},", numPlansColumn.GetQueryColumnLabel()));
            // DEVNOTE(MatteoT): lastQueryExecTime and lastForcedPlanExecTime both resolve to a column name 'last_execution_time',
            // which requires special care (you'd need to qualify the name with an alias in a ORDER BY, for example).
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    B.{0},", lastQueryExecTime.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    A.{0},", lastForcedPlanExecTime.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    A.{0},", objectIdColumn.GetQueryColumnLabel()));
            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, @"    A.{0}", objectNameColumn.GetQueryColumnLabel()));
            return builder.ToString().TrimEnd();
        }

        private static string GetPlanSpecificColumns(long replicaGroupId)
        {
            QueryIdColumnInfo queryIdColumn = new QueryIdColumnInfo();
            QueryTextColumnInfo queryTextColumn = new QueryTextColumnInfo();
            ForcedPlanIdColumnInfo planIdColumn = new ForcedPlanIdColumnInfo();
            ForcedPlanFailureCountColumnInfo forceFailureCount = new ForcedPlanFailureCountColumnInfo();
            ForcedPlanFailureDescpColumnInfo forcedPlanFailureReason = new ForcedPlanFailureDescpColumnInfo();
            LastForcedPlanExecTimeColumnInfo lastExecTimeColumn = new LastForcedPlanExecTimeColumnInfo();
            ObjectIdColumnInfo objectIdColumn = new ObjectIdColumnInfo();
            ObjectNameColumnInfo objectNameColumn = new ObjectNameColumnInfo();
            LastCompileStartTimeColumnInfo lastCompileStartTime = new LastCompileStartTimeColumnInfo();

            var queryTemplate =
$@"SELECT
    p.{queryIdColumn.GetQueryColumnLabel()} {queryIdColumn.GetQueryColumnLabel()},
    qt.{queryTextColumn.GetQueryColumnLabel()} {queryTextColumn.GetQueryColumnLabel()},
    p.{planIdColumn.GetQueryColumnLabel()} {planIdColumn.GetQueryColumnLabel()},
    p.{forceFailureCount.GetQueryColumnLabel()} {forceFailureCount.GetQueryColumnLabel()},
    p.{forcedPlanFailureReason.GetQueryColumnLabel()} {forcedPlanFailureReason.GetQueryColumnLabel()},
    p.{lastExecTimeColumn.GetQueryColumnLabel()} {lastExecTimeColumn.GetQueryColumnLabel()},
    q.{objectIdColumn.GetQueryColumnLabel()} {objectIdColumn.GetQueryColumnLabel()},
    ISNULL(OBJECT_NAME(q.{objectIdColumn.GetQueryColumnLabel()}),'') {objectNameColumn.GetQueryColumnLabel()},
    p.{lastCompileStartTime.GetQueryColumnLabel()} {lastCompileStartTime.GetQueryColumnLabel()}
FROM sys.query_store_plan p
    JOIN sys.query_store_query q ON q.query_id = p.query_id
    JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id";

            if (replicaGroupId == ReplicaGroup.Primary.ToLong())
            {
                queryTemplate += System.Environment.NewLine + "where p.is_forced_plan = 1";
            }

            return queryTemplate;
        }

        private static string GetAggregatedColumns(long replicaGroupId)
        {
            QueryIdColumnInfo queryIdColumn = new QueryIdColumnInfo();
            LastQueryExecTimeColumnInfo queryLastExecTime = new LastQueryExecTimeColumnInfo();
            NumPlansColumnInfo numPlansColumn = new NumPlansColumnInfo();

            var queryTemplate =
$@"SELECT
    p.{queryIdColumn.GetQueryColumnLabel()} {queryIdColumn.GetQueryColumnLabel()},
    MAX(p.{queryLastExecTime.GetQueryColumnLabel()}) {queryLastExecTime.GetQueryColumnLabel()},
    COUNT(distinct p.plan_id) {numPlansColumn.GetQueryColumnLabel()}
FROM sys.query_store_plan p
GROUP BY p.query_id";

            // for replicas other than primary, we don't set the is_forced_plan.
            //
            if (replicaGroupId == ReplicaGroup.Primary.ToLong())
            {
                queryTemplate += System.Environment.NewLine + "HAVING MAX(CAST(p.is_forced_plan AS tinyint)) = 1";
            }

            return queryTemplate;
        }

        /// <summary>
        /// Query used to populate the top resource consumer table
        /// </summary>
        /// <param name="configuration">Forced Plan Queries Configuration.</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the ForcedPlanQueriesPane to help keep track of the order of column</param>
        /// <returns></returns>
        public static string ForcedPlanQueriesSummary(
            ForcedPlanQueriesConfiguration configuration,
            out IList<ColumnInfo> columnInfoList)
        => ForcedPlanQueriesSummary(configuration, orderByColumn: null, descending: true, out columnInfoList);

        /// <summary>
        /// Query used to populate the top resource consumer table
        /// </summary>
        /// <param name="configuration">Forced Plan Queries Configuration.</param>
        /// <param name="orderByColumn">The current sorting column</param>
        /// <param name="descending">Whether we want to sort by ascending or descending</param>
        /// <param name="columnInfoList">List of ColumnInfo passed back to the ForcedPlanQueriesPane to help keep track of the order of column</param>
        /// <returns></returns>
        public static string ForcedPlanQueriesSummary(
        ForcedPlanQueriesConfiguration configuration,
        ColumnInfo orderByColumn,
        bool descending,
        out IList<ColumnInfo> columnInfoList)
        {
            var extraJoin =
                configuration.ReplicaGroupId != ReplicaGroup.Primary.ToLong()
                ? " JOIN sys.query_store_plan_forcing_locations qfl ON A.plan_id = qfl.plan_id"
                : string.Empty;
            var replicaGroupIdPredicate =
                configuration.ReplicaGroupId != ReplicaGroup.Primary.ToLong()
                ? $" AND qfl.replica_group_id = {QueryGeneratorUtils.ParameterReplicaGroupId}"
                : string.Empty;
            var queryText = Invariant(
$@"WITH
A AS
(
{GetPlanSpecificColumns(configuration.ReplicaGroupId)}
),
B AS
(
{GetAggregatedColumns(configuration.ReplicaGroupId)}
)
SELECT {QueryGeneratorUtils.RowsToReturnString(configuration.ReturnAllQueries)}
{GetFinalSelects(out columnInfoList)}
FROM A JOIN B ON A.query_id = B.query_id{extraJoin}
WHERE B.num_plans >= {configuration.MinNumberOfQueryPlans}{replicaGroupIdPredicate}");

            // DEVNOTE(MatteoT): the sorting on these 2 columns is odd because they originate from the same column name
            // used in the 2 different sets of the CTE used... so when we are sorting, we need to fully qualify the name
            // so to speak, by using the longer notation "A.last_execution_time" or "B.last_execution_time".
            //
            // It it not clear to me if this is a one-off scenario that is not worth addressing by updating the ColumnInfo
            // abstract class or maybe the intermediate queries should be written to avoid this clash... so for not, I just
            // go with the simplest approach.
            var subqueryAlias =
                orderByColumn is LastQueryExecTimeColumnInfo
                ? "B"
                : orderByColumn is LastForcedPlanExecTimeColumnInfo
                    ? "A"
                    : null;

            return Utils.AppendOrderBy(queryText, orderByColumn, subqueryAlias: subqueryAlias, isDescending: descending);
        }
        #endregion
    }
}
