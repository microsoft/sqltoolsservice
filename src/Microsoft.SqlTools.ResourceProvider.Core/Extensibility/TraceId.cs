//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ResourceProvider.Core.Extensibility
{
    /// <summary>
    /// Enumeration of values used as trace event identifiers that semantically represent the major categories of product features.
    /// </summary>
    public enum TraceId : int
    {
        /// <summary>
        /// Trace Id for Azure Authentication traces
        /// </summary>
        AzureAuthentication = 0,
        /// <summary>
        /// Trace Id for azure resource traces
        /// </summary>
        AzureResource = 1,
        /// <summary>
        /// Trace Id for UI sections traces
        /// </summary>
        Sections = 2,

        /// <summary>
        /// Trace Id for connection traces
        /// </summary>
        Connection = 3,

        /// <summary>
        /// Trace Id for firewall rule traces
        /// </summary>
        FirewallRule = 4,

        /// <summary>
        /// Trace Id for Azure browse traces
        /// </summary>
        AzureSection = 5,

        /// <summary>
        /// Trace Id for network servers
        /// </summary>
        NetworkServers = 6,

        /// <summary>
        /// Trace Id for local servers
        /// </summary>
        LocalServers = 7,

        /// <summary>
        /// Trace Id for sql database discovery
        /// </summary>
        SqlDatabase = 8,

        /// <summary>
        /// Trace Id for local browse traces
        /// </summary>
        LocalSection = 9,

        /// <summary>
        /// Trace Id for account picker traces
        /// </summary>
        AccountPicker = 10,

        /// <summary>
        /// Trace Id for network browse traces
        /// </summary>
        NetworkSection = 11, 

        /// <summary>
        /// Trace Id for main dialog traces
        /// </summary>
        UiInfra = 12,

        /// <summary>
        /// Trace Id for hostory page traces
        /// </summary>
        HistoryPage = 13,

        /// <summary>
        /// TraceId for Telemetry
        /// </summary>
        Telemetry = 14,
    }
}
