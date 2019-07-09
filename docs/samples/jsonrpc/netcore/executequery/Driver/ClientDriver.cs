//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
namespace Microsoft.SqlTools.JsonRpc.Driver
{
    /// <summary>
    /// Test driver for the service host
    /// </summary>
    public class ClientDriver : ClientDriverBase
    {
        public const string ServiceHostEnvironmentVariable = "SQLTOOLSSERVICE_EXE";


        private Process[] serviceProcesses;

        private DateTime startTime;

        public ClientDriver()
        {
            string serviceHostExecutable = Environment.GetEnvironmentVariable(ServiceHostEnvironmentVariable);
            string serviceHostArguments = "--enable-logging";
            if (string.IsNullOrWhiteSpace(serviceHostExecutable))
            {
                // Include a fallback value to for running tests within visual studio
                serviceHostExecutable = @"Microsoft.SqlTools.ServiceLayer.exe";
            }

            // Make sure it exists before continuing
            if (!File.Exists(serviceHostExecutable))
            {
                throw new FileNotFoundException($"Failed to find Microsoft.SqlTools.ServiceLayer.exe at provided location '{serviceHostExecutable}'. " +
                                                "Please set SQLTOOLSERVICE_EXE environment variable to location of exe");
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
