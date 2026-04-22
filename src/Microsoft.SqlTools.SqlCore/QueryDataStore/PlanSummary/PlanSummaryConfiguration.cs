//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.SqlServer.Management.QueryStoreModel.Common;

namespace Microsoft.SqlServer.Management.QueryStoreModel.PlanSummary
{
    /// <summary>
    /// Class for containing the configuration used to generate the PlanSummary View
    /// </summary>
    public class PlanSummaryConfiguration
    {
        /// <summary>
        /// Enum to control if a specified interval should be used, or all of recorded QDS history
        /// </summary>
        public enum PlanTimeIntervalMode
        {
            SpecifiedRange,
            AllHistory
        }

        public long QueryId { get; set; }
        public long ReplicaGroupId { get; set; }
        public bool IsQDSROAvailable { get; set; }
        public PlanTimeIntervalMode TimeIntervalMode { get; set; }
        public TimeInterval TimeInterval { get; set; }
        public Metric SelectedMetric { get; set; }
        public Statistic SelectedStatistic { get; set; }
        public QueryStoreConstants.ReportTypes ParentReportType { get; set; }

        public PlanSummaryConfiguration()
        {
            ReplicaGroupId = ReplicaGroup.Primary.ToLong();
            IsQDSROAvailable = false;
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="configuration"></param>
        public PlanSummaryConfiguration(PlanSummaryConfiguration configuration)
        {
            QueryId = configuration.QueryId;
            ReplicaGroupId = configuration.ReplicaGroupId;
            TimeIntervalMode = configuration.TimeIntervalMode;
            TimeInterval = configuration.TimeInterval;
            SelectedMetric = configuration.SelectedMetric;
            SelectedStatistic = configuration.SelectedStatistic;
        }

        /// <summary>
        /// helper to determine is a custom time interval should be used
        /// </summary>
        public bool UseTimeInterval
        {
            get
            {
                return TimeIntervalMode == PlanTimeIntervalMode.SpecifiedRange;
            }
            set
            {
                TimeIntervalMode = value ? PlanTimeIntervalMode.SpecifiedRange : PlanTimeIntervalMode.AllHistory;
            }
        }
    }
}
