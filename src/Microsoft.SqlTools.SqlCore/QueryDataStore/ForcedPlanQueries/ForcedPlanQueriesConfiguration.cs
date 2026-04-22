//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.SqlServer.Management.QueryStoreModel.Common;

namespace Microsoft.SqlServer.Management.QueryStoreModel.ForcedPlanQueries
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