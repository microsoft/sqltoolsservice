//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.QueryStoreModel.HighVariation;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    /// <summary>
    /// Parameters for getting a High Variation Queries report
    /// </summary>
    public class GetHighVariationQueriesReportParams : OrderableQueryConfigurationParams<HighVariationConfiguration>
    {
        /// <summary>
        /// Time interval for the report
        /// </summary>
        public BasicTimeInterval TimeInterval { get; set; }

        public override HighVariationConfiguration Convert()
        {
            HighVariationConfiguration config = base.Convert();
            config.TimeInterval = TimeInterval.Convert();

            return config;
        }
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
}
