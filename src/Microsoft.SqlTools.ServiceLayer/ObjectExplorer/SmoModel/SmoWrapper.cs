//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Internal for testing purposes only. This class provides wrapper functionality
    /// over SMO objects in order to facilitate unit testing
    /// </summary>
    internal class SmoWrapper
    {
        /// <summary>
        /// Creates instance of <see cref="Server"/> from provided <paramref name="serverConn"/> instance.
        /// </summary>
        /// <param name="serverConn">Server connection instance.</param>
        /// <returns>Server instance.</returns>
        public virtual Server CreateServer(ServerConnection serverConn)
        {
            return serverConn == null ? null : new Server(serverConn);
        }

        /// <summary>
        /// Checks if connection is open on the <paramref name="smoObj"/> instance.
        /// </summary>
        /// <param name="smoObj">SMO Object containing connection context.</param>
        /// <returns>True if connection is open, otherwise false.</returns>
        public virtual bool IsConnectionOpen(SmoObjectBase smoObj)
        {
            SqlSmoObject sqlObj = smoObj as SqlSmoObject;
            return sqlObj != null
                && sqlObj.ExecutionManager != null
                && sqlObj.ExecutionManager.ConnectionContext != null
                && sqlObj.ExecutionManager.ConnectionContext.IsOpen;
        }

        /// <summary>
        /// Opens connection on the connection context of <paramref name="smoObj"/> instance.
        /// </summary>
        /// <param name="smoObj">SMO Object containing connection context.</param>
        public virtual void OpenConnection(SmoObjectBase smoObj)
        {
            SqlSmoObject sqlObj = smoObj as SqlSmoObject;
            if (sqlObj != null
                && sqlObj.ExecutionManager != null
                && sqlObj.ExecutionManager.ConnectionContext != null)
            {
                sqlObj.ExecutionManager.ConnectionContext.Connect();
            }
        }
    }
}