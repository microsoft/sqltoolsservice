//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Microsoft.SqlTools.EditorServices.Session;
using Microsoft.SqlTools.EditorServices.Utility;

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

            // Create the service host
            ServiceHost.ServiceHost serviceHost = ServiceHost.ServiceHost.Create(hostDetails, profilePaths);

            // Initialize the services that will be hosted here
            LanguageService.LanguageService.Instance.InitializeService(serviceHost);

            // Start the service
            serviceHost.Start().Wait();
            serviceHost.WaitForExit();
        }
    }
}
