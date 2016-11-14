//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    public class PerformanceTests : TestBase
    {

        private static string ComplexQuery = LoadComplexScript();
        private static string SimpleQuery = "SELECT * FROM sys.all_columns";

        private static string LoadComplexScript()
        {
            try
            {
                string assemblyLocation = Assembly.GetEntryAssembly().Location;
                string folderName = Path.GetDirectoryName(assemblyLocation);
                string filePath = Path.Combine(folderName, "Scripts/AdventureWorks.sql");
                return File.ReadAllText(filePath);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to load the sql script. error: " + ex.Message);
                return "";
            }
        }

        [Fact]
        public async Task HoverTestOnPrem(string scenarioName = "Hover")
        {
            try
            {
                string ownerUri = Path.GetTempFileName();
                string query = SimpleQuery;
               
                await ConnectAsync(TestServerType.OnPrem, query, ownerUri);
                Hover hover = await CalculateRunTime(scenarioName, async () =>
                {
                    return await RequestHover(ownerUri, query, 0, 15); ;
                });
                Assert.True(hover != null, "Hover tool-tip is not null");

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task SuggestionsTest(string scenarioName = "Suggestions")
        {
            try
            {
                string query = SimpleQuery;
                TestServerType serverType = TestServerType.OnPrem;
                string ownerUri = Path.GetTempFileName();

                WriteToFile(ownerUri, query);

                await ConnectAsync(serverType, query, ownerUri);
                await ValidateCompletionResponse(ownerUri, query, null);
                
                await ValidateCompletionResponse(ownerUri, query, scenarioName);

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task DiagnosticsTests(string scenarioName = "Diagnostics")
        {
            try
            {
                string ownerUri = Path.GetTempFileName();
                string query = "SELECT * FROM sys.objects";

                await ConnectAsync(TestServerType.OnPrem, query, ownerUri);
                Thread.Sleep(500);

                var contentChanges = new TextDocumentChangeEvent[1];
                contentChanges[0] = new TextDocumentChangeEvent()
                {
                    Range = new Range()
                    {
                        Start = new Position()
                        {
                            Line = 0,
                            Character = 5
                        },
                        End = new Position()
                        {
                            Line = 0,
                            Character = 6
                        }
                    },
                    RangeLength = 1,
                    Text = "z"
                };

                DidChangeTextDocumentParams changeParams = new DidChangeTextDocumentParams()
                {
                    ContentChanges = contentChanges,
                    TextDocument = new VersionedTextDocumentIdentifier()
                    {
                        Version = 2,
                        Uri = ownerUri
                    }
                };
                TestTimer timer = new TestTimer();
                await RequestChangeTextDocumentNotification(changeParams);
                
                while (true)
                {
                    var completeEvent = await Driver.WaitForEvent(PublishDiagnosticsNotification.Type, 15000);
                    if (completeEvent != null && completeEvent.Diagnostics != null && completeEvent.Diagnostics.Length > 0)
                    {
                        timer.EndAndPrint(scenarioName);
                        break;
                    }
                    if (timer.TotalMilliSecondsUntilNow >= 500000)
                    {
                        Assert.True(false, "Failed to get Diagnostics");
                        break;
                    }
                }

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

        private async Task ValidateCompletionResponse(string ownerUri, string query, string testName)
        {
            TestTimer timer = new TestTimer();
            CompletionItem completion = null;
            while (true)
            {
                CompletionItem[] completions = await RequestCompletion(ownerUri, query, 0, 15);

                completion = completions != null ? completions.FirstOrDefault(x => x.Label == "master") : null;
                if (completion != null)
                {
                    if (testName != null)
                    {
                        timer.EndAndPrint(testName);
                    }
                    break;
                }
                if (timer.TotalMilliSecondsUntilNow >= 500000)
                {
                    Assert.True(false, "Failed to get a valid auto-complete list");
                    break;
                }

                Thread.Sleep(50);
            }
        }

        private async Task VerifyBindingLoadScenario(TestServerType serverType, string query, string testName = null)
        {
            string ownerUri = Path.GetTempFileName();

            WriteToFile(ownerUri, query);

            await ConnectAsync(serverType, query, ownerUri);
            await ValidateCompletionResponse(ownerUri, query, testName);

            await Disconnect(ownerUri);
        }

        [Fact]
        public async Task BindingCacheColdAzureSimpleQuery(string scenarioName = "[Simple query][Cold][SQL DB] Binding cache")
        {
            try
            {
                string query = SimpleQuery;
                Thread.Sleep(5000);
                await VerifyBindingLoadScenario(TestServerType.Azure, query, scenarioName);
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task BindingCacheColdOnPremSimpleQuery(string scenarioName = "[Simple query][Cold][On-Prem] Binding cache")
        {
            try
            {
                string query = SimpleQuery;
                await VerifyBindingLoadScenario(TestServerType.OnPrem, query, scenarioName);
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task BindingCacheWarmAzureSimpleQuery(string scenarioName = "[Simple query][Warm][SQL DB] Binding cache")
        {
            try
            {
                string query = SimpleQuery;
                string ownerUri = Path.GetTempFileName();
                TestServerType serverType = TestServerType.Azure;
                await ConnectAsync(serverType, query, ownerUri);
                Thread.Sleep(10000);
                await VerifyBindingLoadScenario(serverType, query, scenarioName);
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task BindingCacheWarmOnPremSimpleQuery(string scenarioName = "[Simple query][Warm][On-Prem] Binding cache")
        {
            try
            {
                string query = SimpleQuery;
                string ownerUri = Path.GetTempFileName();
                TestServerType serverType = TestServerType.OnPrem;
                await ConnectAsync(serverType, query, ownerUri);
                Thread.Sleep(10000);
                await VerifyBindingLoadScenario(serverType, query, scenarioName);
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task BindingCacheColdAzureComplexQuery(string scenarioName = "[Complex query][Cold][SQL DB] Binding cache")
        {
            try
            {
                string query = ComplexQuery;
                await VerifyBindingLoadScenario(TestServerType.Azure, query, scenarioName);
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task BindingCacheColdOnPremComplexQuery(string scenarioName = "[Complex query][Cold][On-Prem] Binding cache")
        {
            try
            {
                string query = ComplexQuery;
                await VerifyBindingLoadScenario(TestServerType.OnPrem, query, scenarioName);
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task BindingCacheWarmAzureComplexQuery(string scenarioName = "[Complex query][Warm][SQL DB] Binding cache")
        {
            try
            {
                string query = ComplexQuery;
                string ownerUri = Path.GetTempFileName();
                TestServerType serverType = TestServerType.Azure;
                await ConnectAsync(serverType, query, ownerUri);
                Thread.Sleep(100000);
                await VerifyBindingLoadScenario(serverType, query, scenarioName);
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task BindingCacheWarmOnPremComplexQuery(string scenarioName = "[Complex query][Warm][On-Prem] Binding cache")
        {
            try
            {
                string query = ComplexQuery;
                string ownerUri = Path.GetTempFileName();
                TestServerType serverType = TestServerType.OnPrem;
                await ConnectAsync(serverType, query, ownerUri);
                Thread.Sleep(10000);
                await VerifyBindingLoadScenario(serverType, query, scenarioName);
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task ConnectAzureTest(string scenarioName = "Connect SQL DB")
        {
            try
            {
                string query = SimpleQuery;
                string ownerUri = Path.GetTempFileName();
                TestServerType serverType = TestServerType.Azure;
                WriteToFile(ownerUri, query);

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = ownerUri,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await RequestOpenDocumentNotification(openParams);

                Thread.Sleep(500);
                var connected = await CalculateRunTime(scenarioName, async () =>
                {
                    var connectParams = await GetDatabaseConnectionAsync(serverType);
                    return await Connect(ownerUri, connectParams);
                });
                Assert.True(connected, "Connection is successful");
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task ConnectOnPremTest(string scenarioName = "Connect On-Prem")
        {
            try
            {
                string query = SimpleQuery;
                string ownerUri = Path.GetTempFileName();
                TestServerType serverType = TestServerType.OnPrem;
                WriteToFile(ownerUri, query);

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification()
                {
                    TextDocument = new TextDocumentItem()
                    {
                        Uri = ownerUri,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await RequestOpenDocumentNotification(openParams);

                Thread.Sleep(500);
                var connected = await CalculateRunTime(scenarioName, async () =>
                {
                    var connectParams = await GetDatabaseConnectionAsync(serverType);
                    return await Connect(ownerUri, connectParams);
                });
                Assert.True(connected, "Connection is successful");
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task DisconnectTest(string scenarioName = "Disconnect On-Prem")
        {
            try
            {
                string query = SimpleQuery;
                string ownerUri = Path.GetTempFileName();
                TestServerType serverType = TestServerType.OnPrem;
                await ConnectAsync(serverType, query, ownerUri);
                Thread.Sleep(1000);
                var connected = await CalculateRunTime(scenarioName, async () =>
                {
                    return await base.Disconnect(ownerUri);
                });
                Assert.True(connected);
            }
            finally
            {
                WaitForExit();
            }
        }

        [Fact]
        public async Task QueryResultSummaryOnPremTest(string scenarioName = "Basic Query Result On-Prem")
        {
            string ownerUri = Path.GetTempFileName();
            TestServerType serverType = TestServerType.OnPrem;
            string query = SimpleQuery;

            await ConnectAsync(serverType, query, ownerUri);

            var queryTask = await CalculateRunTime(scenarioName, async () =>
            {
                return await RunQuery(ownerUri, query);
            });
             
            Assert.NotNull(queryTask);
            Assert.True(queryTask.BatchSummaries.Any(x => x.ResultSetSummaries.Any( r => r.RowCount > 0)));

            await Disconnect(ownerUri);
        }

        [Fact]
        public async Task QueryResultFirstOnPremTest(string scenarioName = "Basic Query Result First Rows On-Prem")
        {
            string ownerUri = Path.GetTempFileName();
            TestServerType serverType = TestServerType.OnPrem;
            string query = SimpleQuery;

            await ConnectAsync(serverType, query, ownerUri);

            var queryResult = await CalculateRunTime(scenarioName, async () =>
            {
                var queryTask = await RunQuery(ownerUri, query);
                return await ExecuteSubset(ownerUri, 0, 0, 0, 100);
            });

            Assert.NotNull(queryResult);
            Assert.NotNull(queryResult.ResultSubset);
            Assert.True(queryResult.ResultSubset.Rows.Count() > 0);

            await Disconnect(ownerUri);
        }


        [Fact]
        public async Task CancelQueryOnPremTest(string scenarioName = "Cancel Query On-Prem")
        {
            string ownerUri = Path.GetTempFileName();
            TestServerType serverType = TestServerType.OnPrem;
            string query = "WAITFOR DELAY '00:01:00';";

            await ConnectAsync(serverType, query, ownerUri);
            var queryParams = new QueryExecuteParams();
            queryParams.OwnerUri = ownerUri;
            queryParams.QuerySelection = null;

            var result = await Driver.SendRequest(QueryExecuteRequest.Type, queryParams);
            if (result != null && string.IsNullOrEmpty(result.Messages))
            {
                TestTimer timer = new TestTimer();

                while (true)
                {
                    var queryTask = await CancelQuery(ownerUri);
                    if (queryTask != null)
                    {
                        timer.EndAndPrint(scenarioName);
                        break;
                    }
                    if (timer.TotalMilliSecondsUntilNow >= 100000)
                    {
                        Assert.True(false, "Failed to cancel query");
                        break;
                    }

                    Thread.Sleep(10);
                }
            }
            else
            {
                Assert.True(false, "Failed to run the query");
            }

            await Disconnect(ownerUri);
        }

        [Fact]
        public async Task TestSaveResultsToCsvTest(string scenarioName = "Basic Query Save To CSV")
        {
            string ownerUri = Path.GetTempFileName();
            string query = SimpleQuery;
            TestServerType serverType = TestServerType.OnPrem;
            string output = Path.GetTempFileName();
            await ConnectAsync(serverType, query, ownerUri);

            // Execute a query
            await RunQuery(ownerUri, query);

            var saveTask = await CalculateRunTime(scenarioName, async () =>
            {
                return await SaveAsCsv(ownerUri, output, 0, 0);
            });
            
            await Disconnect(ownerUri);
        }

        [Fact]
        public async Task TestSaveResultsToJsonTest(string scenarioName = "Basic Query Save To Json")
        {
            string ownerUri = Path.GetTempFileName();
            string query = SimpleQuery;
            TestServerType serverType = TestServerType.OnPrem;
            await ConnectAsync(serverType, query, ownerUri);
            string output = Path.GetTempFileName();
            // Execute a query
            await RunQuery(ownerUri, query);

            var saveTask = await CalculateRunTime(scenarioName, async () =>
            {
                return await SaveAsJson(ownerUri, output, 0, 0);
            });

            await Disconnect(ownerUri);
        }

        private async Task<bool> ConnectAsync(TestServerType serverType, string query, string ownerUri)
        {
            WriteToFile(ownerUri, query);

            DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification()
            {
                TextDocument = new TextDocumentItem()
                {
                    Uri = ownerUri,
                    LanguageId = "enu",
                    Version = 1,
                    Text = query
                }
            };

            await RequestOpenDocumentNotification(openParams);

            Thread.Sleep(500);
            var connectParams = await GetDatabaseConnectionAsync(serverType);
            bool connected = await Connect(ownerUri, connectParams);
            Assert.True(connected, "Connection is successful");
            if (connected)
            {
                Console.WriteLine("Connection is successful");
            }

            return connected;
        }

        private async Task<T> CalculateRunTime<T>(string testName, Func<Task<T>> testToRun)
        {
            TestTimer timer = new TestTimer();
            T result = await testToRun();
            timer.EndAndPrint(testName);
           

            return result;
        }
    }
}
