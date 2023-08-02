//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    public class GetRegressedQueriesReportParams : QueryStoreReportParams
    {

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
