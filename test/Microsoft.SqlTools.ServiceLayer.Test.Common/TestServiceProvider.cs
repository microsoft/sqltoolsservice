//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
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
        private static bool hasInitServices = false;

        public static void InitializeTestServices()
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
