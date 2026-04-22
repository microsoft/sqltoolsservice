//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.SqlCore.Performance.Common
{
    public enum ComparisonTimeInterval
    {
        None,
        Recent,
        History
    }

    public static class ComparisonTimeIntervalUtils
    {

        private const string HistoryString = @"hist";
        private const string RecentString = @"recent";

        public static string QueryString(ComparisonTimeInterval timeInterval)
        {
            switch (timeInterval)
            {
                case ComparisonTimeInterval.History:
                    return HistoryString;
                case ComparisonTimeInterval.Recent:
                    return RecentString;
                default:
                    System.Diagnostics.Trace.TraceError(string.Format("Invalid time interval {0} for ComparisonTimeIntervalUtils.ToString()", timeInterval));
                    return string.Empty;
            }
        }

        public static string LocalizedString(ComparisonTimeInterval timeInterval)
        {
            switch (timeInterval)
            {
                case ComparisonTimeInterval.History:
                    return Resources.HistoryLabelText;
                case ComparisonTimeInterval.Recent:
                    return Resources.RecentLabelText;
                default:
                    System.Diagnostics.Trace.TraceError(string.Format("Invalid time interval {0} for ComparisonTimeIntervalUtils.ToString()", timeInterval));
                    return string.Empty;
            }
        }
    }
}
