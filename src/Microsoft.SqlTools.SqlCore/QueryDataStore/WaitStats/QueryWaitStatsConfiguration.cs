//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.SqlServer.Management.QueryStoreModel.Common;

namespace Microsoft.SqlServer.Management.QueryStoreModel.WaitStats
{
    /// <summary>
    /// Stores Configuration for QueryWaitStats report
    /// </summary>
    public class QueryWaitStatsConfiguration : QueryConfigurationBase
    {
        public TimeInterval TimeInterval { get; set; }

        public bool IsExtendedDataForToolTipAvailable { get; set; }

        public bool ReturnAllWaitCategories { get; set; }

        public int TopWaitCategoriesReturned { get; set; }

        public QueryWaitStatsConfiguration()
        {
            ReturnAllWaitCategories = false;
            IsExtendedDataForToolTipAvailable = false;
            TopWaitCategoriesReturned = QueryStoreConstants.TopWaitCategoriesReturned;
            SelectedMetric = Metric.WaitTime;
            SelectedStatistic = Statistic.Total;
            TimeInterval = new TimeInterval(TimeIntervalOptions.LastHour);
        }
    }
}