//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlServer.Management.QueryStoreModel.PlanSummary;

namespace Microsoft.SqlServer.Management.QueryStoreModel.TrackedQueries
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
