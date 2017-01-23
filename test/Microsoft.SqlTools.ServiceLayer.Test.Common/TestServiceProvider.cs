//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Reflection;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Credentials;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;

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

        public static string GetTestSqlFile()
        {
            string filePath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                "sqltest.sql");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.WriteAllText(filePath, "SELECT * FROM sys.objects\n");

            return filePath;
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

                const string hostName = "SQ Tools Test Service Host";
                const string hostProfileId = "SQLToolsTestService";
                Version hostVersion = new Version(1, 0);

                // set up the host details and profile paths 
                var hostDetails = new HostDetails(hostName, hostProfileId, hostVersion);
                SqlToolsContext sqlToolsContext = new SqlToolsContext(hostDetails);

                // Grab the instance of the service host
                ServiceHost serviceHost = ServiceHost.Instance;

                // Start the service
                serviceHost.Start().Wait();

                // Initialize the services that will be hosted here
                WorkspaceService<SqlToolsSettings>.Instance.InitializeService(serviceHost);
                LanguageService.Instance.InitializeService(serviceHost, sqlToolsContext);
                ConnectionService.Instance.InitializeService(serviceHost);
                CredentialService.Instance.InitializeService(serviceHost);
                QueryExecutionService.Instance.InitializeService(serviceHost);

                serviceHost.Initialize();
            }
        }
    }
}
