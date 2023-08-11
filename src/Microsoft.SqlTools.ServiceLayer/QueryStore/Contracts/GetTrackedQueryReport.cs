//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.QueryStoreModel.TrackedQueries;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    public class GetTrackedQueriesReportParams : GetPlanSummaryParams
    {
        // TrackedQueriesConfiguration doesn't contain any new properties useful to query generation, so a straight conversion here is fine.
        public override TrackedQueriesConfiguration Convert() => (TrackedQueriesConfiguration)base.Convert();
    }

    /// <summary>
    /// Gets the report for a Forced Plan Queries summary
    /// </summary>
    public class GetTrackedQueriesReportRequest
    {
        public static readonly RequestType<GetTrackedQueriesReportParams, QueryStoreQueryResult> Type
            = RequestType<GetTrackedQueriesReportParams, QueryStoreQueryResult>.Create("queryStore/getTrackedQueriesReport");
    }
}
