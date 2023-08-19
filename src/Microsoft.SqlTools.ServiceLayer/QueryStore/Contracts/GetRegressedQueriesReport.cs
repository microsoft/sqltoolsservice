//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.QueryStoreModel.RegressedQueries;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    /// <summary>
    /// Parameters for getting a Regressed Queries report
    /// </summary>
    public class GetRegressedQueriesReportParams : QueryConfigurationParams<RegressedQueriesConfiguration>
    {
        /// <summary>
        /// Time interval during which to look for performance regressions for the report
        /// </summary>
        public BasicTimeInterval TimeIntervalRecent { get; set; }

        /// <summary>
        /// Time interval during which to establish baseline performance for the report
        /// </summary>
        public BasicTimeInterval TimeIntervalHistory { get; set; }

        /// <summary>
        /// Minimum number of executions for a query to be included
        /// </summary>
        public long MinExecutionCount { get; set; }

        public override RegressedQueriesConfiguration Convert()
        {
            RegressedQueriesConfiguration result = base.Convert();

            result.TimeIntervalRecent = TimeIntervalRecent.Convert();
            result.TimeIntervalHistory = TimeIntervalHistory.Convert();
            result.MinExecutionCount = MinExecutionCount;

            return result;
        }
    }

    /// <summary>
    /// Gets the Regressed Queries summary
    /// </summary>
    public class GetRegressedQueriesSummaryRequest
    {
        public static readonly RequestType<GetRegressedQueriesReportParams, QueryStoreQueryResult> Type
            = RequestType<GetRegressedQueriesReportParams, QueryStoreQueryResult>.Create("queryStore/getRegressedQueriesSummary");
    }

    /// <summary>
    /// Gets the Regressed Queries summary
    /// </summary>
    public class GetRegressedQueriesDetailedSummaryRequest
    {
        public static readonly RequestType<GetRegressedQueriesReportParams, QueryStoreQueryResult> Type
            = RequestType<GetRegressedQueriesReportParams, QueryStoreQueryResult>.Create("queryStore/getRegressedQueriesDetailedSummary");
    }
}
