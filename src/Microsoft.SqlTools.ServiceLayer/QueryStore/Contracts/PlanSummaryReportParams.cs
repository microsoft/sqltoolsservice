//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

    /// <summary>
    /// Gets the query for a Plan Summary chart view
    /// </summary>
    public class GetPlanSummaryChartViewRequest
    {
        public static readonly RequestType<GetPlanSummaryParams, QueryStoreQueryResult> Type
            = RequestType<GetPlanSummaryParams, QueryStoreQueryResult>.Create("queryStore/getPlanSummaryChartView");
    }

    /// <summary>
    /// Gets the query for a forced plan query
    /// </summary>
    public class GetForcedPlanQueryRequest // there's also GetForcedPlanQueries (plural) in QSM; how is that not confusing...
    {
        public static readonly RequestType<GetPlanSummaryParams, QueryStoreQueryResult> Type
            = RequestType<GetPlanSummaryParams, QueryStoreQueryResult>.Create("queryStore/getForcedPlanQuery");
    }

    /// <summary>
    /// Gets the query for a Plan Summary grid view
    /// </summary>
    public class GetPlanSummaryGridViewRequest
    {
        public static readonly RequestType<GetPlanSummaryParams, QueryStoreQueryResult> Type
            = RequestType<GetPlanSummaryParams, QueryStoreQueryResult>.Create("queryStore/getPlanSummaryGridView");
    }
}
