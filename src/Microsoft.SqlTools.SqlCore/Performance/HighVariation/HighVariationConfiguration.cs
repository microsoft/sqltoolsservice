//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.SqlCore.Performance.Common;

namespace Microsoft.SqlTools.SqlCore.Performance.HighVariation
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