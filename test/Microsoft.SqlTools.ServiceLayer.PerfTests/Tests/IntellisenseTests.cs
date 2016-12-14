//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Tests;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class IntellisenseTests
    {
        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task HoverTestOnPrem()
        {
            TestServerType serverType = TestServerType.OnPrem;
            using (TestHelper testHelper = new TestHelper())
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                const string query = Scripts.TestDbSimpleSelectQuery;
                await Common.ConnectAsync(testHelper, serverType, query, queryTempFile.FilePath, Common.PerfTestDatabaseName);
                Hover hover = await Common.CalculateRunTime(() => testHelper.RequestHover(queryTempFile.FilePath, query, 0, Scripts.TestDbComplexSelectQueries.Length + 1), true);
                Assert.NotNull(hover);
                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task SuggestionsTest()
        {
            TestServerType serverType = TestServerType.OnPrem;
            using (TestHelper testHelper = new TestHelper())
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                const string query = Scripts.TestDbSimpleSelectQuery;
                await Common.ConnectAsync(testHelper, serverType, query, queryTempFile.FilePath, Common.PerfTestDatabaseName);
                await ValidateCompletionResponse(testHelper, queryTempFile.FilePath, false, Common.PerfTestDatabaseName, true);
                await ValidateCompletionResponse(testHelper, queryTempFile.FilePath, true, Common.PerfTestDatabaseName, false);
                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task DiagnosticsTests()
        {
            TestServerType serverType = TestServerType.OnPrem;
            await Common.CreateTestDatabase(serverType);

            using (TestHelper testHelper = new TestHelper())
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                await Common.ConnectAsync(testHelper, serverType, Scripts.TestDbSimpleSelectQuery, queryTempFile.FilePath, Common.PerfTestDatabaseName);

                Thread.Sleep(500);
                var contentChanges = new TextDocumentChangeEvent[1];
                contentChanges[0] = new TextDocumentChangeEvent()
                {
                    Range = new Range
                    {
                        Start = new Position
                        {
                            Line = 0,
                            Character = 5
                        },
                        End = new Position
                        {
                            Line = 0,
                            Character = 6
                        }
                    },
                    RangeLength = 1,
                    Text = "z"
                };
                DidChangeTextDocumentParams changeParams = new DidChangeTextDocumentParams
                {
                    ContentChanges = contentChanges,
                    TextDocument = new VersionedTextDocumentIdentifier
                    {
                        Version = 2,
                        Uri = queryTempFile.FilePath
                    }
                };

                TestTimer timer = new TestTimer() { PrintResult = true };
                await testHelper.RequestChangeTextDocumentNotification(changeParams);
                await Common.ExecuteWithTimeout(timer, 60000, async () =>
                {
                    var completeEvent = await testHelper.Driver.WaitForEvent(PublishDiagnosticsNotification.Type, 15000);
                    return completeEvent?.Diagnostics != null && completeEvent.Diagnostics.Length > 0;
                });
                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        [CreateTestDb(TestServerType.Azure)]
        public async Task BindingCacheColdAzureSimpleQuery()
        {
            TestServerType serverType = TestServerType.Azure;
            using (TestHelper testHelper = new TestHelper())
            {
                await VerifyBindingLoadScenario(testHelper, serverType, Scripts.TestDbSimpleSelectQuery, false);
            }
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task BindingCacheColdOnPremSimpleQuery()
        {
            TestServerType serverType = TestServerType.OnPrem;
            using (TestHelper testHelper = new TestHelper())
            {
                await VerifyBindingLoadScenario(testHelper, serverType, Scripts.TestDbSimpleSelectQuery, false);
            }
            
        }

        [Fact]
        [CreateTestDb(TestServerType.Azure)]
        public async Task BindingCacheWarmAzureSimpleQuery()
        {
            TestServerType serverType = TestServerType.Azure;
            using (TestHelper testHelper = new TestHelper())
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                const string query = Scripts.TestDbSimpleSelectQuery;
                await VerifyBindingLoadScenario(testHelper, serverType, query, true);
            }
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task BindingCacheWarmOnPremSimpleQuery()
        {
            TestServerType serverType = TestServerType.OnPrem;

            using (TestHelper testHelper = new TestHelper())
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                const string query = Scripts.TestDbSimpleSelectQuery;
                await VerifyBindingLoadScenario(testHelper, serverType, query, true);
            }
        }

        [Fact]
        [CreateTestDb(TestServerType.Azure)]
        public async Task BindingCacheColdAzureComplexQuery()
        {
            TestServerType serverType = TestServerType.Azure;

            using (TestHelper testHelper = new TestHelper())
            {
                await VerifyBindingLoadScenario(testHelper, serverType, Scripts.TestDbComplexSelectQueries,false);
            }
        }

        [Fact]
        [CreateTestDb(TestServerType.Azure)]
        public async Task BindingCacheColdOnPremComplexQuery()
        {
            TestServerType serverType = TestServerType.OnPrem;
            using (TestHelper testHelper = new TestHelper())
            {
                await VerifyBindingLoadScenario(testHelper, serverType, Scripts.TestDbComplexSelectQueries, false);
            }
        }

        [Fact]
        [CreateTestDb(TestServerType.Azure)]
        public async Task BindingCacheWarmAzureComplexQuery()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                string query = Scripts.TestDbComplexSelectQueries; 
                const TestServerType serverType = TestServerType.Azure;
                await VerifyBindingLoadScenario(testHelper, serverType, query, true);
            }
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task BindingCacheWarmOnPremComplexQuery()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                string query = Scripts.TestDbComplexSelectQueries;
                const TestServerType serverType = TestServerType.OnPrem;
                await VerifyBindingLoadScenario(testHelper, serverType, query, true);
            }
        }

        #region Private Helper Methods

        private async Task VerifyBindingLoadScenario(
            TestHelper testHelper, 
            TestServerType serverType, 
            string query, 
            bool preLoad, 
            [CallerMemberName] string testName = "")
        {
            string databaseName = Common.PerfTestDatabaseName;
            if (preLoad)
            {
                await VerifyCompletationLoaded(testHelper, serverType, Scripts.TestDbSimpleSelectQuery, 
                    databaseName, printResult: false, testName: testName);
                Console.WriteLine("Intellisense cache loaded.");
            }
            await VerifyCompletationLoaded(testHelper, serverType, query, databaseName, 
                printResult: true, testName: testName);
        }

        private  async Task VerifyCompletationLoaded(
            TestHelper testHelper, 
            TestServerType serverType, 
            string query, 
            string databaseName,
            bool printResult, 
            string testName)
        {
            using (SelfCleaningTempFile testTempFile = new SelfCleaningTempFile())
            {
                testHelper.WriteToFile(testTempFile.FilePath, query);
                await Common.ConnectAsync(testHelper, serverType, query, testTempFile.FilePath, databaseName);
                await ValidateCompletionResponse(testHelper, testTempFile.FilePath, printResult, databaseName, 
                    waitForIntelliSense: true, testName: testName);
                await testHelper.Disconnect(testTempFile.FilePath);
            }
        }

        private static async Task ValidateCompletionResponse(
            TestHelper testHelper, 
            string ownerUri, 
            bool printResult, 
            string databaseName, 
            bool waitForIntelliSense, 
            [CallerMemberName] string testName = "")
        {
            TestTimer timer = new TestTimer() { PrintResult = printResult };
            bool isReady = !waitForIntelliSense;
            await Common.ExecuteWithTimeout(timer, 150000, async () =>
            {
                if (isReady)
                {
                    string query = Scripts.SelectQuery;
                    CompletionItem[] completions = await testHelper.RequestCompletion(ownerUri, query, 0, query.Length + 1);
                    return completions != null && 
                    (completions.Any(x => string.Compare(x.Label, databaseName, StringComparison.OrdinalIgnoreCase) == 0 || 
                    string.Compare(x.Label, $"[{databaseName}]", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(x.Label, $"\"{databaseName}\"", StringComparison.OrdinalIgnoreCase) == 0));
                }
                else
                {
                    var completeEvent = await testHelper.Driver.WaitForEvent(IntelliSenseReadyNotification.Type, 100000);
                    isReady = completeEvent.OwnerUri == ownerUri;
                    if (isReady)
                    {
                        Console.WriteLine("IntelliSense cache is loaded.");
                    }
                    return false;
                }
            }, testName: testName);
         }

        #endregion
    }
}
