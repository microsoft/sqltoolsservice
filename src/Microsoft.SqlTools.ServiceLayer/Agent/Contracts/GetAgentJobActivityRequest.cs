//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// SQL Agent Job activity parameters
    /// </summary>
    public class GetAgentJobActivityParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// SQL Agent Job activity result
    /// </summary>
    public class GetAgentJobActivityResult
    {

        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent Job activity request type
    /// </summary>
    public class GetAgentJobActivityRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<GetAgentJobActivityParams, GetAgentJobActivityResult> Type =
            RequestType<GetAgentJobActivityParams, GetAgentJobActivityResult>.Create("agent/activity");
    }
}
