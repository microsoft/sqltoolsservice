﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.Net;

namespace Microsoft.SqlTools.ResourceProvider.Core.Firewall
{
    /// <summary>
    /// Includes all the information needed to create a firewall rule
    /// </summary>
    public class FirewallRuleRequest
    {
        /// <summary>
        /// Start IP address
        /// </summary>
        public IPAddress StartIpAddress { get; set; }

        /// <summary>
        /// End IP address
        /// </summary>
        public IPAddress EndIpAddress { get; set; }

        /// <summary>
        /// Firewall rule name
        /// </summary>
        public string FirewallRuleName { get; set; }
    }
}
