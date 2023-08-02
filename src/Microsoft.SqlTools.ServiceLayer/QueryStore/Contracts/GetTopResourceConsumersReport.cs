//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    public class GetTopResourceConsumersReportParams : QueryStoreReportParams
    {

    }

    /// <summary>
    /// Gets a Forced Plan Queries summary
    /// </summary>
    public class GetTopResourceConsumersSummaryRequest
    {
        public static readonly RequestType<GetTopResourceConsumersReportParams, QueryStoreQueryResult> Type
            = RequestType<GetTopResourceConsumersReportParams, QueryStoreQueryResult>.Create("queryStore/getTopResourceConsumersSummary");
    }

    /// <summary>
    /// Gets a Forced Plan Queries detailed summary
    /// </summary>
    public class GetTopResourceConsumersDetailedSummaryRequest
    {
        public static readonly RequestType<GetTopResourceConsumersReportParams, QueryStoreQueryResult> Type
            = RequestType<GetTopResourceConsumersReportParams, QueryStoreQueryResult>.Create("queryStore/getTopResourceConsumersDetailedSummary");
    }

    /// <summary>
    /// Gets a Forced Plan Queries detailed summary with wait stats
    /// </summary>
    public class GetTopResourceConsumersDetailedSummaryWithWaitStatsRequest
    {
        public static readonly RequestType<GetTopResourceConsumersReportParams, QueryStoreQueryResult> Type
            = RequestType<GetTopResourceConsumersReportParams, QueryStoreQueryResult>.Create("queryStore/getTopResourceConsumersDetailedSummaryWithWaitStats");
    }
}
