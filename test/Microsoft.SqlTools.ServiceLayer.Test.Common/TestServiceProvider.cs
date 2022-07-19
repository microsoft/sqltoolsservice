﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.SqlTools.Credentials;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    /// <summary>
    /// Class to provide SQL tools service classes
    /// </summary>
    public class TestServiceProvider
    {
        private TestServiceProvider()
        {
            InitializeTestServices();
        }

        private static object _lockObject = new object();
        private static TestServiceProvider _instance = new TestServiceProvider();


        public static TestServiceProvider Instance
        {
            get
            {
                return _instance;
            }
        }

        public CredentialService CredentialService
        {
            get
            {
                return CredentialService.Instance;
            }
        }

        public ConnectionService ConnectionService
        {
            get
            {
                return ConnectionService.Instance;
            }
        }

        public ObjectExplorerService ObjectExplorerService
        {
            get
            {
                var serviceProvider = ServiceHost.Instance.ServiceProvider;
                return serviceProvider.GetService<ObjectExplorerService>();
            }
        }

        public TestConnectionProfileService ConnectionProfileService
        {
            get
            {
                return TestConnectionProfileService.Instance;
            }
        }

        public WorkspaceService<SqlToolsSettings> WorkspaceService
        {
            get
            {
                return WorkspaceService<SqlToolsSettings>.Instance;
            }
        }

        /// <summary>
        /// Runs a query by calling the services directly (not using the test driver) 
        /// </summary>
        public void RunQuery(TestServerType serverType, string databaseName, string queryText, bool throwOnError = false)
        {
            RunQueryAsync(serverType, databaseName, queryText, throwOnError).Wait();

        }

        /// <summary>
        /// Runs a query by calling the services directly (not using the test driver) 
        /// </summary>
        public async Task RunQueryAsync(TestServerType serverType, string databaseName, string queryText, bool throwOnError = false)
        {
            string uri = "";
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                uri = queryTempFile.FilePath;

                ConnectionInfo connInfo = await InitLiveConnectionInfo(serverType, databaseName, uri);
                Query query = new Query(queryText, connInfo, new QueryExecutionSettings(), MemoryFileSystem.GetFileStreamFactory());
                query.Execute();
                await query.ExecutionTask;

                if (throwOnError)
                {
                    IEnumerable<Batch> errorBatches = query.Batches.Where(b => b.HasError);
                    if (errorBatches.Count() > 0)
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "The query encountered and error. The batches with errors: {0}",
                                string.Join(Environment.NewLine, errorBatches.Select(b => b.BatchText))));
                    }
                }
                DisconnectConnection(uri);
            }

        }

        public static async Task<T> CalculateRunTime<T>(Func<Task<T>> testToRun, bool printResult, [CallerMemberName] string testName = "")
        {
            TestTimer timer = new TestTimer() { PrintResult = printResult };
            T result = await testToRun();
            timer.EndAndPrint(testName);

            return result;
        }

        private async Task<ConnectionInfo> InitLiveConnectionInfo(TestServerType serverType, string databaseName, string scriptFilePath)
        {
            ConnectParams connectParams = ConnectionProfileService.GetConnectionParameters(serverType, databaseName);

            string ownerUri = scriptFilePath;
            var connectionService = ConnectionService.Instance;
            var connectionResult = await connectionService.Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = connectParams.Connection
                });

            if(!string.IsNullOrEmpty(connectionResult.ErrorMessage))
            {
                throw new Exception($"Error creating live connection to {connectParams.Connection.ServerName} (Type={serverType}). Error: {connectionResult.ErrorMessage}");
            }
            ConnectionInfo? connInfo = null;
            connectionService.TryFindConnection(ownerUri, out connInfo);
            Assert.That(connInfo, Is.Not.Null, $"Could not find connection {ownerUri} when creating live connection");
            return connInfo;
        }

        private void DisconnectConnection(string uri)
        {
            ConnectionService.Instance.Disconnect(new DisconnectParams
            {
                OwnerUri = uri,
                Type = ConnectionType.Default
            });
        }

        private static bool hasInitServices = false;

        private static void InitializeTestServices()
        {
            if (TestServiceProvider.hasInitServices)
            {
                return;
            }

            lock (_lockObject)
            {
                if (TestServiceProvider.hasInitServices)
                {
                    return;
                }
                TestServiceProvider.hasInitServices = true;

                const string hostName = "SQL Tools Test Service Host";
                const string hostProfileId = "SQLToolsTestService";
                Version hostVersion = new Version(1, 0);

                // set up the host details and profile paths 
                var hostDetails = new HostDetails(hostName, hostProfileId, hostVersion);
                SqlToolsContext sqlToolsContext = new SqlToolsContext(hostDetails);

                // Grab the instance of the service host
                ServiceHost serviceHost = HostLoader.CreateAndStartServiceHost(sqlToolsContext);
            }
        }

    }
}
