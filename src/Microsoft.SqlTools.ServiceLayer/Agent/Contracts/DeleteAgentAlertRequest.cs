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
    /// SQL Agent delete Alert params
    /// </summary>
    public class DeleteAgentAlertParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentAlertInfo Alert { get; set; }
    }

    /// <summary>
    /// SQL Agent delete Alert result
    /// </summary>
    public class DeleteAgentAlertResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent delete Alert request type
    /// </summary>
    public class DeleteAgentAlertRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DeleteAgentAlertParams, DeleteAgentAlertResult> Type =
            RequestType<DeleteAgentAlertParams, DeleteAgentAlertResult>.Create("agent/deletealert");
    }
}
