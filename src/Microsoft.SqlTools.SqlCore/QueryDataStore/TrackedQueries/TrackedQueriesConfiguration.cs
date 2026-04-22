//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.SqlTools.SqlCore.QueryDataStore.Common;
using Microsoft.SqlTools.SqlCore.QueryDataStore.PlanSummary;

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.TrackedQueries
{
    public class TrackedQueriesConfiguration : PlanSummaryConfiguration
    {
        public const int DefaultAutoRefreshIntervalInSeconds = 5;
        public int AutoRefreshIntervalInSeconds { get; set; }

        public TrackedQueriesConfiguration() => SetToDefaultValues();

        public void SetToDefaultValues()
        {
            SelectedMetric = Metric.Duration;
            SelectedStatistic = Statistic.Avg;
            TimeInterval = new TimeInterval(TimeIntervalOptions.LastDay);
            AutoRefreshIntervalInSeconds = DefaultAutoRefreshIntervalInSeconds;
            QueryId = QueryStoreConstants.InvalidQueryId;
            ReplicaGroupId = ReplicaGroup.Primary.ToLong();
        }
    }
}
