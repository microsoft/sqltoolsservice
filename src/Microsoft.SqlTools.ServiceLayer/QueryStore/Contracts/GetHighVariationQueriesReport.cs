//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    public class GetHighVariationQueriesReportParams
    {

    }

    /// <summary>
    /// Gets the report for a Forced Plan Queries summary
    /// </summary>
    public class GetHighVariationQueriesReportRequest
    {
        public static readonly RequestType<GetHighVariationQueriesReportParams, GetHighVariationQueriesReportResult> Type
            = RequestType<GetHighVariationQueriesReportParams, GetHighVariationQueriesReportResult>.Create("queryStore/getHighVariationQueriesReport");
    }

    /// <summary>
    /// Result containing the report for a Forced Plan Queries summary
    /// </summary>
    public class GetHighVariationQueriesReportResult : ResultStatus
    {

    }
}
