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
    public class AgentAlertsParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// SQL Agent Job activity result
    /// </summary>
    public class AgentAlertsResult
    {

        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }

        public AgentAlertInfo[] Alerts { get; set; }
    }

    /// <summary>
    /// SQL Agent Alerts request type
    /// </summary>
    public class AgentAlertsRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentAlertsParams, AgentAlertsResult> Type =
            RequestType<AgentAlertsParams, AgentAlertsResult>.Create("agent/alerts");
    }
}
