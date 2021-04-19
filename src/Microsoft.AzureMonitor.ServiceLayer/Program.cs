using System;
using System.Diagnostics;
using Microsoft.SqlTools.Utility;

namespace Microsoft.AzureMonitor.ServiceLayer
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
            try
            {
                // read command-line arguments
                var commandOptions = new ServiceLayerCommandOptions(args);
                if (commandOptions.ShouldExit)
                {
                    return;
                }

                string logFilePath = commandOptions.LogFilePath;
                if (string.IsNullOrWhiteSpace(logFilePath))
                {
                    logFilePath = Logger.GenerateLogFilePath("azuremonitorservice");
                }

                Logger.AutoFlush = commandOptions.AutoFlushLog;
                Logger.Initialize(commandOptions.TracingLevel, logFilePath, "azuremonitorservice");
                
                ServiceHost serviceHost = HostLoader.CreateAndStartServiceHost();
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