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
        /// <summary>
        /// Gets or sets the owner Uri
        /// </summary>        
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Availability groups result
    /// </summary>
    public class AvailabilityGroupsResult
    {
        /// <summary>
        /// Gets or sets a boolean value indicating whether the processing of request succeeded
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the availability groups
        /// </summary>
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
