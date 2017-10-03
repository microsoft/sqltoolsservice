//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;

namespace Microsoft.SqlTools.Azure.Core.FirewallRule
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
        public string FirewallRuleName
        {
            get
            {
                DateTime now = DateTime.UtcNow;

                return string.Format(CultureInfo.InvariantCulture, "ClientIPAddress_{0}",
                    now.ToString("yyyy-MM-dd_hh:mm:ss", CultureInfo.CurrentCulture));
            }
        }
    }
}
