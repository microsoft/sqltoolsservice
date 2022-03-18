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
        public virtual Server CreateServer(ServerConnection serverConn)
        {
            return serverConn == null ? null : new Server(serverConn);
        }

        public virtual bool IsConnectionOpen(SmoObjectBase smoObj)
        {
            SqlSmoObject sqlObj = smoObj as SqlSmoObject;
            return sqlObj != null
                && sqlObj.ExecutionManager != null
                && sqlObj.ExecutionManager.ConnectionContext != null
                && sqlObj.ExecutionManager.ConnectionContext.IsOpen;
        }

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