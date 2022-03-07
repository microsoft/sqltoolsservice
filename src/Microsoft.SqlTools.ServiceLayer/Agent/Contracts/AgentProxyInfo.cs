//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Agent.Contracts
{
    /// <summary>
    /// a class for storing various properties of agent proxy accounts
    /// </summary>
    public class AgentProxyInfo
    {
        public int Id { get; set; }
        public string AccountName { get; set; }
        public string Description { get; set; }
        public string CredentialName { get; set; }
        public string CredentialIdentity { get; set; }
        public int CredentialId { get; set; }
        public bool IsEnabled { get; set; }
    }
}
