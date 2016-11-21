//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Tests;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class Common
    {
        internal static async Task ExecuteWithTimeout(TestTimer timer, int timeout, Func<Task<bool>> repeatedCode,
            TimeSpan? delay = null, [CallerMemberName] string testName = "")
        {
            while (true)
            {
                if (await repeatedCode())
                {
                    timer.EndAndPrint(testName);
                    break;
                }
                if (timer.TotalMilliSecondsUntilNow >= timeout)
                {
                    Assert.True(false, $"{testName} timed out after {timeout} milliseconds");
                    break;
                }
                if (delay.HasValue)
                {
                    await Task.Delay(delay.Value);
                }
            }
        }

        internal static async Task<bool> ConnectAsync(TestHelper testHelper, TestServerType serverType, string query, string ownerUri)
        {
            testHelper.WriteToFile(ownerUri, query);

            DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = ownerUri,
                    LanguageId = "enu",
                    Version = 1,
                    Text = query
                }
            };

            await testHelper.RequestOpenDocumentNotification(openParams);

            Thread.Sleep(500);
            var connectParams = await testHelper.GetDatabaseConnectionAsync(serverType);
            bool connected = await testHelper.Connect(ownerUri, connectParams);
            Assert.True(connected, "Connection is successful");
            Console.WriteLine($"Connection to {connectParams.Connection.ServerName} is successful");

            return connected;
        }

        internal static async Task<T> CalculateRunTime<T>(Func<Task<T>> testToRun, [CallerMemberName] string testName = "")
        {
            TestTimer timer = new TestTimer();
            T result = await testToRun();
            timer.EndAndPrint(testName);

            return result;
        }
    }
}
