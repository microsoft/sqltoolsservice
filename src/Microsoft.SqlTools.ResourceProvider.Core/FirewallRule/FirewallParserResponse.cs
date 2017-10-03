//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Microsoft.SqlTools.ResourceProvider.Core.FirewallRule
{
    /// <summary>
    /// The response that's created by firewall rule parser
    /// </summary>
    public class FirewallParserResponse
    {
        public FirewallParserResponse(bool firewallRuleErrorDetected, IPAddress blockedIpAddress)
        {
            FirewallRuleErrorDetected = firewallRuleErrorDetected;
            BlockedIpAddress = blockedIpAddress;
        }

        public FirewallParserResponse()
        {
            FirewallRuleErrorDetected = false;
        }

        /// <summary>
        /// Returns true if firewall rule is detected, otherwise returns false.
        /// </summary>
        public bool FirewallRuleErrorDetected
        {
            get; private set;
        }

        /// <summary>
        /// Returns the blocked ip address if firewall rule is detected
        /// </summary>
        public IPAddress BlockedIpAddress
        {
            get; private set;
        }
    }
}
