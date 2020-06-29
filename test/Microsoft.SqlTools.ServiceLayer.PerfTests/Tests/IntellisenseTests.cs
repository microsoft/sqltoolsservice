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
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;
using Range = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Range;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class IntellisenseTests
    {
        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task HoverTestOnPrem()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.OnPrem;
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    const string query = Scripts.TestDbSimpleSelectQuery;
                    await testService.ConnectForQuery(serverType, query, queryTempFile.FilePath, Common.PerfTestDatabaseName);
                    await ValidateCompletionResponse(testService, queryTempFile.FilePath, Common.PerfTestDatabaseName, waitForIntelliSense: true);
                    Hover hover = await testService.CalculateRunTime(() => testService.RequestHover(queryTempFile.FilePath, query, 0, Scripts.TestDbComplexSelectQueries.Length + 1), timer);
                    Assert.NotNull(hover);
                    await testService.Disconnect(queryTempFile.FilePath);
                }
            });
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task SuggestionsTest()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.OnPrem;
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    const string query = Scripts.TestDbSimpleSelectQuery;
                    await testService.ConnectForQuery(serverType, query, queryTempFile.FilePath, Common.PerfTestDatabaseName);
                    await ValidateCompletionResponse(testService, queryTempFile.FilePath, Common.PerfTestDatabaseName, timer: null, waitForIntelliSense: true);
                    await ValidateCompletionResponse(testService, queryTempFile.FilePath, Common.PerfTestDatabaseName, timer: timer, waitForIntelliSense: false);
                    await testService.Disconnect(queryTempFile.FilePath);
                }
            });
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task DiagnosticsTests()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.OnPrem;
                SqlTestDb.CreateNew(serverType, doNotCleanupDb: true, databaseName: Common.PerfTestDatabaseName, query: Scripts.CreateDatabaseObjectsQuery);

                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    await testService.ConnectForQuery(serverType, Scripts.TestDbSimpleSelectQuery, queryTempFile.FilePath, Common.PerfTestDatabaseName);

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

                    await testService.RequestChangeTextDocumentNotification(changeParams);
                    await testService.ExecuteWithTimeout(timer, 60000, async () =>
                    {
                        var completeEvent = await testService.Driver.WaitForEvent(PublishDiagnosticsNotification.Type, 15000);
                        return completeEvent?.Diagnostics != null && completeEvent.Diagnostics.Length > 0;
                    });
                    await testService.Disconnect(queryTempFile.FilePath);
                }
            });
        }

        [Fact]
        [CreateTestDb(TestServerType.Azure)]
        public async Task BindingCacheColdAzureSimpleQuery()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.Azure;
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                {
                    await VerifyBindingLoadScenario(testService, serverType, Scripts.TestDbSimpleSelectQuery, false, timer);
                }
            });
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task BindingCacheColdOnPremSimpleQuery()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.OnPrem;
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                {
                    await VerifyBindingLoadScenario(testService, serverType, Scripts.TestDbSimpleSelectQuery, false, timer);
                }
            });
            
        }

        [Fact]
        [CreateTestDb(TestServerType.Azure)]
        public async Task BindingCacheWarmAzureSimpleQuery()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.Azure;
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    const string query = Scripts.TestDbSimpleSelectQuery;
                    await VerifyBindingLoadScenario(testService, serverType, query, true, timer);
                }
            });
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task BindingCacheWarmOnPremSimpleQuery()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.OnPrem;

                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    const string query = Scripts.TestDbSimpleSelectQuery;
                    await VerifyBindingLoadScenario(testService, serverType, query, true, timer);
                }
            });
        }

        [Fact]
        [CreateTestDb(TestServerType.Azure)]
        public async Task BindingCacheColdAzureComplexQuery()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.Azure;

                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                {
                    await VerifyBindingLoadScenario(testService, serverType, Scripts.TestDbComplexSelectQueries, false, timer);
                }
            });
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task BindingCacheColdOnPremComplexQuery()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.OnPrem;
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                {
                    await VerifyBindingLoadScenario(testService, serverType, Scripts.TestDbComplexSelectQueries, false, timer);
                }
            });
        }

        [Fact]
        [CreateTestDb(TestServerType.Azure)]
        public async Task BindingCacheWarmAzureComplexQuery()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                {
                    string query = Scripts.TestDbComplexSelectQueries;
                    const TestServerType serverType = TestServerType.Azure;
                    await VerifyBindingLoadScenario(testService, serverType, query, true, timer);
                }
            });
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task BindingCacheWarmOnPremComplexQuery()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                {
                    string query = Scripts.TestDbComplexSelectQueries;
                    const TestServerType serverType = TestServerType.OnPrem;
                    await VerifyBindingLoadScenario(testService, serverType, query, true, timer);
                }
            });
        }

        #region Private Helper Methods

        private async Task VerifyBindingLoadScenario(
            TestServiceDriverProvider testService, 
            TestServerType serverType, 
            string query, 
            bool preLoad,
            TestTimer timer,
            [CallerMemberName] string testName = "")
        {
            string databaseName = Common.PerfTestDatabaseName;
            if (preLoad)
            {
                await VerifyCompletationLoaded(testService, serverType, Scripts.TestDbSimpleSelectQuery, 
                    databaseName, null, testName: testName);
                Console.WriteLine("Intellisense cache loaded.");
            }
            await VerifyCompletationLoaded(testService, serverType, query, databaseName, 
                timer, testName: testName);
        }

        private async Task VerifyCompletationLoaded(
            TestServiceDriverProvider testService,
            TestServerType serverType,
            string query,
            string databaseName,
            TestTimer timer,
            string testName)
        {

            using (SelfCleaningTempFile testTempFile = new SelfCleaningTempFile())
            {
                testService.WriteToFile(testTempFile.FilePath, query);
                await testService.ConnectForQuery(serverType, query, testTempFile.FilePath, databaseName);
                await ValidateCompletionResponse(testService, testTempFile.FilePath, databaseName,
                    waitForIntelliSense: true, timer: timer, testName: testName);
                await testService.Disconnect(testTempFile.FilePath);
            }
        }

        private static async Task ValidateCompletionResponse(
            TestServiceDriverProvider testService, 
            string ownerUri, 
            string databaseName, 
            bool waitForIntelliSense,
            TestTimer timer = null,
            [CallerMemberName] string testName = "")
        {
            if (timer == null)
            {
                timer = new TestTimer { PrintResult = false };
            }
            bool isReady = !waitForIntelliSense;
            await testService.ExecuteWithTimeout(timer, 550000, async () =>
            {
                if (isReady)
                {
                    string query = Scripts.SelectQuery;
                    CompletionItem[] completions = await testService.RequestCompletion(ownerUri, query, 0, query.Length + 1);
                    return completions != null && 
                    (completions.Any(x => string.Compare(x.Label, databaseName, StringComparison.OrdinalIgnoreCase) == 0 || 
                    string.Compare(x.Label, $"[{databaseName}]", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(x.Label, $"\"{databaseName}\"", StringComparison.OrdinalIgnoreCase) == 0));
                }
                else
                {
                    var completeEvent = await testService.Driver.WaitForEvent(IntelliSenseReadyNotification.Type, 100000);
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
