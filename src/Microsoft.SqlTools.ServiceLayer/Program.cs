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
        internal static async Task Main(string[] args)
        {
            ThreadPool.GetMinThreads(out var minWorkerThreads, out var minCompletionThreads);
            ThreadPool.GetMaxThreads(out var workerThreads, out var completionThreads);
            ThreadPool.SetMinThreads(workerThreads, completionThreads);

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

                Logger.Initialize(tracingLevel: commandOptions.TracingLevel, commandOptions.PiiLogging, logFilePath: logFilePath, traceSource: "sqltools", commandOptions.AutoFlushLog);

                Logger.Verbose($"Configured min worker threads: {workerThreads} from {minWorkerThreads} and min completion threads: {completionThreads} from {minCompletionThreads}");

                // Register PII Logging configuration change callback
                Workspace.WorkspaceService<SqlToolsSettings>.Instance.RegisterConfigChangeCallback((newSettings, oldSettings, context) =>
                {
                    Logger.IsPiiEnabled = newSettings?.MssqlTools?.PiiLogging ?? false;
                    Logger.Information(Logger.IsPiiEnabled ? "PII Logging enabled" : "PII Logging disabled");
                    return Task.FromResult(true);
                });

                // Only enable SQL Client logging when verbose or higher to avoid extra overhead when the
                // detailed logging it provides isn't needed
                if (Logger.TracingLevel.HasFlag(SourceLevels.Verbose))
                {
                    sqlClientListener = new SqlClientListener();
                }

                // set up the host details and profile paths 
                var hostDetails = new HostDetails(version: new Version(1, 0));

                SqlToolsContext sqlToolsContext = new SqlToolsContext(hostDetails);
                ServiceHost serviceHost = HostLoader.CreateAndStartServiceHost(sqlToolsContext, commandOptions);
                serviceHost.MessageDispatcher.ParallelMessageProcessing = commandOptions.ParallelMessageProcessing;

                // If this service was started by another process, then it should shutdown when that parent process does.
                if (commandOptions.ParentProcessId != null)
                {
                    ProcessExitTimer.Start(commandOptions.ParentProcessId.Value);
                }

                // Run background thread monitor to track thread availability.
                Task threadMonitor = Task.Run(async () =>
                {
                    int workerThreads = 0, completionThreads = 0;
                    await Task.Delay(60000); // 1 min delay to monitor thread activity.
                    ThreadPool.GetAvailableThreads(out workerThreads, out completionThreads);
                    Logger.Verbose($"Currently available threads in threadpool: WorkerThreds = {workerThreads}; CompletionPortThreads ={completionThreads}");
                });

                await new Task(async() => await serviceHost.WaitForExitAsync(), TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
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
