//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;

namespace Microsoft.SqlServer.Management.QueryStoreModel.Common
{
    // Values in this enum come from EQDSOperationalState in sql engine.
    public enum QueryStoreOperationalStatus
    {
        Off = 0,
        ReadOnly = 1,
        ReadWrite = 2,
        // Error = 3, We don't use this value since QDS view is disabled in SSMS when QDS is in error state.
        ReadCapture = 4,
    };

    [Flags]
    internal enum QueryStoreReadOnlyReason
    {
        None = 0x00000000,	                    // QDS can be read-write
        DbReadOnly = 0x00000001,	            // Database is in read-only mode
        DbInSingleUserMode = 0x00000002,	    // Database is in single-user mode
        DbInEmergencyMode = 0x00000004,	        // Database is in emergency mode
        DbInLogAcceptMode = 0x00000008,	        // Database is in log accept mode
        DiskSizeLimit = 0x00010000,	            // QDS has reached disk size limit
        StmtHashMapMemoryLimit = 0x00020000,    // QDS statement hash map has reached memory limit
    }

    public class QueryStoreOperationalMode
    {
        public QueryStoreOperationalStatus OperationalStatus { get; set; }

        public int ReadOnlyReason { get; set; }

        public QueryStoreOperationalMode()
        {
            OperationalStatus = QueryStoreOperationalStatus.Off;
            ReadOnlyReason = (int)QueryStoreReadOnlyReason.None;
        }

        public bool IsReadOnly => OperationalStatus == QueryStoreOperationalStatus.ReadOnly;

        public bool IsReadWrite => OperationalStatus == QueryStoreOperationalStatus.ReadWrite;

        public bool IsReadCapture => OperationalStatus == QueryStoreOperationalStatus.ReadCapture;

        public bool IsReadOnlyOrReadCapture =>
          (OperationalStatus == QueryStoreOperationalStatus.ReadOnly) ||
          (OperationalStatus == QueryStoreOperationalStatus.ReadCapture);
    }

    /// <summary>
    /// Base class for report Configurations. 
    /// Encapsulates common properties which are shared across all the reports.
    /// </summary>
    public class QueryConfigurationBase
    {
        #region Delegates

        public delegate void QueryStoreOperationalModeChangedHandler(QueryStoreOperationalMode operationalMode);

        public event QueryStoreOperationalModeChangedHandler OperationalModeChangedEvent;

        #endregion

        #region Attributes

        internal QueryStoreOperationalMode operationalMode;

        #endregion

        #region Constructor

        internal QueryConfigurationBase()
        {
            SelectedMetric = Metric.Duration; // getting that initial bind for free!
            SelectedStatistic = Statistic.Avg;
            TopQueriesReturned = QueryStoreConstants.TopQueriesReturned;
            ReturnAllQueries = false;
            OperationalMode = new QueryStoreOperationalMode();
            MinNumberOfQueryPlans = QueryStoreConstants.MinNumberOfQueryPlans;
            ReplicaGroupId = ReplicaGroup.Primary.ToLong();
            IsQDSROAvailable = false;
        }

        internal QueryConfigurationBase(QueryConfigurationBase configurationBase)
        {
            SelectedMetric = configurationBase.SelectedMetric;
            SelectedStatistic = configurationBase.SelectedStatistic;
            TopQueriesReturned = configurationBase.TopQueriesReturned;
            ReturnAllQueries = configurationBase.ReturnAllQueries;
            OperationalMode = configurationBase.OperationalMode;
            MinNumberOfQueryPlans = configurationBase.MinNumberOfQueryPlans;
            ReplicaGroupId = configurationBase.ReplicaGroupId;
            IsQDSROAvailable = configurationBase.IsQDSROAvailable;
        }

        #endregion

        #region Properties

        public Metric SelectedMetric { get; set; }

        public Statistic SelectedStatistic { get; set; }

        public int TopQueriesReturned { get; set; }

        public bool ReturnAllQueries { get; set; }

        public int MinNumberOfQueryPlans { get; set; }

        public long ReplicaGroupId { get; set; }

        public bool IsQDSROAvailable { get; set; }

        public QueryStoreOperationalMode OperationalMode
        {
            get { return this.operationalMode; }
            set
            {
                this.operationalMode = value;
                if (this.OperationalModeChangedEvent != null)
                {
                    this.OperationalModeChangedEvent(value);
                }
            }
        }
        #endregion
    }
}
