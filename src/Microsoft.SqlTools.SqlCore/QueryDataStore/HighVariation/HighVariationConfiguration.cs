//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.SqlTools.SqlCore.QueryDataStore.Common;

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.HighVariation
{
    public class HighVariationConfiguration : QueryConfigurationBase
    {
        public TimeInterval TimeInterval { get; set; }

        public HighVariationConfiguration()
        {
            SelectedMetric = Metric.Duration;
            SelectedStatistic = Statistic.Variation;
            TimeInterval = new TimeInterval(TimeIntervalOptions.LastHour);
        }
    }
}