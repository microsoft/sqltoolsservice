//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.Credentials;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Xunit;

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
        public void RunQuery(TestServerType serverType, string databaseName, string queryText)
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                ConnectionInfo connInfo = InitLiveConnectionInfo(serverType, databaseName, queryTempFile.FilePath);
                Query query = new Query(queryText, connInfo, new QueryExecutionSettings(), MemoryFileSystem.GetFileStreamFactory());
                query.Execute();
                query.ExecutionTask.Wait();
            }
        }

        private ConnectionInfo InitLiveConnectionInfo(TestServerType serverType, string databaseName, string scriptFilePath)
        {
            ConnectParams connectParams = ConnectionProfileService.GetConnectionParameters(serverType, databaseName);

            string ownerUri = scriptFilePath;
            var connectionService = ConnectionService.Instance;
            var connectionResult = connectionService.Connect(new ConnectParams()
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
