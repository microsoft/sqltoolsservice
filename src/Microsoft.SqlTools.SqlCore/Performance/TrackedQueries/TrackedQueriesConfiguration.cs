//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.SqlCore.Performance.Common;
using Microsoft.SqlTools.SqlCore.Performance.PlanSummary;

namespace Microsoft.SqlTools.SqlCore.Performance.TrackedQueries
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
