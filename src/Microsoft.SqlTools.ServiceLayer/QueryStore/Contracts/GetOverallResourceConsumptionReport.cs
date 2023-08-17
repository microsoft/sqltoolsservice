//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlServer.Management.QueryStoreModel.OverallResourceConsumption;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts
{
    public class GetOverallResourceConsumptionReportParams : QueryConfigurationParams<OverallResourceConsumptionConfiguration>
    {
        public TimeInterval SpecifiedTimeInterval { get; set; }
        public BucketInterval SpecifiedBucketInterval { get; set; }

        public override OverallResourceConsumptionConfiguration Convert()
        {
            OverallResourceConsumptionConfiguration result = base.Convert();

            result.SpecifiedTimeInterval = SpecifiedTimeInterval;
            result.SelectedBucketInterval = SpecifiedBucketInterval;

            return result;
        }
    }

    /// <summary>
    /// Gets the report for a Forced Plan Queries summary
    /// </summary>
    public class GetOverallResourceConsumptionReportRequest
    {
        public static readonly RequestType<GetOverallResourceConsumptionReportParams, QueryStoreQueryResult> Type
            = RequestType<GetOverallResourceConsumptionReportParams, QueryStoreQueryResult>.Create("queryStore/getOverallResourceConsumptionReport");
    }
}
