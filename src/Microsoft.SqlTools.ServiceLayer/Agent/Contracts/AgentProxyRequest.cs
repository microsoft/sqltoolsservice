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
    /// SQL Agent proxy accounts parameters
    /// </summary>
    public class AgentProxiesParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// SQL Agent proxy accounts result
    /// </summary>
    public class AgentProxiesResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }

        public AgentProxyInfo[] Proxies { get; set; }
    }

    /// <summary>
    /// SQL Agent Proxy Accounts request type
    /// </summary>
    public class AgentProxiesRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentProxiesParams, AgentProxiesResult> Type =
            RequestType<AgentProxiesParams, AgentProxiesResult>.Create("agent/proxies");
    }

    /// <summary>
    /// SQL Agent create Proxy Account params
    /// </summary>
    public class CreateAgentProxyParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentProxyInfo Proxy { get; set; }
    }

    /// <summary>
    /// SQL Agent create Proxy result
    /// </summary>
    public class CreateAgentProxyResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent create Proxy request type
    /// </summary>
    public class CreateAgentProxyRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateAgentProxyParams, CreateAgentProxyResult> Type =
            RequestType<CreateAgentProxyParams, CreateAgentProxyResult>.Create("agent/createproxy");
    }

    /// <summary>
    /// SQL Agent delete Proxy params
    /// </summary>
    public class DeleteAgentProxyParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentProxyInfo Proxy { get; set; }
    }

    /// <summary>
    /// SQL Agent delete Proxy result
    /// </summary>
    public class DeleteAgentProxyResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent delete Proxy request type
    /// </summary>
    public class DeleteAgentProxyRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DeleteAgentProxyParams, DeleteAgentProxyResult> Type =
            RequestType<DeleteAgentProxyParams, DeleteAgentProxyResult>.Create("agent/deleteproxy");
    }

    /// <summary>
    /// SQL Agent update Proxy params
    /// </summary>
    public class UpdateAgentProxyParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string OriginalProxyName { get; set; }

        public AgentProxyInfo Proxy { get; set; }
    }

    /// <summary>
    /// SQL Agent update Proxy result
    /// </summary>
    public class UpdateAgentProxyResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent update Proxy request type
    /// </summary>
    public class UpdateAgentProxyRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<UpdateAgentProxyParams, UpdateAgentProxyResult> Type =
            RequestType<UpdateAgentProxyParams, UpdateAgentProxyResult>.Create("agent/updateproxy");
    }
}
