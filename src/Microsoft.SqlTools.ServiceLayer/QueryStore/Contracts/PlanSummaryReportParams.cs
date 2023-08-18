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
    public class GetPlanSummaryParams : TypedQueryStoreReportParams<PlanSummaryConfiguration>
    {
        public long QueryId { get; set; }
        public PlanTimeIntervalMode TimeIntervalMode { get; set; }
        public BasicTimeInterval TimeInterval { get; set; }
        public Metric SelectedMetric { get; set; }
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

    public class GetPlanSummaryGridViewParams : GetPlanSummaryParams, IOrderableQueryParams
    {
        public string OrderByColumnId { get; set; }
        public bool Descending { get; set; }

        public string GetOrderByColumnId() => OrderByColumnId;
    }

    public class GetForcedPlanParams : QueryStoreReportParams
    {
        public long QueryId { get; set; }
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
