

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

namespace Microsoft.SqlTools.ServiceLayer.PerfTests.Tests
{
    public class IntellisenseTests
    {
        [Fact]
        public async Task HoverTestOnPrem()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                const string query = Scripts.SimpleQuery;
                await Common.ConnectAsync(testBase, TestServerType.OnPrem, query, queryFile.FilePath);
                Hover hover = await Common.CalculateRunTime(() => testBase.RequestHover(queryFile.FilePath, query, 0, 15));
                Assert.NotNull(hover);
                await testBase.Disconnect(queryFile.FilePath);
            }
        }

        [Fact]
        public async Task SuggestionsTest()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                const string query = Scripts.SimpleQuery;
                await Common.ConnectAsync(testBase, TestServerType.OnPrem, query, queryFile.FilePath);
                await ValidateCompletionResponse(testBase, queryFile.FilePath, query, null);
                await ValidateCompletionResponse(testBase, queryFile.FilePath, query);
                await testBase.Disconnect(queryFile.FilePath);
            }
        }

        [Fact]
        public async Task DiagnosticsTests()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                await Common.ConnectAsync(testBase, TestServerType.OnPrem, Scripts.SimpleQuery, queryFile.FilePath);

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
                        Uri = queryFile.FilePath
                    }
                };

                TestTimer timer = new TestTimer();
                await testBase.RequestChangeTextDocumentNotification(changeParams);
                await Common.ExecuteWithTimeout(timer, 60000, async () =>
                {
                    var completeEvent = await testBase.Driver.WaitForEvent(PublishDiagnosticsNotification.Type, 15000);
                    return completeEvent?.Diagnostics != null && completeEvent.Diagnostics.Length > 0;
                });
                await testBase.Disconnect(queryFile.FilePath);
            }
        }

        [Fact]
        public async Task BindingCacheColdAzureSimpleQuery()
        {
            using (TestBase testBase = new TestBase())
            {
                await VerifyBindingLoadScenario(testBase, TestServerType.Azure, Scripts.SimpleQuery);
            }
        }

        [Fact]
        public async Task BindingCacheColdOnPremSimpleQuery()
        {
            using (TestBase testBase = new TestBase())
            {
                await VerifyBindingLoadScenario(testBase, TestServerType.OnPrem, Scripts.SimpleQuery);
            }
        }

        [Fact]
        public async Task BindingCacheWarmAzureSimpleQuery()
        {
            using (TestBase testBase = new TestBase())
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            {
                const string query = Scripts.SimpleQuery;
                const TestServerType serverType = TestServerType.Azure;
                await Common.ConnectAsync(testBase, serverType, query, queryFile.FilePath);
                Thread.Sleep(10000);
                await VerifyBindingLoadScenario(testBase, serverType, query);
            }
        }

        [Fact]
        public async Task BindingCacheWarmOnPremSimpleQuery()
        {
            using (TestBase testBase = new TestBase())
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            {
                const string query = Scripts.SimpleQuery;
                const TestServerType serverType = TestServerType.OnPrem;
                await Common.ConnectAsync(testBase, serverType, query, queryFile.FilePath);
                Thread.Sleep(10000);
                await VerifyBindingLoadScenario(testBase, serverType, query);
            }
        }

        [Fact]
        public async Task BindingCacheColdAzureComplexQuery()
        {
            using (TestBase testBase = new TestBase())
            {
                await VerifyBindingLoadScenario(testBase, TestServerType.Azure, Scripts.ComplexQuery);
            }
        }

        [Fact]
        public async Task BindingCacheColdOnPremComplexQuery()
        {
            using (TestBase testBase = new TestBase())
            {
                await VerifyBindingLoadScenario(testBase, TestServerType.OnPrem, Scripts.ComplexQuery);
            }
        }

        [Fact]
        public async Task BindingCacheWarmAzureComplexQuery()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                string query = Scripts.ComplexQuery;
                const TestServerType serverType = TestServerType.Azure;
                await Common.ConnectAsync(testBase, serverType, query, queryFile.FilePath);
                Thread.Sleep(10000);
                await VerifyBindingLoadScenario(testBase, serverType, query);
            }
        }

        [Fact]
        public async Task BindingCacheWarmOnPremComplexQuery()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                string query = Scripts.ComplexQuery;
                const TestServerType serverType = TestServerType.OnPrem;
                await Common.ConnectAsync(testBase, serverType, query, queryFile.FilePath);
                Thread.Sleep(10000);
                await VerifyBindingLoadScenario(testBase, serverType, query);
            }
        }

        #region Private Helper Methods

        private static async Task VerifyBindingLoadScenario(TestBase testBase, TestServerType serverType, string query, [CallerMemberName] string testName = "")
        {
            using(SelfCleaningFile testFile = new SelfCleaningFile()) { 
                testBase.WriteToFile(testFile.FilePath, query);
                await Common.ConnectAsync(testBase, serverType, query, testFile.FilePath);
                await ValidateCompletionResponse(testBase, testFile.FilePath, query, testName);
                await testBase.Disconnect(testFile.FilePath);
            }
        }

        private static async Task ValidateCompletionResponse(TestBase testBase, string ownerUri, string query, [CallerMemberName] string testName="")
        {
            TestTimer timer = new TestTimer();
            await Common.ExecuteWithTimeout(timer, 60000, async () =>
            {
                CompletionItem[] completions = await testBase.RequestCompletion(ownerUri, query, 0, 15);
                return completions != null && completions.Any(x => x.Label == "master");
            }, testName:testName);
        }

        #endregion
    }
}
