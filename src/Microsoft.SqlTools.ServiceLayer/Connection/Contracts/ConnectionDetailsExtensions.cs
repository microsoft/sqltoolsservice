//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Extension methods for the ConnectionDetails contract class
    /// </summary>
    public static class ConnectionDetailsExtensions
    {
        /// <summary>
        /// Create a copy of a connection details object.
        /// </summary>
        public static ConnectionDetails Clone(this ConnectionDetails details)
        {
            return new ConnectionDetails()
            {
                ServerName = details.ServerName,
                DatabaseName = details.DatabaseName,
                UserName = details.UserName,
                Password = details.Password
            };
        }
    }
}
