﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Management.Common;
using System;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
    public class ConnectSqlCmdCommand : SqlCmdCommand
    {
        internal ConnectSqlCmdCommand(string server, string username, string password) : base(LexerTokenType.Connect)
        {
            Server = server;
            UserName = username;
            Password = password;
        }

        public string Server { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }


        /// <summary>
        /// attempts to establish connection with given params
        /// </summary>
        /// <returns>returns the connection object is successful, throws otherwise</returns>
        public DbConnection Connect()
        {
            //create SqlConnectionInfo object
            SqlConnectionInfo connectionInfo = new SqlConnectionInfo();
            if (Server != null && Server.Length > 0)
            {
                connectionInfo.ServerName = Server;
            }
            if (UserName != null && UserName.Length > 0)
            {
                connectionInfo.UseIntegratedSecurity = false;
                connectionInfo.UserName = UserName;
                connectionInfo.Password = Password;
            }
            else
            {
                connectionInfo.UseIntegratedSecurity = true;
            }

            DbConnection dbConnection = AttemptToEstablishCurConnection(connectionInfo);
            return dbConnection;
        }

        /// <summary>
        /// called when we need to establish new connection for batch executio as a
        /// result of "connect" command processing
        /// </summary>
        /// <param name="ci"></param>
        /// <returns></returns>
        private DbConnection AttemptToEstablishCurConnection(SqlConnectionInfo ci)
        {
            if (ci == null || ci.ServerName == null)
            {
                return null;
            }

            SqlConnection conn = null;
            try
            {
                string connString = ci.ConnectionString;
                connString += ";Pooling=false"; //turn off connection pooling (this is done in other tools so following the same pattern)

                conn = new SqlConnection(connString);
                conn.RetryLogicProvider = Connection.ReliableConnection.SqlRetryProviders.ServerlessDBRetryProvider();
                conn.Open();

                return conn as DbConnection;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to Change connection to {ci.ServerName}", ex);
            }
        }

    }
}
