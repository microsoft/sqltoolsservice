//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    public class GetForcedPlanQueriesReportParams
    {

    }

    /// <summary>
    /// Gets the report for a Forced Plan Queries summary
    /// </summary>
    public class GetForcedPlanQueriesReportRequest
    {
        public static readonly RequestType<GetForcedPlanQueriesReportParams, GetForcedPlanQueriesReportResult> Type
            = RequestType<GetForcedPlanQueriesReportParams, GetForcedPlanQueriesReportResult>.Create("queryStore/getForcedPlanQueriesReport");
    }

    /// <summary>
    /// Result containing the report for a Forced Plan Queries summary
    /// </summary>
    public class GetForcedPlanQueriesReportResult : ResultStatus
    {

    }
}
