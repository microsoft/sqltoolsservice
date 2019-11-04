//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Kusto;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.KustoModel
{
    /// <summary>
    /// Internal for testing purposes only. This class provides wrapper functionality
    /// over SMO objects in order to facilitate unit testing
    /// </summary>
    internal class KustoWrapper
    {
        public virtual Server CreateServer(ServerConnection serverConn)
        {
            return serverConn == null ? null : new Server(serverConn);
        }

        public virtual bool IsConnectionOpen(KustoObjectBase smoObj)
        {
            SqlKustoObject sqlObj = smoObj as SqlKustoObject;
            return sqlObj != null
                && sqlObj.ExecutionManager != null
                && sqlObj.ExecutionManager.ConnectionContext != null
                && sqlObj.ExecutionManager.ConnectionContext.IsOpen;
        }

        public virtual void OpenConnection(KustoObjectBase smoObj)
        {
            SqlKustoObject sqlObj = smoObj as SqlKustoObject;
            if (sqlObj != null
                && sqlObj.ExecutionManager != null
                && sqlObj.ExecutionManager.ConnectionContext != null)
            {
                sqlObj.ExecutionManager.ConnectionContext.Connect();
            }
        }
    }
}