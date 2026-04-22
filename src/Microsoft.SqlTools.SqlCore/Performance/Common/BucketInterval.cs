//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.ComponentModel;

namespace Microsoft.SqlTools.SqlCore.Performance.Common
{
    [TypeConverter(typeof(BucketIntervalValueConverter))]
    public enum BucketInterval
    {
        Minute,
        Hour,
        Day,
        Week,
        Month,
        Automatic
    }

    public class BucketIntervalUtils
    {
        // maximum number of minutes in a time interval for the buck size to be in minutes
        private const int BucketSizeCutoffMinutes = 60;
        // maximum number of hours in the time interval for the bucket size to be in hours
        private const int BucketSizeCutoffHours = 48;
        // maximum number of days in the time interval for the bucket size to be in days
        private const int BucketSizeCutoffDays = 31;
        // maximum number of days in the time interval for the bucket size to be in weeks
        private const int BucketSizeCutoffWeeks = 300;
        // note that the largest bucket size is months

        public static string LocalizedString(BucketInterval interval)
        {
            switch (interval)
            {
                case BucketInterval.Minute:
                    return Resources.TimeIntervalOptionMinute;
                case BucketInterval.Hour:
                    return Resources.TimeIntervalOptionHour;
                case BucketInterval.Day:
                    return Resources.TimeIntervalOptionDay;
                case BucketInterval.Week:
                    return Resources.TimeIntervalOptionWeek;
                case BucketInterval.Month:
                    return Resources.TimeIntervalOptionMonth;
                case BucketInterval.Automatic:
                    return Resources.TimeIntervalOptionAuto;
                default:
                    {
                        // this indicates a code level error
                        System.Diagnostics.Debug.Assert(false, $"Unknown BucketInterval Type {interval}");
                        return string.Empty;
                    }
            }
        }

        /// <summary>
        /// Get TimeSpan from BucketInterval.
        /// </summary>
        /// <param name="bucketInterval"></param>
        /// <returns></returns>
        public static TimeSpan ToTimeSpan(BucketInterval bucketInterval)
        {
            switch (bucketInterval)
            {
                case BucketInterval.Minute:
                    return TimeSpan.FromMinutes(1);
                case BucketInterval.Hour:
                    return TimeSpan.FromHours(1);
                case BucketInterval.Day:
                    return TimeSpan.FromDays(1);
                case BucketInterval.Week:
                    return TimeSpan.FromDays(7);
                case BucketInterval.Month:
                    return TimeSpan.FromDays(30);
                case BucketInterval.Automatic:
                    return TimeSpan.Zero;
                default:
                    // this indicates a code level error
                    System.Diagnostics.Debug.Assert(false, $"Incorrect BucketInterval Type {bucketInterval}");
                    return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Get the string used in TSQL DATEADD / DATEDIFF functions for the specified time function
        /// </summary>
        /// <param name="bucketInterval"></param>
        /// <returns></returns>
        public static string DateFunctionIntervalString(BucketInterval bucketInterval)
        {
            string timeIntervalSpecification;
            switch (bucketInterval)
            {
                case BucketInterval.Minute:
                    timeIntervalSpecification = "mi";
                    break;
                case BucketInterval.Hour:
                    timeIntervalSpecification = "hh";
                    break;
                case BucketInterval.Day:
                    timeIntervalSpecification = "d";
                    break;
                case BucketInterval.Week:
                    timeIntervalSpecification = "ww";
                    break;
                case BucketInterval.Month:
                    timeIntervalSpecification = "m";
                    break;
                default:
                    // this is an error condition, gracefully log an error
                    System.Diagnostics.Debug.Assert(false, "Must specify a concrete time interval for query generator");
                    System.Diagnostics.Trace.TraceError("OverallResourceConsumptionQueryGenerator cannot handle TimeInterval {0}.  Defaulting to Days", bucketInterval);
                    timeIntervalSpecification = "d";
                    break;
            }

            return timeIntervalSpecification;
        }

        /// <summary>
        /// Method will determine an appropriate time interval in which to bucketize results based upon the input timespan
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static BucketInterval CalculateGoodSubInterval(TimeSpan duration)
        {
            if (duration.TotalMinutes <= BucketSizeCutoffMinutes)
            {
                return BucketInterval.Minute;
            }
            else if (duration.TotalHours <= BucketSizeCutoffHours)
            {
                return BucketInterval.Hour;
            }
            else if (duration.TotalDays <= BucketSizeCutoffDays)
            {
                return BucketInterval.Day;
            }
            else if (duration.TotalDays <= BucketSizeCutoffWeeks)
            {
                return BucketInterval.Week;
            }
            else
            {
                return BucketInterval.Month;
            }
        }
    }

    /// <summary>
    /// Provide a mapping between the enum selection for configuration and human-readable text
    /// </summary>
    public class BucketIntervalValueConverter : EnumStringConverter<BucketInterval>
    {
        protected override string EnumToString(BucketInterval enumValue) => BucketIntervalUtils.LocalizedString(enumValue);
    }
}
