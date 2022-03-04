﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility
{
    public class LiveConnectionHelper
    {
        public static string GetTestSqlFile(string fileName = null)
        {
            string filePath = null;
            if (string.IsNullOrEmpty(fileName))
            {
                filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "sqltest.sql");
            }
            else
            {
                filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), fileName + ".sql");
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            File.WriteAllText(filePath, "SELECT * FROM sys.objects\n");
            return filePath;
        }

        public static TestConnectionResult InitLiveConnectionInfo(string databaseName = null, string ownerUri = null)
        {
            ScriptFile scriptFile = null;
            ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem, databaseName);
            if (string.IsNullOrEmpty(ownerUri))
            {
                ownerUri = GetTestSqlFile();
                scriptFile = TestServiceProvider.Instance.WorkspaceService.Workspace.GetFile(ownerUri);
                ownerUri = scriptFile.ClientUri;
            }
            var connectionService = GetLiveTestConnectionService();
            var connectionResult =
                connectionService
                .Connect(new ConnectParams
                {
                    OwnerUri = ownerUri,
                    Connection = connectParams.Connection
                });

            connectionResult.Wait();

            ConnectionInfo connInfo = null;
            connectionService.TryFindConnection(ownerUri, out connInfo);
            return new TestConnectionResult() { ConnectionInfo = connInfo, ScriptFile = scriptFile };
        }

        public static async Task<TestConnectionResult> InitLiveConnectionInfoAsync(string databaseName = null, string ownerUri = null, 
            string connectionType = ServiceLayer.Connection.ConnectionType.Default, TestServerType serverType = TestServerType.OnPrem)
        {
            ScriptFile scriptFile = null;
            if (string.IsNullOrEmpty(ownerUri))
            {
                ownerUri = GetTestSqlFile();
                scriptFile = TestServiceProvider.Instance.WorkspaceService.Workspace.GetFile(ownerUri);
                ownerUri = scriptFile.ClientUri;
            }
            ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(serverType, databaseName);

            var connectionService = GetLiveTestConnectionService();
            var connectionResult =
                await connectionService
                .Connect(new ConnectParams
                {
                    OwnerUri = ownerUri,
                    Connection = connectParams.Connection,
                    Type = connectionType
                });
            if (!string.IsNullOrEmpty(connectionResult.ErrorMessage))
            {
                Console.WriteLine(connectionResult.ErrorMessage);
            }

            ConnectionInfo connInfo = null;
            connectionService.TryFindConnection(ownerUri, out connInfo);
            return new TestConnectionResult() { ConnectionInfo = connInfo, ScriptFile = scriptFile };
        }

        public static ConnectionInfo InitLiveConnectionInfoForDefinition(string databaseName = null)
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem, databaseName);
                string ownerUri = queryTempFile.FilePath;
                var connectionService = GetLiveTestConnectionService();
                var connectionResult =
                    connectionService
                    .Connect(new ConnectParams
                    {
                        OwnerUri = ownerUri,
                        Connection = connectParams.Connection
                    });

                connectionResult.Wait();

                ConnectionInfo connInfo = null;
                connectionService.TryFindConnection(ownerUri, out connInfo);

                Assert.NotNull(connInfo);
                return connInfo;
            }
        }

        public static ServerConnection InitLiveServerConnectionForDefinition(ConnectionInfo connInfo)
        {
            SqlConnection sqlConn = new SqlConnection(ConnectionService.BuildConnectionString(connInfo.ConnectionDetails));
            return new ServerConnection(sqlConn);
        }

        /// <summary>
        /// Creates a test sql connection factory instance
        /// </summary>
        public static ISqlConnectionFactory GetLiveTestSqlConnectionFactory()
        {
            // connect to a real server instance
            return ConnectionService.Instance.ConnectionFactory;
        }

        public static ConnectionService GetLiveTestConnectionService()
        {
            // connect to a real server instance
            return ConnectionService.Instance;
        }

        public class TestConnectionResult
        {
            public ConnectionInfo ConnectionInfo { get; set; }

            public ScriptFile ScriptFile { get; set; }

            public TextDocumentPosition TextDocumentPosition { get; set; }
        }
    }
}
