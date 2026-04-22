//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.Common
{
    [TypeConverter(typeof(TimeIntervalValueConverter))]
    public enum TimeIntervalOptions
    {
        [LocalizedStringAttribute("TimeIntervalOptionLast5Minutes")]
        Last5Minutes,
        [LocalizedStringAttribute("TimeIntervalOptionLast15Minutes")]
        Last15Minutes,
        [LocalizedStringAttribute("TimeIntervalOptionLast30Minutes")]
        Last30Minutes,
        [LocalizedStringAttribute("TimeIntervalOptionLastHour")]
        LastHour,
        [LocalizedStringAttribute("TimeIntervalOptionLast12Hours")]
        Last12Hours,
        [LocalizedStringAttribute("TimeIntervalOptionLastDay")]
        LastDay,
        [LocalizedStringAttribute("TimeIntervalOptionLast2Days")]
        Last2Days,
        [LocalizedStringAttribute("TimeIntervalOptionLastWeek")]
        LastWeek,
        [LocalizedStringAttribute("TimeIntervalOptionLast2Weeks")]
        Last2Weeks,
        [LocalizedStringAttribute("TimeIntervalOptionLastMonth")]
        LastMonth,
        [LocalizedStringAttribute("TimeIntervalOptionsLast3Months")]
        Last3Months,
        [LocalizedStringAttribute("TimeIntervalOptionsLast6Months")]
        Last6Months,
        [LocalizedStringAttribute("TimeIntervalOptionsLastYear")]
        LastYear,
        [LocalizedStringAttribute("TimeIntervalOptionsAllTime")]
        AllTime,
        [LocalizedStringAttribute("TimeIntervalOptionsCustom")]
        Custom
    }

    public struct TimeInterval
    {
        private DateTimeOffset startDateTimeInUtc;
        private DateTimeOffset endDateTimeInUtc;
        private TimeIntervalOptions timeIntervalOptions;

        public TimeInterval(TimeInterval timeInterval)
        {
            this.startDateTimeInUtc = timeInterval.startDateTimeInUtc;
            this.endDateTimeInUtc = timeInterval.endDateTimeInUtc;
            this.timeIntervalOptions = timeInterval.timeIntervalOptions;
        }

        public TimeInterval(TimeIntervalOptions timeIntervalOptions)
        {
            // Set Dates for Custom Option
            this.endDateTimeInUtc = DateTimeOffset.UtcNow;
            this.startDateTimeInUtc = this.endDateTimeInUtc.AddDays(-1.0);

            this.timeIntervalOptions = timeIntervalOptions;
        }

        public TimeInterval(DateTimeOffset startDateTimeInUtc, DateTimeOffset endDateTimeInUtc)
        {
            this.startDateTimeInUtc = startDateTimeInUtc;
            this.endDateTimeInUtc = endDateTimeInUtc;
            this.timeIntervalOptions = TimeIntervalOptions.Custom;
        }

        public DateTimeOffset StartDateTimeOffset
        {
            get
            {
                DateTimeOffset result = this.TimeIntervalOptions == TimeIntervalOptions.Custom
                    ? this.startDateTimeInUtc
                    : TimeIntervalUtils.GetDateTimeOffset(DateTime.UtcNow, this.TimeIntervalOptions);

                return QueryStoreCommonConfiguration.DisplayTimeKind == DateTimeKind.Local
                    ? result.ToLocalTime()
                    : result.ToUniversalTime();
            }
            set
            {
                this.startDateTimeInUtc = value;
                this.timeIntervalOptions = TimeIntervalOptions.Custom;
            }
        }

        public DateTimeOffset EndDateTimeOffset
        {
            get
            {
                DateTimeOffset result = this.TimeIntervalOptions == TimeIntervalOptions.Custom
                    ? this.endDateTimeInUtc
                    : DateTimeOffset.UtcNow;

                return QueryStoreCommonConfiguration.DisplayTimeKind == DateTimeKind.Local
                    ? result.ToLocalTime()
                    : result.ToUniversalTime();
            }
            set
            {
                this.endDateTimeInUtc = value;
                this.timeIntervalOptions = TimeIntervalOptions.Custom;
            }
        }

        public TimeIntervalOptions TimeIntervalOptions
        {
            get { return this.timeIntervalOptions; }
            set { this.timeIntervalOptions = value; }
        }

        public TimeSpan TimeSpan => this.EndDateTimeOffset.Subtract(this.StartDateTimeOffset);

        public override string ToString() => TimeIntervalUtils.LocalizedString(this.TimeIntervalOptions);
    }

    public static class TimeIntervalUtils
    {
        private static readonly IList<TimeIntervalOptions> TimeIntervalOptionsWithoutAll = new List<TimeIntervalOptions>
        {
            TimeIntervalOptions.Last5Minutes,
            TimeIntervalOptions.Last15Minutes,
            TimeIntervalOptions.Last30Minutes,
            TimeIntervalOptions.LastHour,
            TimeIntervalOptions.Last12Hours,
            TimeIntervalOptions.LastDay,
            TimeIntervalOptions.Last2Days,
            TimeIntervalOptions.LastWeek,
            TimeIntervalOptions.Last2Weeks,
            TimeIntervalOptions.LastMonth,
            TimeIntervalOptions.Last3Months,
            TimeIntervalOptions.Last6Months,
            TimeIntervalOptions.LastYear,
            TimeIntervalOptions.Custom
        };

        internal static DateTimeOffset GetDateTimeOffset(DateTimeOffset dateTime, TimeIntervalOptions timeIntervalOptions)
        {
            switch (timeIntervalOptions)
            {
                case TimeIntervalOptions.Last5Minutes:
                    return dateTime.AddMinutes(-5.0);

                case TimeIntervalOptions.Last15Minutes:
                    return dateTime.AddMinutes(-15.0);

                case TimeIntervalOptions.Last30Minutes:
                    return dateTime.AddMinutes(-30.0);

                case TimeIntervalOptions.LastHour:
                    return dateTime.AddHours(-1.0);

                case TimeIntervalOptions.Last12Hours:
                    return dateTime.AddHours(-12.0);

                case TimeIntervalOptions.LastDay:
                    return dateTime.AddDays(-1.0);

                case TimeIntervalOptions.Last2Days:
                    return dateTime.AddDays(-2.0);

                case TimeIntervalOptions.LastWeek:
                    return dateTime.AddDays(-7.0);

                case TimeIntervalOptions.Last2Weeks:
                    return dateTime.AddDays(-14.0);

                case TimeIntervalOptions.LastMonth:
                    return dateTime.AddMonths(-1);

                case TimeIntervalOptions.Last3Months:
                    return dateTime.AddMonths(-3);

                case TimeIntervalOptions.Last6Months:
                    return dateTime.AddMonths(-6);

                case TimeIntervalOptions.LastYear:
                    return dateTime.AddYears(-1);

                case TimeIntervalOptions.AllTime:
                    return DateTimeOffset.MinValue;

                case TimeIntervalOptions.Custom:
                    System.Diagnostics.Debug.Assert(false, "Unable to get date from TimeIntervalOptions.Custom");
                    throw new InvalidOperationException("timeIntervalOptions");

                default:
                    {
                        // this indicates a code level error
                        System.Diagnostics.Debug.Assert(false, $"Unknown TimeInterval Type {timeIntervalOptions}");
                        throw new ArgumentOutOfRangeException("timeIntervalOptions");
                    }
            }
        }

        /// <summary>
        /// Return the time interval string used in pane header titles.
        /// </summary>
        /// <param name="timeInterval"></param>
        /// <returns></returns>
        public static string LocalizedPaneHeaderString(TimeInterval timeInterval)
        {
            if (timeInterval.TimeIntervalOptions == TimeIntervalOptions.Custom ||
                timeInterval.TimeIntervalOptions == TimeIntervalOptions.AllTime)
            {
                return string.Format("{0} {1} - {2} {3}",
                timeInterval.StartDateTimeOffset.DateTime.ToShortDateString(),
                timeInterval.StartDateTimeOffset.DateTime.ToShortTimeString(),
                timeInterval.EndDateTimeOffset.DateTime.ToShortDateString(),
                timeInterval.EndDateTimeOffset.DateTime.ToShortTimeString());
            }

            return string.Format(Resources.TimeIntervalPaneHeader,
                LocalizedString(timeInterval.TimeIntervalOptions),
                timeInterval.EndDateTimeOffset.DateTime.ToShortDateString(),
                timeInterval.EndDateTimeOffset.DateTime.ToShortTimeString());
        }

        /// <summary>
        /// Return the time interval string used in pane header titles in lower case.
        /// </summary>
        /// <param name="timeInterval"></param>
        /// <returns></returns>
        internal static string LocalizedPaneHeaderStringInLower(TimeInterval timeInterval) => LocalizedPaneHeaderString(timeInterval).ToLower(CultureInfo.CurrentUICulture);

        /// <summary>
        /// Return the time interval string used in pane header titles.
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string LocalizedString(TimeIntervalOptions enumValue)
        {
            LocalizedStringAttribute attribute = enumValue.GetType()
                .GetMember(enumValue.ToString()).Single()
                .GetCustomAttributes(typeof(LocalizedStringAttribute), false)
                .FirstOrDefault() as LocalizedStringAttribute;

            if (attribute != null)
                return Resources.ResourceManager.GetString(attribute.Value);

            // this indicates a code level error
            System.Diagnostics.Debug.Assert(false, $"Unknown TimeIntervalOptions Type {enumValue}");
            return string.Empty;
        }

        public static IList<TimeIntervalOptions> GetTimeOptionsExcludeAll() => TimeIntervalOptionsWithoutAll;
    }

    /// <summary>
    /// Provide a mapping between the enum selection for configuration and human-readable text
    /// </summary>
    public class TimeIntervalValueConverter : EnumStringConverter<TimeIntervalOptions>
    {
        protected override string EnumToString(TimeIntervalOptions enumValue) => TimeIntervalUtils.LocalizedString(enumValue);
    }
}
