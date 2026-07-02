//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Sts2.Bootstrap;
using Microsoft.SqlTools.Utility;
using System.Diagnostics;
using System.Threading.Tasks;

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

                Logger.Initialize(tracingLevel: commandOptions.TracingLevel, commandOptions.PiiLogging, logFilePath: logFilePath, traceSource: "sqltools", commandOptions.AutoFlushLog);

                // Register PII Logging configuration change callback
                Microsoft.SqlTools.LanguageService.Workspace.WorkspaceService<SqlToolsSettings>.Instance.RegisterConfigChangeCallback((newSettings, oldSettings, context) =>
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

                // STS2 seam (docs/sts2/SPEC.md §5): disabled returns null streams, preserving
                // the existing ServiceHost.Initialize(null, null) console-stream behavior.
                await using Sts2BootstrapHandle sts2 = Sts2Bootstrap.TryStart(args, logFilePath);
                ServiceHost serviceHost = HostLoader.CreateAndStartServiceHost(sqlToolsContext, commandOptions, sts2.LegacyInputStream, sts2.LegacyOutputStream);
                serviceHost.MessageDispatcher.ParallelMessageProcessing = commandOptions.ParallelMessageProcessing;
                serviceHost.MessageDispatcher.ParallelMessageProcessingLimit = commandOptions.ParallelMessageProcessingLimit;

                // If this service was started by another process, then it should shutdown when that parent process does.
                if (commandOptions.ParentProcessId != null)
                {
                    ProcessExitTimer.Start(commandOptions.ParentProcessId.Value);
                }

                // Perf-harness self-report: no-op unless PERF_MODE=1 (fire-and-forget HTTP to a localhost sink).
                PerfSelfReport.TrySendProcessReady();

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
                    // STS2: in enabled mode the multiplexer owns stdout as a framed JSON-RPC
                    // channel, so emergency text MUST go to stderr or it corrupts the stream
                    // (R029). stderr is never the protocol channel in either mode.
                    Console.Error.WriteLine($"Error: Logger unavailable: {loggerEx}");
                    Console.Error.WriteLine($"An unhandled exception occurred: {ex}");
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
