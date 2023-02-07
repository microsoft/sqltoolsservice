//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Diagnostics;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Utility;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.Migration
{
    internal class Program
    {
        private const string ServiceName = "MicrosoftSqlToolsMigration.exe";

        internal static async Task Main(string[] args)
        {
            try
            {
                CommandOptions commandOptions = new CommandOptions(args, ServiceName);
                if (commandOptions.ShouldExit)
                {
                    return;
                }

                string logFilePath = "MicrosoftSqlToolsMigration";
                if (!string.IsNullOrWhiteSpace(commandOptions.LogFilePath))
                {
                    logFilePath = Path.Combine(commandOptions.LogFilePath, logFilePath);
                }
                else
                {
                    logFilePath = Logger.GenerateLogFilePath(logFilePath);
                }

                Logger.Initialize(SourceLevels.Verbose, logFilePath, "Migration", commandOptions.AutoFlushLog);
                
                Logger.Verbose("Starting SqlTools Migration Server...");

                ExtensionServiceHost serviceHost = new ExtensionServiceHost(
                    new ExtensibleServiceHostOptions
                    {
                        HostName = "Migration",
                        HostProfileId = "SqlTools.Migration",
                        HostVersion = new Version(1, 0, 0, 0),
                        InitializeServiceCallback = (server, serivce) => { }
                    });

                serviceHost.RegisterAndInitializeService(new MigrationService());
                await serviceHost.WaitForExitAsync();
                Logger.Verbose("SqlTools Migration Server exiting....");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw ex;
            }
        }

    }

}