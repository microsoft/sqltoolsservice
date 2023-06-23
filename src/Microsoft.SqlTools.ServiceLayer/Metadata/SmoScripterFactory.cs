//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Specialized;
using System.Data.Common;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;

namespace Microsoft.SqlTools.ServiceLayer.Metadata
{
    internal static class SmoScripterFactory
    {
        public static StringCollection GenerateAllServerScripts(DbConnection connection)
        {
            var serverConnection = SmoScripterFactory.GetServerConnection(connection);
            if (serverConnection == null)
            {
                return null;
            }

            Server server = new Server(serverConnection);
            var scripts = SmoScripterFactory.GenerateAllScripts(server);

            return scripts;
        }

        private static ServerConnection GetServerConnection(DbConnection connection)
        {
            // Get a connection to the database for SMO purposes
            SqlConnection sqlConnection = connection as SqlConnection ?? SmoScripterFactory.TryFindingReliableSqlConnection(connection);
            if (sqlConnection == null)
            {
                return null;
            }

            var serverConnection = SmoScripterFactory.ConnectToServerWithSmo(sqlConnection);
            return serverConnection;
        }

        private static SqlConnection TryFindingReliableSqlConnection(DbConnection connection)
        {
            // It's not actually a SqlConnection, so let's try a reliable SQL connection
            ReliableSqlConnection reliableConnection = connection as ReliableSqlConnection;
            if (reliableConnection == null)
            {
                // If we don't have connection we can use with SMO, just give up on using SMO
                return null;
            }

            // We have a reliable connection, use the underlying connection
            return reliableConnection.GetUnderlyingConnection();
        }

        private static ServerConnection ConnectToServerWithSmo(SqlConnection connection)
        {
            // Connect with SMO and get the metadata for the table
            var serverConnection = (connection.AccessToken == null)
                ? new ServerConnection(connection)
                : new ServerConnection(connection, new AzureAccessToken(connection.AccessToken));

            return serverConnection;
        }

        private static StringCollection GenerateAllScripts(Server server)
        {
            var urns = SmoScripterFactory.GetAllServerObjectUrns(server).ToArray();

            var scriptingOptions = new ScriptingOptions
            {
                AgentAlertJob = false,
                AgentJobId = false,
                AgentNotify = false,
                AllowSystemObjects = false,
                ChangeTracking = false,
                ClusteredIndexes = false,
                ColumnStoreIndexes = false,
                ContinueScriptingOnError = true,
                DriIncludeSystemNames = false,
                Indexes = false,
                NoExecuteAs = true,
                NonClusteredIndexes = false,
                IncludeIfNotExists = false,
                SchemaQualify = true,
                ScriptForCreateOrAlter = true,
                Permissions = false,
                ScriptData = false,
                ScriptOwner = false,
                Statistics = false,
                Triggers = false,
                WithDependencies = false
            };

            var scripter = new Scripter(server);
            scripter.Options = scriptingOptions;
            var scripts = scripter.Script(urns);

            return scripts;
        }

        private static UrnCollection GetAllServerObjectUrns(Server server)
        {
            UrnCollection urnCollection = new UrnCollection();
            urnCollection.Add(server.Urn);

            foreach (Database db in server.Databases)
            {
                urnCollection.Add(db.Urn);

                foreach (SqlServer.Management.Smo.Table t in db.Tables)
                {
                    urnCollection.Add(t.Urn);
                }
            }

            return urnCollection;
        }
    }
}
