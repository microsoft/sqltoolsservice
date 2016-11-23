//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Tests;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class Common
    {
        public const string PerfTestDatabaseName = "SQLToolsCrossPlatPerfTestDb";
        public const string MasterDatabaseName = "master";


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

        internal static async Task<bool> ConnectAsync(TestHelper testHelper, TestServerType serverType, string query, string ownerUri, string databaseName)
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
            var connectParams = await testHelper.GetDatabaseConnectionAsync(serverType, databaseName);
            
            bool connected = await testHelper.Connect(ownerUri, connectParams);
            Assert.True(connected, "Connection is successful");
            Console.WriteLine($"Connection to {connectParams.Connection.ServerName} is successful");

            return connected;
        }

        internal static async Task<T> CalculateRunTime<T>(Func<Task<T>> testToRun, bool printResult, [CallerMemberName] string testName = "")
        {
            TestTimer timer = new TestTimer() { PrintResult = printResult };
            T result = await testToRun();
            timer.EndAndPrint(testName);

            return result;
        }

        /// <summary>
        /// Create the test db if not already exists
        /// </summary>
        internal static async Task CreateTestDatabase(TestServerType serverType)
        {
            using (TestHelper testHelper = new TestHelper())
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                string databaseName = Common.PerfTestDatabaseName;
                string createDatabaseQuery = Scripts.CreateDatabaseQuery.Replace("#DatabaseName#", databaseName);
                await RunQuery(testHelper, serverType, Common.MasterDatabaseName, createDatabaseQuery);
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Verified test database '{0}' is created", databaseName));
                await RunQuery(testHelper, serverType, databaseName, Scripts.CreateDatabaseObjectsQuery);
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Verified test database '{0}' SQL types are created", databaseName));
            }
        }

        internal static async Task RunQuery(TestHelper testHelper, TestServerType serverType, string databaseName, string query)
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                await Common.ConnectAsync(testHelper, serverType, query, queryTempFile.FilePath, databaseName);
                var queryResult = await Common.CalculateRunTime(() => testHelper.RunQuery(queryTempFile.FilePath, query, 50000), false);

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }
    }
}
