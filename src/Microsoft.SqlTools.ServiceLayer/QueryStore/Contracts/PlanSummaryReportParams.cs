//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlServer.Management.QueryStoreModel.PlanSummary;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    public class GetPlanSummaryParams : TypedQueryStoreReportParams<PlanSummaryConfiguration>
    {
        public long QueryId;
        //public PlanTimeIntervalMode TimeIntervalMode; // TODO: make enum public in QueryStoreModel
        public TimeInterval TimeInterval;
        public Metric SelectedMetric;
        public Statistic SelectedStatistic;

        public override PlanSummaryConfiguration Convert() => new()
        {
            QueryId = QueryId,
            TimeInterval = TimeInterval,
            SelectedMetric = SelectedMetric,
            SelectedStatistic = SelectedStatistic
        };
    }

    public class GetPlanSummaryGridViewParams : GetPlanSummaryParams
    {
        public string OrderByColumnId;
        public bool Descending;
    }

    public class GetForcedPlanParams : QueryStoreReportParams
    {
        public long QueryId;
        public long PlanId;
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
