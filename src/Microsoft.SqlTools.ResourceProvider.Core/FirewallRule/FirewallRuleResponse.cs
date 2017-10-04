//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.ResourceProvider.Core.FirewallRule
{
    /// <summary>
    /// The response that's created when the firewall rule creation request is complete
    /// </summary>
    public class FirewallRuleResponse
    {
        /// <summary>
        /// End IP address
        /// </summary>
        public string EndIpAddress
        {
            get;
            set;
        }

        /// <summary>
        /// Start IP address
        /// </summary>
        public string StartIpAddress
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the firewall rule created successfully or not
        /// </summary>
        public bool Created { get; set; }
    }
}
