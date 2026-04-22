//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Microsoft.SqlServer.Management.QueryStoreModel.Common
{
    /// <summary>
    /// This is a list of aggregations that are supported in query store UI. We call them Statistics.
    /// Any selected data point (CPU, Duration, etc.) can be aggregated on the selected Statistic.
    /// </summary>
    [TypeConverter(typeof(StatisticValueConverter))]
    public enum Statistic
    {
        [RuntimeStatsCalculationSummaryAttribute("CONVERT(float, SUM({2}.{0}_{1}*{2}.count_executions))/NULLIF(SUM({2}.count_executions), 0)")]
        [WaitStatsCalculationSummaryAttribute("CONVERT(float, SUM({0}.total_query_wait_time_ms)/SUM({0}.total_query_wait_time_ms/{0}.avg_query_wait_time_ms))")]
        [QueryStringAttribute("avg")]
        [LocalizedStringAttribute("StatisticsOptionAvg")]
        Avg,
        [RuntimeStatsCalculationSummaryAttribute("CONVERT(float, MIN({2}.{0}_{1}))")]
        [WaitStatsCalculationSummaryAttribute("CONVERT(float, MIN({0}.min_query_wait_time_ms))")]
        [QueryStringAttribute("min")]
        [LocalizedStringAttribute("StatisticsOptionMin")]
        Min,
        [RuntimeStatsCalculationSummaryAttribute("CONVERT(float, MAX({2}.{0}_{1}))")]
        [WaitStatsCalculationSummaryAttribute("CONVERT(float, MAX({0}.max_query_wait_time_ms))")]
        [QueryStringAttribute("max")]
        [LocalizedStringAttribute("StatisticsOptionMax")]
        Max,
        [RuntimeStatsCalculationSummaryAttribute("CONVERT(float, SQRT( SUM({2}.{0}_{1}*{2}.{0}_{1}*{2}.count_executions)/NULLIF(SUM({2}.count_executions), 0)))")]
        [WaitStatsCalculationSummaryAttribute("CONVERT(float, SQRT( SUM({0}.stdev_query_wait_time_ms*{0}.stdev_query_wait_time_ms*({0}.total_query_wait_time_ms/{0}.avg_query_wait_time_ms))/SUM({0}.total_query_wait_time_ms/{0}.avg_query_wait_time_ms)))")]
        [QueryStringAttribute("stdev")]
        [LocalizedStringAttribute("StatisticsOptionStddev")]
        Stdev,                      // matches QS backing table definitions
        [QueryStringAttribute("last")]
        [WaitStatsCalculationSummaryAttribute("CONVERT(float, MIN({0}.last_query_wait_time))")]
        [LocalizedStringAttribute("StatisticsOptionLast")]
        Last,
        [RuntimeStatsCalculationSummaryAttribute("CONVERT(float, SUM({2}.{0}_{1}*{2}.count_executions))")]
        [WaitStatsCalculationSummaryAttribute("CONVERT(float, SUM({0}.total_query_wait_time_ms))")]
        [QueryStringAttribute("total")]
        [LocalizedStringAttribute("StatisticsOptionTotal")]
        Total,
        [RuntimeStatsCalculationSummaryAttribute("ISNULL(ROUND(CONVERT(float, (SQRT( SUM({2}.{0}_{1}*{2}.{0}_{1}*{2}.count_executions)/NULLIF(SUM({2}.count_executions), 0))*SUM({2}.count_executions)) / NULLIF(SUM({2}.{3}_{1}*{2}.count_executions), 0)),2), 0)")]
        [QueryStringAttribute("variation")]
        [LocalizedStringAttribute("StatisticsOptionVariation")]
        Variation
    }

    public class StatisticUtils
    {

        /// <summary>
        /// Gets the LocalizedString value of this Statistic
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string LocalizedString(Statistic enumValue)
        {
            LocalizedStringAttribute attribute = enumValue.GetType()
                .GetMember(enumValue.ToString()).Single()
                .GetCustomAttributes(typeof(LocalizedStringAttribute), false)
                .FirstOrDefault() as LocalizedStringAttribute;

            if (attribute != null)
                return Resources.ResourceManager.GetString(attribute.Value);

            // this indicates a code level error
            System.Diagnostics.Debug.Assert(false, $"Unknown Statistic Type {enumValue}");
            return String.Empty;
        }

        /// <summary>
        /// Gets the QueryString value of this Statistic
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string QueryString(Statistic enumValue)
        {
            QueryStringAttribute attribute = enumValue.GetType()
                .GetMember(enumValue.ToString()).Single()
                .GetCustomAttributes(typeof(QueryStringAttribute), false)
                .FirstOrDefault() as QueryStringAttribute;

            if (attribute != null)
                return attribute.Value;

            throw new ArgumentException("Invalid selection for statistic - " + enumValue);
        }

        /// <summary>
        /// Gets the mathematical formula for this Statistic from sys.query_store_runtime_stats
        /// </summary>
        /// <param name="enumValue"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string GetAggregationFormulaForRuntimeStats(Statistic enumValue, params object[] args)
        {
            RuntimeStatsCalculationSummaryAttribute attribute = enumValue.GetType()
                .GetMember(enumValue.ToString()).Single()
                .GetCustomAttributes(typeof(RuntimeStatsCalculationSummaryAttribute), false)
                .FirstOrDefault() as RuntimeStatsCalculationSummaryAttribute;

            if (attribute == null)
                throw new ArgumentException("Invalid selection for statistic - " + enumValue);

            return string.Format(attribute.Value, args);
        }

        /// <summary>
        /// Gets the mathematical formula for this Statistic from sys.query_store_wait_stats
        /// </summary>
        /// <param name="enumValue"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string GetAggregationFormulaForWaitStats(Statistic enumValue, params object[] args)
        {
            WaitStatsCalculationSummaryAttribute attribute = enumValue.GetType()
                .GetMember(enumValue.ToString()).Single()
                .GetCustomAttributes(typeof(WaitStatsCalculationSummaryAttribute), false)
                .FirstOrDefault() as WaitStatsCalculationSummaryAttribute;

            if (attribute == null)
                throw new ArgumentException("Invalid selection for statistic - " + enumValue);

            return string.Format(attribute.Value, args);
        }
    }

    /// <summary>
    /// Provide a mapping between the enum selection for configuration and human-readable text
    /// </summary>
    public class StatisticValueConverter : EnumStringConverter<Statistic>
    {
        protected override string EnumToString(Statistic enumValue) => StatisticUtils.LocalizedString(enumValue);

        // Convert from a string to the Statistic enum
        //
        // DEVNOTE(MatteoT): removing this logic would cause to regress
        // https://msdata.visualstudio.com/SQLToolsAndLibraries/_workitems/edit/3104868
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => (Statistic)Enum.Parse(typeof(Statistic), value as string);
    }
}
