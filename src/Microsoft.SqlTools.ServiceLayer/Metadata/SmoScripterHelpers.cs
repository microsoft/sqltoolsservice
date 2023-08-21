//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.SqlCore.Connection;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Metadata
{
    internal static class SmoScripterHelpers
    {
        public static IEnumerable<string>? GenerateAllServerTableScripts(DbConnection connection)
        {
            var serverConnection = SmoScripterHelpers.GetServerConnection(connection);
            if (serverConnection == null)
            {
                return null;
            }

            Server server = new Server(serverConnection);
            var scripts = SmoScripterHelpers.GenerateTableScripts(server);

            return scripts;
        }

        private static ServerConnection? GetServerConnection(DbConnection connection)
        {
            // Get a connection to the database for SMO purposes
            var sqlConnection = connection as SqlConnection ?? SmoScripterHelpers.TryFindingReliableSqlConnection(connection as ReliableSqlConnection);
            if (sqlConnection == null)
            {
                return null;
            }

            var serverConnection = SmoScripterHelpers.ConnectToServerWithSmo(sqlConnection);
            return serverConnection;
        }

        private static SqlConnection? TryFindingReliableSqlConnection(ReliableSqlConnection reliableSqlConnection)
        {
            // It's not actually a SqlConnection, so let's try a reliable SQL connection
            if (reliableSqlConnection == null)
            {
                // If we don't have connection we can use with SMO, just give up on using SMO
                return null;
            }

            // We have a reliable connection, use the underlying connection
            return reliableSqlConnection.GetUnderlyingConnection();
        }

        private static ServerConnection ConnectToServerWithSmo(SqlConnection connection)
        {
            // Connect with SMO and get the metadata for the table
            var serverConnection = (connection.AccessToken == null)
                ? new ServerConnection(connection)
                : new ServerConnection(connection, new AzureAccessToken(connection.AccessToken));

            return serverConnection;
        }

        private static IEnumerable<string> GenerateTableScripts(Server server)
        {
            var urns = SmoScripterHelpers.GetAllServerTableAndViewUrns(server);

            var scriptingOptions = new ScriptingOptions
            {
                AgentAlertJob = false,
                AgentJobId = false,
                AgentNotify = false,
                AllowSystemObjects = false,
                AnsiFile = false,
                AnsiPadding = false,
                AppendToFile = false,
                Bindings = false,
                ChangeTracking = false,
                ClusteredIndexes = false,
                ColumnStoreIndexes = false,
                ContinueScriptingOnError = true,
                ConvertUserDefinedDataTypesToBaseType = false,
                DdlBodyOnly = false,
                DdlHeaderOnly = true,
                DriAll = false,
                DriAllConstraints = false,
                DriAllKeys = false,
                DriChecks = false,
                DriClustered = false,
                DriDefaults = false,
                DriForeignKeys = false,
                DriIncludeSystemNames = false,
                DriIndexes = false,
                DriNonClustered = false,
                DriPrimaryKey = false,
                DriUniqueKeys = false,
                DriWithNoCheck = false,
                EnforceScriptingOptions = true,
                ExtendedProperties = false,
                FullTextCatalogs = false,
                FullTextIndexes = false,
                FullTextStopLists = false,
                IncludeDatabaseContext = false,
                IncludeDatabaseRoleMemberships = false,
                IncludeFullTextCatalogRootPath = false,
                IncludeHeaders = false,
                IncludeIfNotExists = false,
                IncludeScriptingParametersHeader = false,
                Indexes = false,
                LoginSid = false,
                NoAssemblies = true,
                NoCollation = true,
                NoCommandTerminator = true,
                NoExecuteAs = true,
                NoFileGroup = true,
                NoFileStream = true,
                NoFileStreamColumn = true,
                NoIdentities = true,
                NoIndexPartitioningSchemes = true,
                NoMailProfileAccounts = true,
                NoMailProfilePrincipals = true,
                NonClusteredIndexes = false,
                NoTablePartitioningSchemes = true,
                NoVardecimal = false,
                NoViewColumns = false,
                NoXmlNamespaces = false,
                OptimizerData = false,
                Permissions = false,
                PrimaryObject = true,
                SchemaQualify = true,
                SchemaQualifyForeignKeysReferences = true,
                ScriptBatchTerminator = false,
                ScriptData = false,
                ScriptDataCompression = false,
                ScriptDrops = false,
                ScriptForAlter = false,
                ScriptForCreateDrop = false,
                ScriptForCreateOrAlter = true,
                ScriptOwner = false,
                ScriptSchema = true,
                ScriptXmlCompression = false,
                SpatialIndexes = false,
                Statistics = false,
                TimestampToBinary = false,
                ToFileOnly = false,
                Triggers = false,
                WithDependencies = false,
                XmlIndexes = false
            };

            var scripter = new Scripter(server);
            scripter.Options = scriptingOptions;
            var generatedScripts = scripter.Script(urns);

            var scripts = new List<string>();
            foreach (var s in generatedScripts)
            {
                // Needed to remove '\r' and '\n' characters from script, so that an entire create script
                // can be written and read as a single line to and from a temp file. Since scripts aren't
                // going to be read by people, and mainly sent to Copilot to generate accurate suggestions,
                // a lack of formatting is fine.
                var script = s.Replace("\r", string.Empty).Replace("\n", string.Empty);
                scripts.Add(script);
            }

            return scripts;
        }

        private static UrnCollection GetAllServerTableAndViewUrns(Server server)
        {
            UrnCollection urnCollection = new UrnCollection();

            foreach (Database db in server.Databases)
            {
                try
                {
                    foreach (SqlServer.Management.Smo.Table t in db.Tables)
                    {
                        urnCollection.Add(t.Urn);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Unable to get table URNs. Error: {ex.Message}");
                }

                try
                {
                    foreach (SqlServer.Management.Smo.View v in db.Views)
                    {
                        urnCollection.Add(v.Urn);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Unable to get view URNs. Error: {ex.Message}");
                }
            }
            return urnCollection;
        }
    }
}
