//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
        public async Task HoverTestOnPrem()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                const string query = Scripts.SimpleQuery;
                await Common.ConnectAsync(testHelper, TestServerType.OnPrem, query, queryTempFile.FilePath);
                Hover hover = await Common.CalculateRunTime(() => testHelper.RequestHover(queryTempFile.FilePath, query, 0, 15));
                Assert.NotNull(hover);
                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task SuggestionsTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                const string query = Scripts.SimpleQuery;
                await Common.ConnectAsync(testHelper, TestServerType.OnPrem, query, queryTempFile.FilePath);
                await ValidateCompletionResponse(testHelper, queryTempFile.FilePath, query, null);
                await ValidateCompletionResponse(testHelper, queryTempFile.FilePath, query);
                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task DiagnosticsTests()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                await Common.ConnectAsync(testHelper, TestServerType.OnPrem, Scripts.SimpleQuery, queryTempFile.FilePath);

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

                TestTimer timer = new TestTimer();
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
        public async Task BindingCacheColdAzureSimpleQuery()
        {
            using (TestHelper testHelper = new TestHelper())
            {
                await VerifyBindingLoadScenario(testHelper, TestServerType.Azure, Scripts.SimpleQuery);
            }
        }

        [Fact]
        public async Task BindingCacheColdOnPremSimpleQuery()
        {
            using (TestHelper testHelper = new TestHelper())
            {
                await VerifyBindingLoadScenario(testHelper, TestServerType.OnPrem, Scripts.SimpleQuery);
            }
        }

        [Fact]
        public async Task BindingCacheWarmAzureSimpleQuery()
        {
            using (TestHelper testHelper = new TestHelper())
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                const string query = Scripts.SimpleQuery;
                const TestServerType serverType = TestServerType.Azure;
                await Common.ConnectAsync(testHelper, serverType, query, queryTempFile.FilePath);
                Thread.Sleep(10000);
                await VerifyBindingLoadScenario(testHelper, serverType, query);
            }
        }

        [Fact]
        public async Task BindingCacheWarmOnPremSimpleQuery()
        {
            using (TestHelper testHelper = new TestHelper())
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                const string query = Scripts.SimpleQuery;
                const TestServerType serverType = TestServerType.OnPrem;
                await Common.ConnectAsync(testHelper, serverType, query, queryTempFile.FilePath);
                Thread.Sleep(10000);
                await VerifyBindingLoadScenario(testHelper, serverType, query);
            }
        }

        [Fact]
        public async Task BindingCacheColdAzureComplexQuery()
        {
            using (TestHelper testHelper = new TestHelper())
            {
                await VerifyBindingLoadScenario(testHelper, TestServerType.Azure, Scripts.ComplexQuery);
            }
        }

        [Fact]
        public async Task BindingCacheColdOnPremComplexQuery()
        {
            using (TestHelper testHelper = new TestHelper())
            {
                await VerifyBindingLoadScenario(testHelper, TestServerType.OnPrem, Scripts.ComplexQuery);
            }
        }

        [Fact]
        public async Task BindingCacheWarmAzureComplexQuery()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                string query = Scripts.ComplexQuery;
                const TestServerType serverType = TestServerType.Azure;
                await Common.ConnectAsync(testHelper, serverType, query, queryTempFile.FilePath);
                Thread.Sleep(10000);
                await VerifyBindingLoadScenario(testHelper, serverType, query);
            }
        }

        [Fact]
        public async Task BindingCacheWarmOnPremComplexQuery()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                string query = Scripts.ComplexQuery;
                const TestServerType serverType = TestServerType.OnPrem;
                await Common.ConnectAsync(testHelper, serverType, query, queryTempFile.FilePath);
                Thread.Sleep(10000);
                await VerifyBindingLoadScenario(testHelper, serverType, query);
            }
        }

        #region Private Helper Methods

        private static async Task VerifyBindingLoadScenario(TestHelper testHelper, TestServerType serverType, string query, [CallerMemberName] string testName = "")
        {
            using(SelfCleaningTempFile testTempFile = new SelfCleaningTempFile()) { 
                testHelper.WriteToFile(testTempFile.FilePath, query);
                await Common.ConnectAsync(testHelper, serverType, query, testTempFile.FilePath);
                await ValidateCompletionResponse(testHelper, testTempFile.FilePath, query, testName);
                await testHelper.Disconnect(testTempFile.FilePath);
            }
        }

        private static async Task ValidateCompletionResponse(TestHelper testHelper, string ownerUri, string query, [CallerMemberName] string testName="")
        {
            TestTimer timer = new TestTimer();
            await Common.ExecuteWithTimeout(timer, 500000, async () =>
            {
                CompletionItem[] completions = await testHelper.RequestCompletion(ownerUri, query, 0, 15);
                return completions != null && completions.Any(x => x.Label == "master");
            }, testName:testName);
        }

        #endregion
    }
}
