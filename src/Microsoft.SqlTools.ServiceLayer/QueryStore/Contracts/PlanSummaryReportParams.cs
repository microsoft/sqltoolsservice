//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlServer.Management.QueryStoreModel.PlanSummary;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using static Microsoft.SqlServer.Management.QueryStoreModel.PlanSummary.PlanSummaryConfiguration;

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    /// <summary>
    /// Parameters for getting a Plan Summary
    /// </summary>
    public class GetPlanSummaryParams : TypedQueryStoreReportParams<PlanSummaryConfiguration>
    {
        /// <summary>
        /// Query ID to view a summary of plans for
        /// </summary>
        public long QueryId { get; set; }

        /// <summary>
        /// Mode of the time interval search
        /// </summary>
        public PlanTimeIntervalMode TimeIntervalMode { get; set; }

        /// <summary>
        /// Time interval for the report
        /// </summary>
        public BasicTimeInterval TimeInterval { get; set; }

        /// <summary>
        /// Metric to summarize
        /// </summary>
        public Metric SelectedMetric { get; set; }

        /// <summary>
        /// Statistic to calculate on SelecticMetric
        /// </summary>
        public Statistic SelectedStatistic { get; set; }

        public override PlanSummaryConfiguration Convert() => new()
        {
            QueryId = QueryId,
            TimeIntervalMode = TimeIntervalMode,
            TimeInterval = TimeInterval.Convert(),
            SelectedMetric = SelectedMetric,
            SelectedStatistic = SelectedStatistic
        };
    }

    /// <summary>
    /// Parameters for getting the grid view of a Plan Summary
    /// </summary>
    public class GetPlanSummaryGridViewParams : GetPlanSummaryParams, IOrderableQueryParams
    {
        /// <summary>
        /// Name of the column to order results by
        /// </summary>
        public string OrderByColumnId { get; set; }

        /// <summary>
        /// Direction of the result ordering
        /// </summary>
        public bool Descending { get; set; }

        public string GetOrderByColumnId() => OrderByColumnId;
    }

    /// <summary>
    /// Parameters for getting the forced plan for a query
    /// </summary>
    public class GetForcedPlanParams : QueryStoreReportParams
    {
        /// <summary>
        /// Query ID to view the plan for
        /// </summary>
        public long QueryId { get; set; }

        /// <summary>
        /// Plan ID to view
        /// </summary>
        public long PlanId { get; set; }
    }

    /// <summary>
    /// Gets the query for a Plan Summary chart view
    /// </summary>
    public class GetPlanSummaryChartViewRequest
    {
        public static readonly RequestType<GetPlanSummaryParams, QueryStoreQueryResult> Type
            = RequestType<GetPlanSummaryParams, QueryStoreQueryResult>.Create("queryStore/getPlanSummaryChartView");
    }

    /// <summary>
    /// Gets the query for a Plan Summary grid view
    /// </summary>
    public class GetPlanSummaryGridViewRequest
    {
        public static readonly RequestType<GetPlanSummaryGridViewParams, QueryStoreQueryResult> Type
            = RequestType<GetPlanSummaryGridViewParams, QueryStoreQueryResult>.Create("queryStore/getPlanSummaryGridView");
    }

    /// <summary>
    /// Gets the query for a forced plan query
    /// </summary>
    public class GetForcedPlanRequest // there's also GetForcedPlanQueries (plural) in QSM; how is that not confusing...
    {
        public static readonly RequestType<GetForcedPlanParams, QueryStoreQueryResult> Type
            = RequestType<GetForcedPlanParams, QueryStoreQueryResult>.Create("queryStore/getForcedPlan");
    }
}
