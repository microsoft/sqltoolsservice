//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using System.Diagnostics;
using System.Threading;

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

                // If this service was started by another process, then it should shutdown when that parent process does.
                if (commandOptions.ParentProcessId != null)
                {
                    var parentProcess = Process.GetProcessById(commandOptions.ParentProcessId.Value);
                    var statusThread = new Thread(() => CheckParentStatusLoop(parentProcess));
                    statusThread.Start();
                }

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

        private static void CheckParentStatusLoop(Process parent)
        {
            Logger.Write(TraceEventType.Information, $"Starting thread to check status of parent process. Parent PID: {parent.Id}");
            while (true)
            {
                if (parent.HasExited)
                {
                    var processName = Process.GetCurrentProcess().ProcessName;
                    Logger.Write(TraceEventType.Information, $"Terminating {processName} process because parent process has exited. Parent PID: {parent.Id}");
                    Environment.Exit(0);
                }
                Thread.Sleep(10000);
            }
        }
    }
}
