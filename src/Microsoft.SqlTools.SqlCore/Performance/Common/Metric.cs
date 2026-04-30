//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Microsoft.SqlTools.SqlCore.Performance.Common
{
    /// <summary>
    /// This is the list of data points that are supported in query store UI. We call them metrics.
    /// DatabaseColumnName attribute can directly be mapped to column names in sys.query_store_runtime_stats dmv.
    /// </summary>
    [TypeConverter(typeof(MetricValueConverter))]
    public enum Metric
    {
        [DatabaseColumnName("avg_cpu_time")]
        [QueryString("cpu_time")]
        [LocalizedString("MetricOptionCPUTime")]
        [Units("UnitMillisecond")]
        CPUTime,

        [DatabaseColumnName("avg_duration")]
        [QueryString("duration")]
        [LocalizedString("MetricOptionDuration")]
        [Units("UnitMillisecond")]
        Duration,

        [DatabaseColumnName("avg_logical_io_writes")]
        [QueryString("logical_io_writes")]
        [LocalizedString("MetricOptionLogicalWrites")]
        [Units("UnitKilobyte")]
        LogicalWrites,

        [DatabaseColumnName("avg_logical_io_reads")]
        [QueryString("logical_io_reads")]
        [LocalizedString("MetricOptionLogicalReads")]
        [Units("UnitKilobyte")]
        LogicalReads,

        [DatabaseColumnName("avg_query_max_used_memory")]
        [QueryString("query_max_used_memory")]
        [LocalizedString("MetricOptionMemoryConsumption")]
        [Units("UnitKilobyte")]
        MemoryConsumption,

        [DatabaseColumnName("avg_physical_io_reads")]
        [QueryString("physical_io_reads")]
        [LocalizedString("MetricOptionPhysicalReads")]
        [Units("UnitKilobyte")]
        PhysicalReads,

        [DatabaseColumnName("count_executions")]
        [QueryString("count_executions")]
        [LocalizedString("MetricOptionExecutions")]
        ExecutionCount,

        [DatabaseColumnName("avg_clr_time")]
        [QueryString("clr_time")]
        [LocalizedString("MetricOptionClrTime")]
        [Units("UnitMillisecond")]
        ClrTime,

        [DatabaseColumnName("avg_dop")]
        [QueryString("dop")]
        [LocalizedString("MetricOptionDop")]
        Dop,

        [DatabaseColumnName("avg_rowcount")]
        [QueryString("rowcount")]
        [LocalizedString("MetricOptionRowcount")]
        RowCount,

        [DatabaseColumnName("avg_log_bytes_used")]
        [QueryString("log_bytes_used")]
        [LocalizedString("MetricOptionLogMemoryUsed")]
        [Units("UnitKilobyte")]
        LogMemoryUsed,

        [DatabaseColumnName("avg_tempdb_space_used")]
        [QueryString("tempdb_space_used")]
        [LocalizedString("MetricOptionTempDbMemoryUsed")]
        [Units("UnitKilobyte")]
        TempDbMemoryUsed,

        [DatabaseColumnName("wait_stats_id")]
        [QueryString("query_wait_time")]
        [LocalizedString("MetricOptionQueryWaitTime")]
        [Units("UnitMillisecond")]
        WaitTime
    }

    public static class MetricUtils
    {

        /// <summary>
        /// Gets the LocalizedString value of this Metric
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string LocalizedString(Metric enumValue)
        {
            LocalizedStringAttribute attribute = enumValue.GetType()
                .GetMember(enumValue.ToString()).Single()
                .GetCustomAttributes(typeof(LocalizedStringAttribute), false)
                .FirstOrDefault() as LocalizedStringAttribute;

            if (attribute != null)
                return SR.Keys.GetString(attribute.Value);

            // this indicates a code level error
            System.Diagnostics.Debug.Assert(false, $"Unknown Metric Type {enumValue}");
            return String.Empty;
        }

        /// <summary>
        /// Gets the QueryString value of this Metric
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string QueryString(Metric enumValue)
        {
            QueryStringAttribute attribute = enumValue.GetType()
                .GetMember(enumValue.ToString()).Single()
                .GetCustomAttributes(typeof(QueryStringAttribute), false)
                .FirstOrDefault() as QueryStringAttribute;

            if (attribute != null)
                return attribute.Value;

            throw new ArgumentException("Invalid selection for Metric - " + enumValue);
        }

        /// <summary>
        /// Gets the QueryString value of this Metric
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string LocalizedStringWithUnits(Metric enumValue)
        {
            UnitsAttribute attribute = enumValue.GetType()
                .GetMember(enumValue.ToString()).Single()
                .GetCustomAttributes(typeof(UnitsAttribute), false)
                .FirstOrDefault() as UnitsAttribute;

            if (attribute != null)
                return SR.MetricOptionWithUnitsTemplate(LocalizedString(enumValue),
                    SR.Keys.GetString(attribute.Value));

            return LocalizedString(enumValue);
        }

        /// <summary>
        /// returns the mapping for metric names in database to enumeration
        /// </summary>
        /// <returns></returns>
        public static IDictionary<string, Metric> DbNamesToServerSupportedMetricMapping()
        {
            var dbNamesToServerSupportedMetricMapping = new Dictionary<string, Metric>();

            foreach (Metric metric in Enum.GetValues(typeof(Metric)).Cast<Metric>())
            {
                DatabaseColumnNameAttribute attribute = metric.GetType()
                .GetMember(metric.ToString()).Single()
                .GetCustomAttributes(typeof(DatabaseColumnNameAttribute), false)
                .FirstOrDefault() as DatabaseColumnNameAttribute;

                if (attribute != null)
                {
                    dbNamesToServerSupportedMetricMapping.Add(attribute.Value, metric);
                }
            }
            return dbNamesToServerSupportedMetricMapping;
        }

        /// <summary>
        /// Gets the Conversion factor for the selected Metric. This can be different for different Metric. 
        /// This is necessary because recoded units for a Metric is different from the reported unit. 
        /// For eg: Duration is recorded in microseconds but reported in Milliseconds and 
        /// Memory Consumption is recorded in 8KB pages and reported in KB.
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static float GetConversionFactor(this Metric enumValue)
        {
            switch (enumValue)
            {
                case Metric.LogicalReads:
                case Metric.LogicalWrites:
                case Metric.PhysicalReads:
                case Metric.MemoryConsumption:
                case Metric.TempDbMemoryUsed:
                    // Number of 8KB page to KB
                    return 8;
                case Metric.CPUTime:
                case Metric.Duration:
                case Metric.ClrTime:
                    // microseconds to milliseconds
                    return 0.001F;
                case Metric.LogMemoryUsed:
                    // Bytes to KB
                    return 0.0009765625F;
                default:
                    return 1;
            }
        }

        /// <summary>
        /// Gets the number of decimal points to round off the value
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static float GetRoundOffPoints(this Metric enumValue)
        {
            switch (enumValue)
            {
                case Metric.ExecutionCount:
                case Metric.Dop:
                case Metric.RowCount:
                    return 0;
                default:
                    return 2;
            }
        }
    }

    /// <summary>
    /// Provide a mapping between the enum selection for configuration and human-readable text
    /// </summary>
    public class MetricValueConverter : EnumStringConverter<Metric>
    {
        protected override string EnumToString(Metric enumValue) => MetricUtils.LocalizedStringWithUnits(enumValue);

        // Convert from a string to the Metric enum.
        //
        // DEVNOTE(MatteoT): removing this logic would cause to regress
        // https://msdata.visualstudio.com/SQLToolsAndLibraries/_workitems/edit/3104868
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => (Metric)Enum.Parse(typeof(Metric), value as string);
    }
}
