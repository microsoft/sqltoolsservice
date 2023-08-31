//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.QueryStoreModel.ForcedPlanQueries;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    public class GetForcedPlanQueriesReportParams : QueryConfigurationParams<ForcedPlanQueriesConfiguration>, IOrderableQueryParams
    {
        public BasicTimeInterval TimeInterval { get; set; }
        public string OrderByColumnId { get; set; }
        public bool Descending { get; set; }

        public override ForcedPlanQueriesConfiguration Convert()
        {
            ForcedPlanQueriesConfiguration config = base.Convert();
            config.TimeInterval = TimeInterval.Convert();

            return config;
        }

        public string GetOrderByColumnId() => OrderByColumnId;
    }

    /// <summary>
    /// Gets the report for a Forced Plan Queries summary
    /// </summary>
    public class GetForcedPlanQueriesReportRequest
    {
        public static readonly RequestType<GetForcedPlanQueriesReportParams, QueryStoreQueryResult> Type
            = RequestType<GetForcedPlanQueriesReportParams, QueryStoreQueryResult>.Create("queryStore/getForcedPlanQueriesReport");
    }
}
