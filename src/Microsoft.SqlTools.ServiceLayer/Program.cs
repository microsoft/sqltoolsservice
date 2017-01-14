//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Credentials;
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer
{
    /// <summary>
    /// Main application class for SQL Tools API Service Host executable
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main entry point into the SQL Tools API Service Host
        /// </summary>
        internal static void Main(string[] args)
        {
            // read command-line arguments
            CommandOptions commandOptions = new CommandOptions(args);
            if (commandOptions.ShouldExit)
            {
                return;
            }

            // turn on Verbose logging during early development
            // we need to switch to Normal when preparing for public preview
            Logger.Initialize(minimumLogLevel: LogLevel.Verbose, isEnabled: commandOptions.EnableLogging);
            Logger.Write(LogLevel.Normal, "Starting SQL Tools Service Host");

            // set up the host details and profile paths 
            var hostDetails = new HostDetails(version: new Version(1,0));

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
            EditDataService.Instance.InitializeService(serviceHost);

            serviceHost.Initialize();
            serviceHost.WaitForExit();
        }
    }
}
