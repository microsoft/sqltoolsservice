//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer
{
    /// <summary>
    /// Main application class for SQL Tools API Service Host executable
    /// </summary>
    internal class Program
    {
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

                string logFilePath = commandOptions.LogFilePath;
                if (string.IsNullOrWhiteSpace(logFilePath))
                {
                    logFilePath = Logger.GenerateLogFilePath("sqltools");
                }

                Logger.AutoFlush = commandOptions.AutoFlushLog;

                Logger.Initialize(tracingLevel: commandOptions.TracingLevel, logFilePath: logFilePath, traceSource: "sqltools");

                // set up the host details and profile paths 
                var hostDetails = new HostDetails(version: new Version(1, 0));

                SqlToolsContext sqlToolsContext = new SqlToolsContext(hostDetails);
                ServiceHost serviceHost = HostLoader.CreateAndStartServiceHost(sqlToolsContext);

                // If this service was started by another process, then it should shutdown when that parent process does.
                if (commandOptions.ParentProcessId != null)
                {
                    var parentProcess = Process.GetProcessById(commandOptions.ParentProcessId.Value);
                    var statusThread = new Thread(() => CheckParentStatusLoop(parentProcess));
                    statusThread.Start();
                }

                serviceHost.WaitForExit();
            }
            catch (Exception ex)
            {
                try 
                {
                    Logger.WriteWithCallstack(TraceEventType.Critical, $"An unhandled exception occurred: {ex}");                    
                }
                catch (Exception loggerEx)
                {
                    Console.WriteLine($"Error: Logger unavailable: {loggerEx}");
                    Console.WriteLine($"An unhandled exception occurred: {ex}");
                }
                Environment.Exit(1);
            }
            finally
            {
                Logger.Close();
            }
        }

        private static void CheckParentStatusLoop(Process parent)
        {
            while (true)
            {
                if (parent.HasExited)
                {
                    Environment.Exit(0);
                }
                Thread.Sleep(5000);
            }
        }
    }
}
