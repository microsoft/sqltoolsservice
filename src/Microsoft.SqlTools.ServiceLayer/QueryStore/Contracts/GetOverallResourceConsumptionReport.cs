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
    /// <summary>
    /// Parameters for getting an Overall Resource Consumption report
    /// </summary>
    public class GetOverallResourceConsumptionReportParams : QueryConfigurationParams<OverallResourceConsumptionConfiguration>
    {
        /// <summary>
        /// Time interval for the report
        /// </summary>
        public BasicTimeInterval SpecifiedTimeInterval { get; set; }

        /// <summary>
        /// Bucket interval for the report
        /// </summary>
        public BucketInterval SpecifiedBucketInterval { get; set; }

        public override OverallResourceConsumptionConfiguration Convert()
        {
            OverallResourceConsumptionConfiguration result = base.Convert();

            result.SpecifiedTimeInterval = SpecifiedTimeInterval.Convert();
            result.SelectedBucketInterval = SpecifiedBucketInterval;

            return result;
        }
    }

    /// <summary>
    /// Gets the query for an Overall Resource Consumption report
    /// </summary>
    public class GetOverallResourceConsumptionReportRequest
    {
        public static readonly RequestType<GetOverallResourceConsumptionReportParams, QueryStoreQueryResult> Type
            = RequestType<GetOverallResourceConsumptionReportParams, QueryStoreQueryResult>.Create("queryStore/getOverallResourceConsumptionReport");
    }
}
