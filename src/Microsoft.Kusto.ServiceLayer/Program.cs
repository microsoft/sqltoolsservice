﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using System.Diagnostics;

namespace Microsoft.Kusto.ServiceLayer
{
    /// <summary>
    /// Main application class for SQL Tools API Service Host executable
    /// </summary>
    internal class Program
    {
        internal static string ServiceName;
        
        /// <summary>
        /// Main entry point into the SQL Tools API Service Layer
        /// </summary>
        internal static void Main(string[] args)
        {
            try
            {
                // read command-line arguments
                ServiceLayerCommandOptions commandOptions = new ServiceLayerCommandOptions(args);
                if (commandOptions.ShouldExit)
                {
                    return;
                }

                ServiceName = commandOptions.ServiceName;
                
                string logFilePath = commandOptions.LogFilePath;
                if (string.IsNullOrWhiteSpace(logFilePath))
                {
                    logFilePath = Logger.GenerateLogFilePath("kustoservice");
                }

                Logger.Initialize(tracingLevel: commandOptions.TracingLevel, logFilePath: logFilePath, traceSource: "kustoservice", commandOptions.AutoFlushLog);

                // set up the host details and profile paths 
                var hostDetails = new HostDetails(version: new Version(1, 0));

                SqlToolsContext sqlToolsContext = new SqlToolsContext(hostDetails);
                ServiceHost serviceHost = HostLoader.CreateAndStartServiceHost(sqlToolsContext);

                serviceHost.WaitForExit();
            }
            catch (Exception e)
            {
                Logger.WriteWithCallstack(TraceEventType.Critical, $"An unhandled exception occurred: {e}");
                Environment.Exit(1);
            }
            finally
            {
                Logger.Close();
            }
        }
    }
}
