//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.SqlTools.SqlCore.QueryDataStore.Common;

namespace Microsoft.SqlTools.SqlCore.QueryDataStore.RegressedQueries
{
    public class RegressedQueriesConfiguration : QueryConfigurationBase
    {
        public TimeInterval TimeIntervalRecent;
        public TimeInterval TimeIntervalHistory;
        public long MinExecutionCount;

        // Normal grid sorting fields
        public ColumnInfo GridSortByColumn;
        public bool SortGridByDescending;

        // Detailed grid sorting fields
        public ColumnInfo DetailedGridSortByColumn;
        public bool SortDetailedGridByDescending;

        // Chart sorting fields
        public ColumnInfo ChartYAxisColumn;
        public ColumnInfo ChartXAxisColumn;

        public RegressedQueriesConfiguration()
        {
            TimeIntervalRecent = new TimeInterval(TimeIntervalOptions.LastHour);
            TimeIntervalHistory = new TimeInterval(TimeIntervalOptions.LastWeek);
            MinExecutionCount = 1;
            SelectedStatistic = Statistic.Total;

            // Chart Y Axis defaults to regression column
            ChartYAxisColumn = new StatisticMetricRegressionColumnInfo(SelectedStatistic, SelectedMetric);
            ChartXAxisColumn = new QueryIdColumnInfo();

            // Normal grid default to sort by regression column
            GridSortByColumn = new StatisticMetricRegressionColumnInfo(SelectedStatistic, SelectedMetric);

            // Detailed grid default to sort by duration regression column
            DetailedGridSortByColumn = new StatisticMetricRegressionColumnInfo(SelectedStatistic, Metric.Duration);

            // Default to sort by descending
            SortGridByDescending = true;
            SortDetailedGridByDescending = true;
        }
    }
}
