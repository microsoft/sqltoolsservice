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
    public class AgentAlertsResult : ResultStatus
    {
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

    /// <summary>
    /// SQL Agent create Alert params
    /// </summary>
    public class CreateAgentAlertParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentAlertInfo Alert { get; set; }
    }

    /// <summary>
    /// SQL Agent create Alert result
    /// </summary>
    public class CreateAgentAlertResult : ResultStatus
    {
    }

    /// <summary>
    /// SQL Agent create Alert request type
    /// </summary>
    public class CreateAgentAlertRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateAgentAlertParams, CreateAgentAlertResult> Type =
            RequestType<CreateAgentAlertParams, CreateAgentAlertResult>.Create("agent/createalert");
    }

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
    public class DeleteAgentAlertResult : ResultStatus
    {        
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
    public class UpdateAgentAlertResult : ResultStatus
    {
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
