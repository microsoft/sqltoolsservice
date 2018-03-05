//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Agent.Contracts
{

    public class AgentJobHistoryParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string JobId { get; set; }
    }

    /// <summary>
    /// SQL Agent Job activity result
    /// </summary>
    public class AgentJobHistoryResult
    {

        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }

        public AgentJobHistoryInfo[] Jobs { get; set; }
    }

    /// <summary>
    /// SQL Agent Jobs request type
    /// </summary>
    public class AgentJobHistoryRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentJobsParams, AgentJobHistoryResult> Type =
            RequestType<AgentJobsParams, AgentJobHistoryResult>.Create("agent/jobHistory");
    }
}
