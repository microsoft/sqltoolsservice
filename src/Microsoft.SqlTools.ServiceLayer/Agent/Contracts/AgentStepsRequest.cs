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
    /// SQL Agent Job Steps parameters
    /// </summary>
    public class AgentStepsParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// SQL Agent Job Steps result
    /// </summary>
    public class AgentStepsResult
    {

        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }

        public AgentStepInfo[] Steps { get; set; }
    }

    /// <summary>
    /// SQL Agent Steps request type
    /// </summary>
    public class AgentStepsRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentStepsParams, AgentStepsResult> Type =
            RequestType<AgentStepsParams, AgentStepsResult>.Create("agent/steps");
    }

    /// <summary>
    /// SQL Agent create Step params
    /// </summary>
    public class CreateAgentStepParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentStepInfo Step { get; set; }
    }

    /// <summary>
    /// SQL Agent create Step result
    /// </summary>
    public class CreateAgentStepResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent create Step request type
    /// </summary>
    public class CreateAgentStepRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateAgentStepParams, CreateAgentStepResult> Type =
            RequestType<CreateAgentStepParams, CreateAgentStepResult>.Create("agent/createstep");
    }

    /// <summary>
    /// SQL Agent delete Step params
    /// </summary>
    public class DeleteAgentStepParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentStepInfo Step { get; set; }
    }

    /// <summary>
    /// SQL Agent delete Step result
    /// </summary>
    public class DeleteAgentStepResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent delete Step request type
    /// </summary>
    public class DeleteAgentStepRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DeleteAgentStepParams, DeleteAgentStepResult> Type =
            RequestType<DeleteAgentStepParams, DeleteAgentStepResult>.Create("agent/deletestep");
    }

    /// <summary>
    /// SQL Agent update Step params
    /// </summary>
    public class UpdateAgentStepParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentStepInfo Step { get; set; }
    }

    /// <summary>
    /// SQL Agent update Step result
    /// </summary>
    public class UpdateAgentStepResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent update Step request type
    /// </summary>
    public class UpdateAgentStepRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<UpdateAgentStepParams, UpdateAgentStepResult> Type =
            RequestType<UpdateAgentStepParams, UpdateAgentStepResult>.Create("agent/updatestep");
    }
}
