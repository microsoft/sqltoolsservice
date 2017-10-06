//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ResourceProvider.Core.Contracts
{
    /// <summary>
    /// A request to open up a firewall rule
    /// </summary>
    public class CreateFirewallRuleRequest
    {
        public static readonly
            RequestType<CreateFirewallRuleParams, CreateFirewallRuleResponse> Type =
            RequestType<CreateFirewallRuleParams, CreateFirewallRuleResponse>.Create("resource/createFirewallRule");
    }

    /// <summary>
    /// A FirewallRule object, usable in <see cref="CreateFirewallRuleRequest"/>s and other messages
    /// </summary>
    public class CreateFirewallRuleParams
    {
        /// <summary>
        /// Account information to use in connecting to Azure
        /// </summary>
        public Account Account { get; set; }
        /// <summary>
        /// Per-tenant oken mappings. Ideally would be set independently of this call, but for 
        /// now this allows us to handle this
        /// </summary>
        public Dictionary<string,string> SecurityTokenMappings { get; set; }

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

    public class CreateFirewallRuleResponse
    {
        public bool Result { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CanHandleFirewallRuleRequest
    {
        public static readonly
            RequestType<HandleFirewallRuleParams, HandleFirewallRuleResponse> Type = 
            RequestType<HandleFirewallRuleParams, HandleFirewallRuleResponse>.Create("resource/handleFirewallRule");
    }

    public class HandleFirewallRuleParams
    {
        /// <summary>
        /// The error code used to defined the error type
        /// </summary>
        public int ErrorCode { get; set; }
        /// <summary>
        /// The error message from which to parse the IP address
        /// </summary>
        public string ErrorMessage { get; set; }
        /// <summary>
        /// The connection type, for example MSSQL
        /// </summary>
        public string ConnectionTypeId { get; set; }
    }
    /// <summary>
    /// Response to the check for Firewall rule support given an error message
    /// </summary>
    public class HandleFirewallRuleResponse
    {
        /// <summary>
        /// Can this be handled?
        /// </summary>
        public bool Result { get; set; }
        /// <summary>
        /// If not, why?
        /// </summary>
        public string ErrorMessage { get; set; }
        /// <summary>
        /// If it can be handled, is there a default IP address to send back so users
        /// can tell what their blocked IP is?
        /// </summary>
        public string IpAddress { get; set; }
    }


}
