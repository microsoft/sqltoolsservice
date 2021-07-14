//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts
{
    /// <summary>
    /// Parameters for executing a query from a provided string
    /// </summary>
    public class GetAssessmentItemsParams : AssessmentParams
    {
        // a placeholder for future specialization
    }

    /// <summary>
    /// Describes a check used to assess SQL Server objects.
    /// </summary>
    public class CheckInfo : AssessmentItemInfo
    {
        // a placeholder for future specialization
    }


    public class GetAssessmentItemsRequest
    {
        public static readonly RequestType<GetAssessmentItemsParams, AssessmentResult<CheckInfo>> Type =
            RequestType<GetAssessmentItemsParams, AssessmentResult<CheckInfo>>.Create(
                "assessment/getAssessmentItems");
    }
}
