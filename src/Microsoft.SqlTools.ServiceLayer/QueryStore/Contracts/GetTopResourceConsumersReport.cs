//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlServer.Management.QueryStoreModel.TopResourceConsumers;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    public class GetTopResourceConsumersReportParams : QueryConfigurationParams<TopResourceConsumersConfiguration>, IOrderableQueryParams
    {
        TimeInterval TimeInterval;
        public string OrderByColumnId;
        public bool Descending;

        public override TopResourceConsumersConfiguration Convert()
        {
            TopResourceConsumersConfiguration result = base.Convert();
            result.TimeInterval = TimeInterval;

            return result;
        }

        public string GetOrderByColumnId() => OrderByColumnId;
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
