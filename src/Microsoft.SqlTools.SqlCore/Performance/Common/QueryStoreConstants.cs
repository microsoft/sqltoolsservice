//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.SqlCore.Performance.Common
{
    public static class QueryStoreConstants
    {
        public static long InvalidQueryId = 0;
        public static long InvalidPlanId = 0;
        public static long InvalidReplicaGroupId = -1;
        public static long InvalidWaitCategoryId = 0;
        public static long ReplicaGroupIdUnavailable = 0;  // used when the sever doesn't support QDS Secondary
        public static int MaxRecordsForWaitStatsPerQueryToolTip = 10;
        public static string PlanForcedTrue = "1";
        public static int QueryIdTextBoxWidth = 75;

        /// <summary>
        /// This controls the default filter value for queries with more than x plans
        /// </summary>
        public static int MinNumberOfQueryPlans = 1;

        /// <summary>
        /// This controls the default value for Top x queries returned
        /// </summary>
        public static int TopQueriesReturned = 25;

        /// <summary>
        /// This controls the default value for Top wait categories returned
        /// </summary>
        public static int TopWaitCategoriesReturned = 10;

        /// <summary>
        /// This controls the duration of the custom tool tips on the query store ui
        /// </summary>
        public static int ToolTipDuration = int.MaxValue;

        public enum ReportTypes
        {
            None,
            RegressedQueries,
            OverallResourceConsumption,
            TopResourceConsuming,
            ForcedPlanQueries,
            HighVariation,
            QueryWaitStats
        }
    }
}
