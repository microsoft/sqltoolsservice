//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

//
// The following is based upon code from PowerShell Editor Services
// License: https://github.com/PowerShell/PowerShellEditorServices/blob/develop/LICENSE
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Driver
{
    /// <summary>
    /// Test driver for the service host
    /// </summary>
    public class ServiceTestDriver : TestDriverBase
    {
        public const string ServiceHostEnvironmentVariable = "SQLTOOLSSERVICE_EXE";

        private Process[] serviceProcesses;

        private DateTime startTime;

        public ServiceTestDriver(string executableFilePath = null)
        {
            string serviceHostExecutable = executableFilePath != null ? executableFilePath : Environment.GetEnvironmentVariable(ServiceHostEnvironmentVariable);
            string serviceHostArguments = "--enable-logging";
            if (string.IsNullOrWhiteSpace(serviceHostExecutable))
            {

                // Include a fallback value to for running tests within visual studio
                serviceHostExecutable =
                    @"..\..\..\..\..\src\Microsoft.SqlTools.ServiceLayer\bin\Debug\net6.0\win-x64\MicrosoftSqlToolsServiceLayer.exe";
                if (!File.Exists(serviceHostExecutable))
                {
                    serviceHostExecutable = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "MicrosoftSqlToolsServiceLayer.exe");
                }
            }

            serviceHostExecutable = Path.GetFullPath(serviceHostExecutable);
            // Make sure it exists before continuing
            if (!File.Exists(serviceHostExecutable))
            {
                throw new FileNotFoundException($"Failed to find Microsoft.SqlTools.ServiceLayer.exe at provided location '{serviceHostExecutable}'. " +
                                                "Please set SQLTOOLSSERVICE_EXE environment variable to location of exe");
            }

            this.clientChannel = new StdioClientChannel(serviceHostExecutable, serviceHostArguments);
            this.protocolClient = new ProtocolEndpoint(clientChannel, MessageProtocolType.LanguageServer);
        }

        /// <summary>
        /// Start the test driver, and launch the sqltoolsservice executable
        /// </summary>
        public async Task Start()
        {
            // Store the time we started
            startTime = DateTime.Now;

            // Launch the process
            this.protocolClient.Initialize();
            await this.protocolClient.Start();
            await Task.Delay(1000); // Wait for the service host to start

            Console.WriteLine("Successfully launched service");

            // Setup events to queue for testing
            this.QueueEventsForType(ConnectionCompleteNotification.Type);
            this.QueueEventsForType(IntelliSenseReadyNotification.Type);
            this.QueueEventsForType(QueryCompleteEvent.Type);
            this.QueueEventsForType(PublishDiagnosticsNotification.Type);
            this.QueueEventsForType(ScriptingCompleteEvent.Type);
            this.QueueEventsForType(ScriptingPlanNotificationEvent.Type);
            this.QueueEventsForType(ScriptingListObjectsCompleteEvent.Type);
        }

        /// <summary>
        /// Stop the test driver, and shutdown the sqltoolsservice executable
        /// </summary>
        public async Task Stop()
        {
            await this.protocolClient.Stop();
        }

        private async Task GetServiceProcess(CancellationToken token)
        {
            while (serviceProcesses == null && !token.IsCancellationRequested)
            {
                var processes = Process.GetProcessesByName("Microsoft.SqlTools.ServiceLayer")
                    .Where(p => p.StartTime >= startTime).ToArray();

                // Wait a second if we can't find the process
                if (processes.Any())
                {
                    serviceProcesses = processes;
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                }
            }
        }
    }
}