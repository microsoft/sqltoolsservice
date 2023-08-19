//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.QueryStoreModel.ForcedPlanQueries;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    /// <summary>
    /// Parameters for getting a Forced Plan Queries report
    /// </summary>
    public class GetForcedPlanQueriesReportParams : OrderableQueryConfigurationParams<ForcedPlanQueriesConfiguration>
    {
        /// <summary>
        /// Time interval for the report
        /// </summary>
        public BasicTimeInterval TimeInterval { get; set; }

        public override ForcedPlanQueriesConfiguration Convert()
        {
            ForcedPlanQueriesConfiguration config = base.Convert();
            config.TimeInterval = TimeInterval.Convert();

            return config;
        }
    }

    /// <summary>
    /// Gets the query for a Forced Plan Queries report
    /// </summary>
    public class GetForcedPlanQueriesReportRequest
    {
        public static readonly RequestType<GetForcedPlanQueriesReportParams, QueryStoreQueryResult> Type
            = RequestType<GetForcedPlanQueriesReportParams, QueryStoreQueryResult>.Create("queryStore/getForcedPlanQueriesReport");
    }
}
