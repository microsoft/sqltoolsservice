//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.SqlTools.EditorServices.Utility;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;

namespace Microsoft.SqlTools.ServiceLayer
{     
    /// <summary>
    /// Main application class for SQL Tools API Service Host executable
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main entry point into the SQL Tools API Service Host
        /// </summary>
        static void Main(string[] args)
        {
            // turn on Verbose logging during early development
            // we need to switch to Normal when preparing for public preview
            Logger.Initialize(minimumLogLevel: LogLevel.Verbose);
            Logger.Write(LogLevel.Normal, "Starting SQL Tools Service Host");

            const string hostName = "SQL Tools Service Host";
            const string hostProfileId = "SQLToolsService";
            Version hostVersion = new Version(1,0); 

            // set up the host details and profile paths 
            var hostDetails = new HostDetails(hostName, hostProfileId, hostVersion);     
            var profilePaths = new ProfilePaths(hostProfileId, "baseAllUsersPath", "baseCurrentUserPath");
            SqlToolsContext sqlToolsContext = new SqlToolsContext(hostDetails, profilePaths);

            // Grab the instance of the service host
            ServiceHost serviceHost = ServiceHost.Instance;

            // Start the service
            serviceHost.Start().Wait();

            // Initialize the services that will be hosted here
            WorkspaceService<SqlToolsSettings>.Instance.InitializeService(serviceHost);
            //AutoCompleteService.Instance.InitializeService(serviceHost);
            //LanguageService.Instance.InitializeService(serviceHost, sqlToolsContext);
            ConnectionService.Instance.InitializeService(serviceHost);
            QueryExecutionService.Instance.InitializeService(serviceHost);

            // TEST

            ConnectionService.Instance.Connect(new ConnectParams
            {
                Connection = new ConnectionDetails
                {
                    DatabaseName = "keep_AdventureworksDW2016CTP3",
                    Password = "Yukon900",
                    ServerName = "sqltools11",
                    UserName = "sa"
                },
                OwnerUri = "test"
            });
            ConnectionInfo ci;
            ConnectionService.Instance.TryFindConnection("test", out ci);


            using (Query q = new Query("SELECT * FROM sys.objects AS a CROSS JOIN sys.objects AS b CROSS JOIN sys.objects AS c", ci,
                    new QueryExecutionSettings(), new ServiceBufferFileStreamFactory()))
            {
                q.Execute().Wait();
                ResultSetSubset qqq = q.GetSubset(0, 0, 0, 10).Result;
            }
            Debugger.Break();


            // END TEST

            serviceHost.Initialize();
            serviceHost.WaitForExit();
        }
    }
}
