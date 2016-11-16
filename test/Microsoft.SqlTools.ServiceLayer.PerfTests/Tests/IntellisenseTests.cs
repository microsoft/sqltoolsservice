

using System.Linq;
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
        public string TestName { get; set; }

        [Fact]
        public async Task HoverTestOnPrem()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                string scenarioName = string.IsNullOrEmpty(TestName) ? "Hover" : TestName;
                const string query = Scripts.SimpleQuery;
                await Common.ConnectAsync(testBase, TestServerType.OnPrem, query, queryFile.FilePath);
                Hover hover = await Common.CalculateRunTime(scenarioName, 
                    () => testBase.RequestHover(queryFile.FilePath, query, 0, 15));
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
                string scenarioName = string.IsNullOrEmpty(TestName) ? "Suggestions" : TestName;
                string query = Scripts.SimpleQuery;
                await Common.ConnectAsync(testBase, TestServerType.OnPrem, query, queryFile.FilePath);
                await ValidateCompletionResponse(queryFile.FilePath, query, null);
                await ValidateCompletionResponse(queryFile.FilePath, query, scenarioName);
                await testBase.Disconnect(queryFile.FilePath);
            }
        }

        [Fact]
        public async Task DiagnosticsTests()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                string scenarioName = string.IsNullOrEmpty(TestName) ? "Diagnostics" : TestName;
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
                await Common.ExecuteWithTimeout(timer, 60000, scenarioName, async () =>
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
                string scenarioName = string.IsNullOrEmpty(TestName) ? "[Simple query][Cold][SQL DB] Binding cache" : TestName;
                await VerifyBindingLoadScenario(testBase, TestServerType.Azure, Scripts.SimpleQuery, scenarioName);
            }
        }

        [Fact]
        public async Task BindingCacheColdOnPremSimpleQuery()
        {
            using (TestBase testBase = new TestBase())
            {
                string scenarioName = string.IsNullOrEmpty(TestName) ? "[Simple query][Cold][On-Prem] Binding cache" : TestName;
                await VerifyBindingLoadScenario(testBase, TestServerType.OnPrem, Scripts.SimpleQuery, scenarioName);
            }
        }

        [Fact]
        public async Task BindingCacheWarmAzureSimpleQuery()
        {
            using (TestBase testBase = new TestBase())
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            {
                string scenarioName = string.IsNullOrEmpty(TestName) ? "[Simple query][Warm][SQL DB] Binding cache" : TestName;
                const string query = Scripts.SimpleQuery;
                const TestServerType serverType = TestServerType.Azure;
                await Common.ConnectAsync(testBase, serverType, query, queryFile.FilePath);
                Thread.Sleep(10000);
                await VerifyBindingLoadScenario(testBase, serverType, query, scenarioName);
            }
        }

        [Fact]
        public async Task BindingCacheWarmOnPremSimpleQuery()
        {
            using (TestBase testBase = new TestBase())
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            {
                string scenarioName = string.IsNullOrEmpty(TestName)? "[Simple query][Warm][On-Prem] Binding cache" : TestName;
                const string query = Scripts.SimpleQuery;
                const TestServerType serverType = TestServerType.OnPrem;
                await Common.ConnectAsync(testBase, serverType, query, queryFile.FilePath);
                Thread.Sleep(10000);
                await VerifyBindingLoadScenario(testBase, serverType, query, scenarioName);
            }
        }

        [Fact]
        public async Task BindingCacheColdAzureComplexQuery()
        {
            using (TestBase testBase = new TestBase())
            {
                string scenarioName = string.IsNullOrEmpty(TestName) ? "[Complex query][Cold][SQL DB] Binding cache" : TestName;
                await VerifyBindingLoadScenario(testBase, TestServerType.Azure, Scripts.ComplexQuery, scenarioName);
            }
        }

        [Fact]
        public async Task BindingCacheColdOnPremComplexQuery()
        {
            using (TestBase testBase = new TestBase())
            {
                string scenarioName = string.IsNullOrEmpty(TestName) ? "[Complex query][Cold][On-Prem] Binding cache" : TestName;
                await VerifyBindingLoadScenario(testBase, TestServerType.OnPrem, Scripts.ComplexQuery, scenarioName);
            }
        }

        [Fact]
        public async Task BindingCacheWarmAzureComplexQuery()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                string scenarioName = string.IsNullOrEmpty(TestName) ? "[Complex query][Warm][SQL DB] Binding cache" : TestName;
                string query = Scripts.ComplexQuery;
                const TestServerType serverType = TestServerType.Azure;
                await Common.ConnectAsync(testBase, serverType, query, queryFile.FilePath);
                Thread.Sleep(100000);
                await VerifyBindingLoadScenario(serverType, query, scenarioName);
            }
        }

        [Fact]
        public async Task BindingCacheWarmOnPremComplexQuery()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                string scenarioName = string.IsNullOrEmpty(TestName) ? "[Complex query][Warm][On-Prem] Binding cache" : TestName;
                string query = Scripts.ComplexQuery;
                const TestServerType serverType = TestServerType.OnPrem;
                await Common.ConnectAsync(testBase, serverType, query, queryFile.FilePath);
                Thread.Sleep(10000);
                await VerifyBindingLoadScenario(serverType, query, scenarioName);
            }
        }

        #region Private Helper Methods

        private static async Task VerifyBindingLoadScenario(TestBase testBase, TestServerType serverType, string query, string testName = null)
        {
            using(SelfCleaningFile testFile = new SelfCleaningFile()) { 
                testBase.WriteToFile(testFile.FilePath, query);
                await Common.ConnectAsync(testBase, serverType, query, testFile.FilePath);
                await ValidateCompletionResponse(testBase, testFile.FilePath, query, testName);
                await testBase.Disconnect(testFile.FilePath);
            }
        }

        private static async Task ValidateCompletionResponse(TestBase testBase, string ownerUri, string query, string testName)
        {
            TestTimer timer = new TestTimer();
            await Common.ExecuteWithTimeout(timer, 60000, testName, async () =>
            {
                CompletionItem[] completions = await testBase.RequestCompletion(ownerUri, query, 0, 15);
                return completions != null && completions.Any(x => x.Label == "master");
            });
        }

        #endregion
    }
}
