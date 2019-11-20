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
    /// SQL Agent Job Steps parameters
    /// </summary>
    public class AgentJobStepsParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// SQL Agent Job Steps result
    /// </summary>
    public class AgentJobStepsResult : ResultStatus
    {
        public AgentJobStepInfo[] Steps { get; set; }
    }

    /// <summary>
    /// SQL Agent Steps request type
    /// </summary>
    public class AgentJobStepsRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentJobStepsParams, AgentJobStepsResult> Type =
            RequestType<AgentJobStepsParams, AgentJobStepsResult>.Create("agent/jobsteps");
    }

    /// <summary>
    /// SQL Agent create Step params
    /// </summary>
    public class CreateAgentJobStepParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string OriginalJobStepName { get; set; }

        public AgentJobStepInfo Step { get; set; }
    }

    /// <summary>
    /// SQL Agent create Step result
    /// </summary>
    public class CreateAgentJobStepResult : ResultStatus
    {
        public AgentJobStepInfo Step { get; set; }
    }

    /// <summary>
    /// SQL Agent create Step request type
    /// </summary>
    public class CreateAgentJobStepRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateAgentJobStepParams, CreateAgentJobStepResult> Type =
            RequestType<CreateAgentJobStepParams, CreateAgentJobStepResult>.Create("agent/createjobstep");
    }

    /// <summary>
    /// SQL Agent delete Step params
    /// </summary>
    public class DeleteAgentJobStepParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentJobStepInfo Step { get; set; }
    }

    /// <summary>
    /// SQL Agent delete Step request type
    /// </summary>
    public class DeleteAgentJobStepRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DeleteAgentJobStepParams, ResultStatus> Type =
            RequestType<DeleteAgentJobStepParams, ResultStatus>.Create("agent/deletejobstep");
    }

    /// <summary>
    /// SQL Agent update Step params
    /// </summary>
    public class UpdateAgentJobStepParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentJobStepInfo Step { get; set; }
    }

    /// <summary>
    /// SQL Agent update Step result
    /// </summary>
    public class UpdateAgentJobStepResult : ResultStatus
    {
        public AgentJobStepInfo Step { get; set; }
    }

    /// <summary>
    /// SQL Agent update Step request type
    /// </summary>
    public class UpdateAgentJobStepRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<UpdateAgentJobStepParams, UpdateAgentJobStepResult> Type =
            RequestType<UpdateAgentJobStepParams, UpdateAgentJobStepResult>.Create("agent/updatejobstep");
    }
}
