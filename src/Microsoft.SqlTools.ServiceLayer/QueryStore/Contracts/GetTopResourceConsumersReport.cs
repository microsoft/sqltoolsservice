//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.QueryStoreModel.TopResourceConsumers;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    /// <summary>
    /// Parameters for getting a Top Resource Consumers report
    /// </summary>
    public class GetTopResourceConsumersReportParams : OrderableQueryConfigurationParams<TopResourceConsumersConfiguration>
    {
        /// <summary>
        /// Time interval for the report
        /// </summary>
        public BasicTimeInterval TimeInterval { get; set; }

        public override TopResourceConsumersConfiguration Convert()
        {
            TopResourceConsumersConfiguration result = base.Convert();
            result.TimeInterval = TimeInterval.Convert();

            return result;
        }
    }

    /// <summary>
    /// Gets a Top Resource Consumers summary
    /// </summary>
    public class GetTopResourceConsumersSummaryRequest
    {
        public static readonly RequestType<GetTopResourceConsumersReportParams, QueryStoreQueryResult> Type
            = RequestType<GetTopResourceConsumersReportParams, QueryStoreQueryResult>.Create("queryStore/getTopResourceConsumersSummary");
    }

    /// <summary>
    /// Gets a Top Resource Consumers detailed summary
    /// </summary>
    public class GetTopResourceConsumersDetailedSummaryRequest
    {
        public static readonly RequestType<GetTopResourceConsumersReportParams, QueryStoreQueryResult> Type
            = RequestType<GetTopResourceConsumersReportParams, QueryStoreQueryResult>.Create("queryStore/getTopResourceConsumersDetailedSummary");
    }
}
