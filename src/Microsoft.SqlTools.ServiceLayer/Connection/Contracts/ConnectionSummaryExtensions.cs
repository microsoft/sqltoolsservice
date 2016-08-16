//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{   
    /// <summary>
    /// Extension methods to ConnectionSummary
    /// </summary>
    public static class ConnectionSummaryExtensions
    {
        /// <summary>
        /// Create a copy of a ConnectionSummary object
        /// </summary>
        public static ConnectionSummary Clone(this ConnectionSummary summary)
        {
            return new ConnectionSummary()
            {
                ServerName = summary.ServerName,
                DatabaseName = summary.DatabaseName,
                UserName = summary.UserName
            };
        }
    }
}
