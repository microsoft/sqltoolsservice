//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.SqlTools.SqlCore.QueryDataStore.Common;

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.OverallResourceConsumption
{
    /// <summary>
    /// Simple class which holds the Configuration settings for the OverallResourceConsumption Control
    /// </summary>
    public class OverallResourceConsumptionConfiguration : QueryConfigurationBase
    {

        public OverallResourceConsumptionConfiguration()
        {
            SelectedMetrics = new List<Metric>
            {
                // hard-coded default values
                Metric.Duration,
                Metric.ExecutionCount,
                Metric.CPUTime,
                Metric.LogicalReads
            };

            SpecifiedTimeInterval = new TimeInterval(TimeIntervalOptions.LastMonth);
            SelectedBucketInterval = BucketInterval.Automatic;
        }

        public TimeInterval SpecifiedTimeInterval { get; set; }

        public List<Metric> SelectedMetrics { get; private set; }

        public BucketInterval SelectedBucketInterval { get; set; }
    }
}
