//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.SqlCore.QueryDataStore.Common;

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.ForcedPlanQueries
{
    /// <summary>
    /// Class to stores the configuration for Forced Plan Queries report
    /// </summary>
    public class ForcedPlanQueriesConfiguration : QueryConfigurationBase
    {
        public TimeInterval TimeInterval { get; set; }

        public ForcedPlanQueriesConfiguration()
        {
            TimeInterval = new TimeInterval(TimeIntervalOptions.LastMonth);
            SelectedMetric = Metric.Duration;
            SelectedStatistic = Statistic.Avg;
            ReturnAllQueries = true;
            MinNumberOfQueryPlans = QueryStoreConstants.MinNumberOfQueryPlans;
        }
    }
}