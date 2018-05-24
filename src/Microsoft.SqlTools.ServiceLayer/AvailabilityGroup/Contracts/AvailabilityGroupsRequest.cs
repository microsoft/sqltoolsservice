//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.AvailabilityGroup.Contracts
{
    /// <summary>
    /// Availability group request parameters
    /// </summary>
    public class AvailabilityGroupsRequestParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Availability groups result
    /// </summary>
    public class AvailabilityGroupsResult
    {

        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }

        public AvailabilityGroupInfo[] AvailabilityGroups { get; set; }
    }

    /// <summary>
    /// Availability groups request type
    /// </summary>
    public class AvailabilityGroupsRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AvailabilityGroupsRequestParams, AvailabilityGroupsResult> Type =
            RequestType<AvailabilityGroupsRequestParams, AvailabilityGroupsResult>.Create("hadr/availabilitygroups");
    }
}
