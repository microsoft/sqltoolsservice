//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.SqlCore.QueryDataStore.Common;

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.WaitStats
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