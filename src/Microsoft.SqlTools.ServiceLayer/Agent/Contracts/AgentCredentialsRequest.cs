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
    /// SQL Agent Credentials parameters
    /// </summary>
    public class AgentCredentialsParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// SQL Agent Credential class
    /// </summary>
    public class AgentCredential {

        public int CredentialID { get; set; }

        public string CredentialIdentity { get; set; }

        public string CredentialName { get; set ;}

    }

    /// <summary>
    /// SQL Agent Credentials result
    /// </summary>
    public class AgentCredentialsResult : ResultStatus
    {
        public AgentCredential[] Credentials { get; set; }
    }

    /// <summary>
    /// SQL Agent Credentials request type
    /// </summary>
    public class AgentCredentialsRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentCredentialsParams, AgentCredentialsResult> Type =
            RequestType<AgentCredentialsParams, AgentCredentialsResult>.Create("agent/credentials");
    }
}
