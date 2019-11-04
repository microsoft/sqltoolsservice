//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.SqlTools.CoreServices.Connection.ReliableConnection;

public static class ConnectionUtils
{
    public static SqlConnection GetAsSqlConnection(DbConnection connection)
    {
        SqlConnection sqlConn = connection as SqlConnection;
        if (sqlConn == null)
        {
            // It's not actually a SqlConnection, so let's try a reliable SQL connection
            ReliableSqlConnection reliableConn = connection as ReliableSqlConnection;
            if (reliableConn == null)
            {
                // If we don't have connection we can use with SMO, just give up on using SMO
                return null;
            }

            // We have a reliable connection, use the underlying connection
            sqlConn = reliableConn.GetUnderlyingConnection();
        }
        return sqlConn;
    }
}