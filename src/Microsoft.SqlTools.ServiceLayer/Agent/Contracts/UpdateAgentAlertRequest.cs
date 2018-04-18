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
    /// SQL Agent update Alert params
    /// </summary>
    public class UpdateAgentAlertParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentAlertInfo Alert { get; set; }
    }

    /// <summary>
    /// SQL Agent update Alert result
    /// </summary>
    public class UpdateAgentAlertResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent update Alert request type
    /// </summary>
    public class UpdateAgentAlertRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<UpdateAgentAlertParams, UpdateAgentAlertResult> Type =
            RequestType<UpdateAgentAlertParams, UpdateAgentAlertResult>.Create("agent/updatealert");
    }
}
