//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Agent.Contracts
{
    /// <summary>
    /// SQL Agent Job activity parameters
    /// </summary>
    public class AgentJobsParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// SQL Agent Job activity result
    /// </summary>
    public class AgentJobsResult
    {

        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }

        public AgentJobInfo[] Jobs { get; set; }
    }

    /// <summary>
    /// SQL Agent Jobs request type
    /// </summary>
    public class AgentJobsRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentJobsParams, AgentJobsResult> Type =
            RequestType<AgentJobsParams, AgentJobsResult>.Create("agent/jobs");
    }
}
