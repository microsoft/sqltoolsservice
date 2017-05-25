//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Internal for testing purposes only. This class provides wrapper functionality
    /// over SMO objects in order to facilitate unit testing
    /// </summary>
    internal class SmoWrapper
    {
        public virtual Server Create(SqlConnection connection)
        {
            ServerConnection serverConn = new ServerConnection(connection);
            return new Server(serverConn);
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