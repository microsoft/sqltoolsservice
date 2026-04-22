//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.SqlCore.QueryDataStore.Common;

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.TopResourceConsumers
{
    public class TopResourceConsumersConfiguration : QueryConfigurationBase, ICloneable
    {
        public TimeInterval TimeInterval { get; set; }

        public TopResourceConsumersConfiguration()
        {
            TimeInterval = new TimeInterval(TimeIntervalOptions.LastHour);
            SelectedStatistic = Statistic.Total;
        }

        public TopResourceConsumersConfiguration(TopResourceConsumersConfiguration copy)
        {
            TimeInterval = copy.TimeInterval;
            OperationalMode = copy.OperationalMode;
            SelectedMetric = copy.SelectedMetric;
            SelectedStatistic = copy.SelectedStatistic;
            TopQueriesReturned = copy.TopQueriesReturned;
            ReturnAllQueries = copy.ReturnAllQueries;
            MinNumberOfQueryPlans = copy.MinNumberOfQueryPlans;
            ReplicaGroupId = copy.ReplicaGroupId;
            IsQDSROAvailable = copy.IsQDSROAvailable;
        }

        public object Clone() => new TopResourceConsumersConfiguration(this);
    }
}