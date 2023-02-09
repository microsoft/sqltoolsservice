//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.Authentication.Sql;

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
        internal static async Task Main(string[] args)
        {
            SqlClientListener? sqlClientListener = null;
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

                Logger.Initialize(tracingLevel: commandOptions.TracingLevel, logFilePath: logFilePath, traceSource: "sqltools", commandOptions.AutoFlushLog);
                // Only enable SQL Client logging when verbose or higher to avoid extra overhead when the
                // detailed logging it provides isn't needed
                if (Logger.TracingLevel.HasFlag(SourceLevels.Verbose))
                {
                    sqlClientListener = new SqlClientListener();
                }


                // set up the host details and profile paths 
                var hostDetails = new HostDetails(version: new Version(1, 0));

                SqlToolsContext sqlToolsContext = new SqlToolsContext(hostDetails);
                ServiceHost serviceHost = HostLoader.CreateAndStartServiceHost(sqlToolsContext);
                serviceHost.MessageDispatcher.ParallelMessageProcessing = commandOptions.ParallelMessageProcessing;

                if (commandOptions.EnableSqlAuthenticationProvider)
                {
                    // Register SqlAuthenticationProvider with SqlConnection for AAD Interactive (MFA) authentication.
                    SqlAuthenticationProvider.SetProvider(SqlAuthenticationMethod.ActiveDirectoryInteractive, new AuthenticationProvider());
                    ConnectionService.EnableSqlAuthenticationProvider = true;
                }

                // If this service was started by another process, then it should shutdown when that parent process does.
                if (commandOptions.ParentProcessId != null)
                {
                    ProcessExitTimer.Start(commandOptions.ParentProcessId.Value);
                }

                await serviceHost.WaitForExitAsync();
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
                sqlClientListener?.Dispose();
            }
        }
    }
}
