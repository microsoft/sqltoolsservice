//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ResourceProvider.Core.Contracts
{
    /// <summary>
    /// A formatting request to process an entire document
    /// </summary>
    public class CreateFirewallRuleRequest
    {
        public static readonly
            RequestType<FirewallRule, bool> Type =
            RequestType<FirewallRule, bool>.Create("resourceProvider/createFirewallRule");
    }
    
    /// <summary>
    /// A FirewallRule object, usable in <see cref="CreateFirewallRuleRequest"/>s and other messages
    /// </summary>
    public class FirewallRule
    {
        /// <summary>
        /// Fully qualified name of the server to create a new firewall rule on
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Start of the IP address range
        /// </summary>
        public string StartIpAddressValue { get; set; }

        /// <summary>
        /// End of the IP address range
        /// </summary>
        public string EndIpAddressValue { get; set; }
  
    }
}
