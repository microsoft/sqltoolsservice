//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.Agent.Contracts
{
    /// <summary>
    /// SQL Agent Schedules parameters
    /// </summary>
    public class AgentSchedulesParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// SQL Agent Schedules result
    /// </summary>
    public class AgentSchedulesResult
    {
        public bool Success { get; set; }

        public string ErrorMessage { get; set; }

        public AgentScheduleInfo[] Schedules { get; set; }
    }

    /// <summary>
    /// SQL Agent Schedules request type
    /// </summary>
    public class AgentSchedulesRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentSchedulesParams, AgentSchedulesResult> Type =
            RequestType<AgentSchedulesParams, AgentSchedulesResult>.Create("agent/schedules");
    }

    /// <summary>
    /// SQL Agent Schedule result
    /// </summary>
    public class AgentScheduleResult : ResultStatus
    {
        public AgentScheduleInfo Schedule { get; set; }
    }    


    /// <summary>
    /// SQL Agent create Schedules params
    /// </summary>
    public class CreateAgentScheduleParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentScheduleInfo Schedule { get; set; }
    }

    /// <summary>
    /// SQL Agent create Schedule request type
    /// </summary>
    public class CreateAgentScheduleRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateAgentScheduleParams, AgentScheduleResult> Type =
            RequestType<CreateAgentScheduleParams, AgentScheduleResult>.Create("agent/createschedule");
    }

    /// <summary>
    /// SQL Agent update Schedule params
    /// </summary>
    public class UpdateAgentScheduleParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string OriginalScheduleName { get; set; }

        public AgentScheduleInfo Schedule { get; set; }
    }

    /// <summary>
    /// SQL Agent update Schedule request type
    /// </summary>
    public class UpdateAgentScheduleRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<UpdateAgentScheduleParams, AgentScheduleResult> Type =
            RequestType<UpdateAgentScheduleParams, AgentScheduleResult>.Create("agent/updateschedule");
    }    

    /// <summary>
    /// SQL Agent delete Schedule params
    /// </summary>
    public class DeleteAgentScheduleParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentScheduleInfo Schedule { get; set; }
    }

    /// <summary>
    /// SQL Agent delete Schedule request type
    /// </summary>
    public class DeleteAgentScheduleRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DeleteAgentScheduleParams, ResultStatus> Type =
            RequestType<DeleteAgentScheduleParams, ResultStatus>.Create("agent/deleteschedule");
    }    
}
