//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    public class GetRegressedQueriesReportParams
    {

    }

    /// <summary>
    /// Gets the report for a Forced Plan Queries summary
    /// </summary>
    public class GetRegressedQueriesReportRequest
    {
        public static readonly RequestType<GetRegressedQueriesReportParams, GetRegressedQueriesReportResult> Type
            = RequestType<GetRegressedQueriesReportParams, GetRegressedQueriesReportResult>.Create("queryStore/getRegressedQueriesReport");
    }

    /// <summary>
    /// Result containing the report for a Forced Plan Queries summary
    /// </summary>
    public class GetRegressedQueriesReportResult : ResultStatus
    {

    }
}
