//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.SqlServer.Management.QueryStoreModel.Common;

namespace Microsoft.SqlServer.Management.QueryStoreModel.HighVariation
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