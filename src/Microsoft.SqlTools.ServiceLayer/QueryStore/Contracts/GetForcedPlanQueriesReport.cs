//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    public class GetForcedPlanQueriesReportParams : QueryStoreReportParams
    {
        
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
