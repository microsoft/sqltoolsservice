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
    internal class SmoScriptorFactory
    {
        public StringCollection GetAllScripts(DbConnection connection)
        {
            var serverConnection = GetServerConnection(connection);
            if (serverConnection == null)
            {
                return new StringCollection();
            }

            Server server = new Server(serverConnection);
            var stringCollection = GenerateAllScripts(server);
            return stringCollection;
        }

        private ServerConnection GetServerConnection(DbConnection connection)
        {
            // Get a connection to the database for SMO purposes
            SqlConnection sqlConnection = connection as SqlConnection;
            if (sqlConnection == null)
            {
                // It's not actually a SqlConnection, so let's try a reliable SQL connection
                ReliableSqlConnection reliableConn = connection as ReliableSqlConnection;
                if (reliableConn == null)
                {
                    // If we don't have connection we can use with SMO, just give up on using SMO
                    return null;
                }

                // We have a reliable connection, use the underlying connection
                sqlConnection = reliableConn.GetUnderlyingConnection();
            }

            // Connect with SMO and get the metadata for the table
            ServerConnection serverConnection;
            if (sqlConnection.AccessToken == null)
            {
                serverConnection = new ServerConnection(sqlConnection);
            }
            else
            {
                serverConnection = new ServerConnection(sqlConnection, new AzureAccessToken(sqlConnection.AccessToken));
            }

            return serverConnection;
        }
        
        private StringCollection GenerateAllScripts(Server server)
        {
            var urns = GetAllServerObjectUrns(server).ToArray();

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
            var stringCollection = scripter.Script(urns);

            return stringCollection;
        }

        private UrnCollection GetAllServerObjectUrns(Server server)
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

                //foreach (SqlServer.Management.Smo.View v in db.Views)
                //{
                //    urnCollection.Add(v.Urn);
                //}

                //foreach (StoredProcedure sp in db.StoredProcedures)
                //{
                //    urnCollection.Add(sp.Urn);
                //}
            }

            return urnCollection;
        }
    }
}
