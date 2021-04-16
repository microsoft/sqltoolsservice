//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.Connection
{
    /// <summary>
    /// Parameters for the ConnectionChanged Notification.
    /// </summary>
    public class ConnectionChangedParams
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.         
        /// </summary>
        public string OwnerUri { get; set; }
        /// <summary>
        /// Contains the high-level properties about the connection, for display to the user.
        /// </summary>
        public ConnectionSummary Connection { get; set; }
    }
}
