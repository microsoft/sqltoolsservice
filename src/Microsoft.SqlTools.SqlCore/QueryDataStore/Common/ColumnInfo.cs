//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.Common
{
    /// <summary>
    /// Base class for all column info classes.
    /// ColumnInfo classes contain metadata about the query columns such as metric, statistic and comparison time interval.
    /// These metadata helps generate localized column headers and query column labels.
    /// </summary>
    public abstract class ColumnInfo
    {
        public abstract string GetLocalizedColumnHeader();
        public abstract string GetQueryColumnLabel();

        public virtual string GetLocalizedColumnHeaderWithUnits() => GetLocalizedColumnHeader();

        public string GetLocalizedColumnHeaderInLower() => GetLocalizedColumnHeader().ToLower(CultureInfo.CurrentUICulture);

        /// <summary>
        /// If any derived ColumnInfo have any column specific data to load. They
        /// shall override this property.
        /// E.g. Total_WaitTime/Avg_WaitTime columns have specific data for type of wait category for the total wait time
        /// </summary>
        /// <returns></returns>
        public virtual bool IsBindRuntimeData() => false;

        /// <summary>
        /// We want this to be a deep copy.
        /// If all of the fields are value type, we can take advantage of the MemberwiseClone function.
        /// If a new derived class of ColumnInfo contains a reference type field, then they need to override this
        /// to create a deep copy of the reference field.
        /// </summary>
        /// <returns></returns>
        public virtual ColumnInfo DeepClone() => (ColumnInfo)MemberwiseClone();

        public override string ToString() => GetLocalizedColumnHeader();

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // Return true if it is of the same object type
            return GetType() == obj.GetType();
        }

        /// <summary>
        /// Need to override since we are overriding Equals
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// Override == operator to compare value instead of reference
        /// </summary>
        /// <param name="leftCi">ColumnInfo on the left side of the operator </param>
        /// <param name="rightCi">ColumnInfo on the right side of the operator</param>
        /// <returns></returns>
        public static bool operator ==(ColumnInfo leftCi, ColumnInfo rightCi)
        {
            if (ReferenceEquals(leftCi, null))
            {
                return ReferenceEquals(rightCi, null);
            }

            return leftCi.Equals(rightCi);
        }

        /// <summary>
        /// Override != operator to compare value instead of reference
        /// </summary>
        /// <param name="leftCi">ColumnInfo on the left side of the operator </param>
        /// <param name="rightCi">ColumnInfo on the right side of the operator</param>
        /// <returns></returns>
        public static bool operator !=(ColumnInfo leftCi, ColumnInfo rightCi)
        {
            return !(leftCi == rightCi);
        }
    }

    /// <summary>
    /// Helper utility class for ColumnInfo
    /// </summary>
    public static class ColumnInfoUtils
    {

        /// <summary>
        /// Get the index of the first ColumnInfo with the parameter Type columnInfoType.
        /// This should be used by simple column infos without properties. Ex. QueryIdColumnInfo, QueryTextColumnInfo, PlanForcedColumnInfo, etc.
        /// </summary>
        /// <param name="columnInfoList">The ColumnInfo list that we want to search in</param>
        /// <param name="columnInfoType">Type of ColumnInfo to search for</param>
        /// <returns>Index of the first ColumnInfo with the parameter Type columnInfoType</returns>
        public static int GetColumnIndex(IList<ColumnInfo> columnInfoList, Type columnInfoType)
        {
            System.Diagnostics.Debug.Assert(columnInfoList != null);
            System.Diagnostics.Debug.Assert(columnInfoType != null);

            Debug.Assert(columnInfoList != null, "columnInfoList != null");
            return columnInfoList.ToList().FindIndex(columnInfo => columnInfo.GetType() == columnInfoType);
        }

        /// <summary>
        /// Updates the current ColumnInfo object with the latest metric and statistic selection
        /// Only need to update if the ColumnInfo is of type StatisticMetricColumnInfo or derived from it
        /// </summary>
        /// <param name="columnInfo">The current sorting column that might need to be updated</param>
        /// <param name="metric">New metric selection</param>
        /// <param name="statistic">New statistic selection</param>
        public static ColumnInfo UpdateColumnInfo(this ColumnInfo columnInfo, Metric metric, Statistic statistic)
        {
            // Special case for when Metric = ExecutionCount and Statistic = Total.
            // This happens when Metric ExecutionCount is selected in the top resource consumers view.
            if (metric.Equals(Metric.ExecutionCount) && statistic.Equals(Statistic.Total))
            {
                return new ExecutionCountColumnInfo();
            }

            // Case when Executioncount or PlanCount is selected 
            //
            else if (columnInfo as ExecutionCountColumnInfo != null || columnInfo as NumPlansColumnInfo != null)
            {
                return new StatisticMetricColumnInfo(statistic, metric);
            }

            // Column is a derived class of StatisticMetricColumnInfo
            else if (columnInfo as StatisticMetricColumnInfo != null)
            {
                StatisticMetricColumnInfo smci = columnInfo as StatisticMetricColumnInfo;
                smci.Statistic = statistic;
                smci.Metric = metric;
                return smci;
            }

            return columnInfo;
        }
    }

    #region Simple derived classes of ColumnInfo

    public sealed class QueryIdColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelQueryId = @"query_id";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderQueryID;

        public override string GetQueryColumnLabel() => QueryColumnLabelQueryId;
    }

    public sealed class ObjectIdColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelObjectId = @"object_id";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderObjectID;

        public override string GetQueryColumnLabel() => QueryColumnLabelObjectId;
    }

    public sealed class ObjectNameColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelObjectName = @"object_name";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderObjectName;

        public override string GetQueryColumnLabel() => QueryColumnLabelObjectName;
    }

    public sealed class QueryTextColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelQueryText = @"query_sql_text";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderQuerySQLText;

        public override string GetQueryColumnLabel() => QueryColumnLabelQueryText;
    }

    public sealed class PlanIdColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelPlanId = @"plan_id";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderPlanID;

        public override string GetQueryColumnLabel() => QueryColumnLabelPlanId;
    }

    public sealed class ExecutionTypeColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelExecutionType = @"execution_type";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderExecutionType;

        public override string GetQueryColumnLabel() => QueryColumnLabelExecutionType;
    }

    public sealed class WaitCategoryDescColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelWaitCategoryDesc = @"wait_category_desc";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderWaitCategoryDesc;

        public override string GetQueryColumnLabel() => QueryColumnLabelWaitCategoryDesc;
    }

    public sealed class WaitCategoryIdColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelWaitCategoryId = @"wait_category";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderWaitCategoryId;

        public override string GetQueryColumnLabel() => QueryColumnLabelWaitCategoryId;
    }

    public sealed class NumPlansColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelNumPlans = @"num_plans";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderNumPlans;

        public override string GetQueryColumnLabel() => QueryColumnLabelNumPlans;
    }

    public sealed class ForcedPlanFailureCountColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelForcedPlanFailureCount = @"force_failure_count";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderForcedPlanFailureCount;

        public override string GetQueryColumnLabel() => QueryColumnLabelForcedPlanFailureCount;
    }

    internal sealed class ForcedPlanIdColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelForcedPlanId = @"plan_id";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderForcedPlanId;

        public override string GetQueryColumnLabel() => QueryColumnLabelForcedPlanId;
    }

    internal sealed class ForcedPlanFailureDescpColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelForcedPlanFailureDescp = @"last_force_failure_reason_desc";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderForcedPlanFailureDescp;

        public override string GetQueryColumnLabel() => QueryColumnLabelForcedPlanFailureDescp;
    }

    internal sealed class LastCompileStartTimeColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelLastCompileStartTime = @"last_compile_start_time";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderLastCompileStartTime;

        public override string GetQueryColumnLabel() => QueryColumnLabelLastCompileStartTime;
    }

    internal sealed class LastForcedPlanExecTimeColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelLastForcedPlanExecTime = @"last_execution_time";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderLastForcedPlanExecTime;

        public override string GetQueryColumnLabel() => QueryColumnLabelLastForcedPlanExecTime;
    }

    internal sealed class LastQueryExecTimeColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelLastQueryExecTime = @"last_execution_time";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderLastQueryExecTime;

        public override string GetQueryColumnLabel() => QueryColumnLabelLastQueryExecTime;
    }

    public sealed class PlanForcedColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelPlanForced = @"is_forced_plan";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderPlanForced;

        public override string GetQueryColumnLabel() => QueryColumnLabelPlanForced;
    }

    public sealed class FirstExecTimeColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelFirstExecutionTime = @"first_execution_time";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderFirstExecTime;

        public override string GetQueryColumnLabel() => QueryColumnLabelFirstExecutionTime;
    }

    public sealed class LastExecTimeColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelLastExecutionTime = @"last_execution_time";

        public override string GetLocalizedColumnHeader() => Resources.ColumnHeaderLastExecTime;

        public override string GetQueryColumnLabel() => QueryColumnLabelLastExecutionTime;
    }

    public sealed class BucketStartTimeColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelBucketStartTime = @"bucket_start";

        public override string GetLocalizedColumnHeader() =>
            //TODO review localization
            Resources.IntervalStartTime;

        public override string GetQueryColumnLabel() => QueryColumnLabelBucketStartTime;
    }

    public sealed class BucketEndTimeColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelBucketEndTime = @"bucket_end";

        public override string GetLocalizedColumnHeader() =>
            //TODO review localization
            Resources.IntervalEndTime;

        public override string GetQueryColumnLabel() => QueryColumnLabelBucketEndTime;
    }

    #endregion Simple derived classes of ColumnInfo

    /// <summary>
    /// For execution count columns.
    /// There are 3 variations:
    /// 1. exec_count
    /// 2. exec_count_recent
    /// 3. exec_count_history
    /// The variations are based on the ComparisonTypeInterval property
    /// </summary>
    public sealed class ExecutionCountColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelMetricTimeInterval = @"{0}_{1}";

        internal ComparisonTimeInterval TimeInterval { get; set; }

        /// <summary>
        /// For execution count columns without time interval
        /// Used for Plan Summary and Top Resource Consumers
        /// </summary>
        public ExecutionCountColumnInfo() => TimeInterval = ComparisonTimeInterval.None;

        /// <summary>
        /// For execution count with time interval
        /// Used for Regressed Queries
        /// </summary>
        /// <param name="timeInterval"></param>

        public ExecutionCountColumnInfo(ComparisonTimeInterval timeInterval) => TimeInterval = timeInterval;

        public override bool Equals(object obj)
        {
            // If parameter is null return false.

            // Return true if obj can be casted to ExecutionCountColumnInfo and all properties are equal
            ExecutionCountColumnInfo columnInfo = obj as ExecutionCountColumnInfo;
            if (columnInfo != null)
            {
                if (TimeInterval.Equals(columnInfo.TimeInterval))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Need to override since we are overriding Equals
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => base.GetHashCode();

        public override string GetLocalizedColumnHeader()
        {
            if (TimeInterval.Equals(ComparisonTimeInterval.None))
            {
                return Resources.ColumnHeaderExecCount;
            }
            else
            {
                return string.Format(
                    CultureInfo.CurrentUICulture,
                    Resources.ColumnHeaderMetricTime,
                    MetricUtils.LocalizedString(Metric.ExecutionCount),
                    ComparisonTimeIntervalUtils.LocalizedString(TimeInterval));
            }
        }

        public override string GetQueryColumnLabel()
        {
            if (TimeInterval.Equals(ComparisonTimeInterval.None))
            {
                return MetricUtils.QueryString(Metric.ExecutionCount);
            }
            else
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    QueryColumnLabelMetricTimeInterval,
                    MetricUtils.QueryString(Metric.ExecutionCount),
                    ComparisonTimeIntervalUtils.QueryString(TimeInterval));
            }
        }
    }

    public class StatisticMetricColumnInfo : ColumnInfo
    {
        private const string QueryColumnLabelStatisticMetric = @"{0}_{1}";

        public Statistic Statistic { get; set; }
        public Metric Metric { get; set; }

        /// <summary>
        /// If StatisticMetric column needs to bind any specific data at runtime. 
        /// set this flag to true.
        /// </summary>
        internal bool BindRuntimeData { get; set; }

        public StatisticMetricColumnInfo(Statistic statistic, Metric metric)
        {
            Statistic = statistic;
            Metric = metric;
            BindRuntimeData = false;
        }

        public override bool Equals(object obj)
        {
            // Return true if obj can be casted to StatisticMetricColumnInfo and all properties are equal
            StatisticMetricColumnInfo columnInfo = obj as StatisticMetricColumnInfo;
            if (columnInfo != null)
            {
                if (Statistic.Equals(columnInfo.Statistic) && Metric.Equals(columnInfo.Metric))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Need to override since we are overriding Equals
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => base.GetHashCode();

        public override string GetLocalizedColumnHeader()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                Resources.ColumnHeaderStatMetric,
                StatisticUtils.LocalizedString(Statistic),
                MetricUtils.LocalizedString(Metric));
        }

        public override string GetLocalizedColumnHeaderWithUnits()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                Resources.ColumnHeaderStatMetric,
                StatisticUtils.LocalizedString(Statistic),
                MetricUtils.LocalizedStringWithUnits(Metric));
        }

        public override string GetQueryColumnLabel()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                QueryColumnLabelStatisticMetric,
                StatisticUtils.QueryString(Statistic),
                MetricUtils.QueryString(Metric));
        }

        public override bool IsBindRuntimeData() => BindRuntimeData;
    }

    internal sealed class StatisticMetricTimeColumnInfo : StatisticMetricColumnInfo
    {
        private const string QueryColumnLabelStatisticMetricTimeInterval = @"{0}_{1}_{2}";

        internal ComparisonTimeInterval TimeInterval { get; set; }

        public StatisticMetricTimeColumnInfo(Statistic statistic, Metric metric, ComparisonTimeInterval timeInterval)
            : base(statistic, metric) => TimeInterval = timeInterval;

        public override bool Equals(object obj)
        {
            // Return true if obj can be casted to StatisticMetricTimeColumnInfo and all properties are equal
            StatisticMetricTimeColumnInfo columnInfo = obj as StatisticMetricTimeColumnInfo;
            if (columnInfo != null)
            {
                if (TimeInterval.Equals(columnInfo.TimeInterval) && base.Equals(obj))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Need to override since we are overriding Equals
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => base.GetHashCode();

        public override string GetLocalizedColumnHeader()
        {
            return string.Format(
                CultureInfo.CurrentUICulture,
                Resources.ColumnHeaderStatMetricTime,
                StatisticUtils.LocalizedString(Statistic),
                MetricUtils.LocalizedString(Metric),
                ComparisonTimeIntervalUtils.LocalizedString(TimeInterval));
        }

        public override string GetLocalizedColumnHeaderWithUnits()
        {
            return string.Format(
                CultureInfo.CurrentUICulture,
                Resources.ColumnHeaderStatMetricTime,
                StatisticUtils.LocalizedString(Statistic),
                MetricUtils.LocalizedStringWithUnits(Metric),
                ComparisonTimeIntervalUtils.LocalizedString(TimeInterval));
        }

        public override string GetQueryColumnLabel()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                QueryColumnLabelStatisticMetricTimeInterval,
                StatisticUtils.QueryString(Statistic),
                MetricUtils.QueryString(Metric),
                ComparisonTimeIntervalUtils.QueryString(TimeInterval));
        }
    }

    internal sealed class StatisticMetricRegressionColumnInfo : StatisticMetricColumnInfo
    {
        private const string QueryColumnLabelAdditionalWorkload = @"additional_{0}_workload";
        private const string QueryColumnLabelRegressionPercentage = @"{0}_regr_perc_recent";

        public StatisticMetricRegressionColumnInfo(Statistic statistic, Metric metric)
            : base(statistic, metric)
        {
        }

        public override bool Equals(object obj)
        {
            // Return false if obj cannot be casted to StatisticMetricRegressionColumnInfo
            StatisticMetricRegressionColumnInfo columnInfo = obj as StatisticMetricRegressionColumnInfo;
            if (columnInfo == null)
            {
                return false;
            }

            return base.Equals(columnInfo);
        }

        /// <summary>
        /// Need to override since we are overriding Equals
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => base.GetHashCode();

        public override string GetLocalizedColumnHeader()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                Statistic.Equals(Statistic.Total)
                    ? Resources.ColumnHeaderAdditionalWorkload
                    : Resources.ColumnHeaderRegrPercRecent,
                MetricUtils.LocalizedString(Metric));
        }

        public override string GetLocalizedColumnHeaderWithUnits()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                Statistic.Equals(Statistic.Total)
                    ? Resources.ColumnHeaderAdditionalWorkload
                    : Resources.ColumnHeaderRegrPercRecent,
                MetricUtils.LocalizedStringWithUnits(Metric));
        }

        public override string GetQueryColumnLabel()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                Statistic.Equals(Statistic.Total)
                    ? QueryColumnLabelAdditionalWorkload
                    : QueryColumnLabelRegressionPercentage,
                MetricUtils.QueryString(Metric));
        }
    }
}
