//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.SqlServer.Management.QueryStoreModel.Common
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
