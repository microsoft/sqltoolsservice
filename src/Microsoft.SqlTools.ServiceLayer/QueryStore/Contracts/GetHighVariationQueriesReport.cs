//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlServer.Management.QueryStoreModel.HighVariation;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    public class GetHighVariationQueriesReportParams : QueryConfigurationParams<HighVariationConfiguration>, IOrderableQueryParams
    {
        public TimeInterval TimeInterval { get; set; }
        public string OrderByColumnId { get; set; }
        public bool Descending { get; set; }

        public override HighVariationConfiguration Convert()
        {
            HighVariationConfiguration config = base.Convert();
            config.TimeInterval = TimeInterval;

            return config;
        }

        public string GetOrderByColumnId() => OrderByColumnId;
    }

    /// <summary>
    /// Gets the High Variation Queries summary
    /// </summary>
    public class GetHighVariationQueriesSummaryRequest
    {
        public static readonly RequestType<GetHighVariationQueriesReportParams, QueryStoreQueryResult> Type
            = RequestType<GetHighVariationQueriesReportParams, QueryStoreQueryResult>.Create("queryStore/getHighVariationQueriesSummary");
    }

    /// <summary>
    /// Gets the High Variation Queries detailed summary
    /// </summary>
    public class GetHighVariationQueriesDetailedSummaryRequest
    {
        public static readonly RequestType<GetHighVariationQueriesReportParams, QueryStoreQueryResult> Type
            = RequestType<GetHighVariationQueriesReportParams, QueryStoreQueryResult>.Create("queryStore/getHighVariationQueriesDetailedSummary");
    }

    /// <summary>
    /// Gets the High Variation Queries detailed summary with wait stats
    /// </summary>
    public class GetHighVariationQueriesDetailedSummaryWithWaitStatsRequest
    {
        public static readonly RequestType<GetHighVariationQueriesReportParams, QueryStoreQueryResult> Type
            = RequestType<GetHighVariationQueriesReportParams, QueryStoreQueryResult>.Create("queryStore/getHighVariationQueriesDetailedSummaryWithWaitStats");
    }
}
