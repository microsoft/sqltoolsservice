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
    /// SQL Agent Operators request parameters
    /// </summary>
    public class AgentOperatorsParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// SQL Agent Operators request result
    /// </summary>
    public class AgentOperatorsResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }

        public AgentOperatorInfo[] Operators { get; set; }
    }

    /// <summary>
    /// SQL Agent Operators request type
    /// </summary>
    public class AgentOperatorsRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentOperatorsParams, AgentOperatorsResult> Type =
            RequestType<AgentOperatorsParams, AgentOperatorsResult>.Create("agent/operators");
    }

    /// <summary>
    /// SQL Agent create Operator params
    /// </summary>
    public class CreateAgentOperatorParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentOperatorInfo Operator { get; set; }
    }

    /// <summary>
    /// SQL Agent create Operator result
    /// </summary>
    public class CreateAgentOperatorResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent create Operator request type
    /// </summary>
    public class CreateAgentOperatorRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateAgentOperatorParams, CreateAgentOperatorResult> Type =
            RequestType<CreateAgentOperatorParams, CreateAgentOperatorResult>.Create("agent/createoperator");
    }

    /// <summary>
    /// SQL Agent delete Operator params
    /// </summary>
    public class DeleteAgentOperatorParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentOperatorInfo Operator { get; set; }
    }

    /// <summary>
    /// SQL Agent delete Operator result
    /// </summary>
    public class DeleteAgentOperatorResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent delete Operator request type
    /// </summary>
    public class DeleteAgentOperatorRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DeleteAgentOperatorParams, DeleteAgentOperatorResult> Type =
            RequestType<DeleteAgentOperatorParams, DeleteAgentOperatorResult>.Create("agent/deleteoperator");
    }

    /// <summary>
    /// SQL Agent update Operator params
    /// </summary>
    public class UpdateAgentOperatorParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentOperatorInfo Operator { get; set; }
    }

    /// <summary>
    /// SQL Agent update Operator result
    /// </summary>
    public class UpdateAgentOperatorResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent update Operator request type
    /// </summary>
    public class UpdateAgentOperatorRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<UpdateAgentOperatorParams, UpdateAgentOperatorResult> Type =
            RequestType<UpdateAgentOperatorParams, UpdateAgentOperatorResult>.Create("agent/updateoperator");
    }
}
