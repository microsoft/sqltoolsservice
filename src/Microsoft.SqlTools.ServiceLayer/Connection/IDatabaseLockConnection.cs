//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    public interface IDatabaseLockConnection
    {
        /// <summary>
        /// Returns true if the lock on database can temporary be released
        /// </summary>
        bool CanTemporaryClose { get; }

        /// <summary>
        /// Disconnect the connection and has lock on the database
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Opens the connection again if not already connected
        /// </summary>
        void Connect();

        /// <summary>
        /// Returns true if the connection is open
        /// </summary>
        bool IsConnctionOpen { get; }
    }
}
