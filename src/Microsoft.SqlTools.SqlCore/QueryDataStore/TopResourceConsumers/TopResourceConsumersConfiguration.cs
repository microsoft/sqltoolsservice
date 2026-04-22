//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using Microsoft.SqlServer.Management.QueryStoreModel.Common;

namespace Microsoft.SqlServer.Management.QueryStoreModel.TopResourceConsumers
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