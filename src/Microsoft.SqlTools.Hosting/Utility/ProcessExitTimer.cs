//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.SqlTools.Utility
{
    public static class ProcessExitTimer
    {
        /// <summary>
        /// Starts a thread that checks if the provided parent process has exited each time the provided interval has elapsed.
        /// Once the parent process has exited the process that started the timer also exits.
        /// </summary>
        /// <param name="parentProcessId">The ID of the parent process to monitor.</param>
        /// <param name="intervalMs">The time interval in milliseconds for when to poll the parent process.</param>
        /// <returns>The ID of the thread running the timer.</returns>
        public static int Start(int parentProcessId, int intervalMs = 10000)
        {
            var statusThread = new Thread(() => CheckParentStatusLoop(parentProcessId, intervalMs));
            statusThread.Start();
            return statusThread.ManagedThreadId;
        }

        private static void CheckParentStatusLoop(int parentProcessId, int intervalMs)
        {
            var parent = Process.GetProcessById(parentProcessId);
            Logger.Write(TraceEventType.Information, $"Starting thread to check status of parent process. Parent PID: {parent.Id}");
            while (true)
            {
                if (parent.HasExited)
                {
                    var processName = Process.GetCurrentProcess().ProcessName;
                    Logger.Write(TraceEventType.Information, $"Terminating {processName} process because parent process has exited. Parent PID: {parent.Id}");
                    Environment.Exit(0);
                }
                Thread.Sleep(intervalMs);
            }
        }
    }
}