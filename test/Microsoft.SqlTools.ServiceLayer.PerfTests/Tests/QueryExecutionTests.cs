using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Tests;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests.Tests
{
    public class QueryExecutionTests
    {
        public string TestName { get; set; }

        [Fact]
        public async Task QueryResultSummaryOnPremTest()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                string scenarioName = string.IsNullOrEmpty(TestName) ? "Basic Query Result On-Prem" : TestName;
                const string query = Scripts.SimpleQuery;

                await Common.ConnectAsync(testBase, TestServerType.OnPrem, query, queryFile.FilePath);
                var queryResult = await Common.CalculateRunTime(scenarioName, 
                    () => testBase.RunQuery(queryFile.FilePath, query));

                Assert.NotNull(queryResult);
                Assert.True(queryResult.BatchSummaries.Any(x => x.ResultSetSummaries.Any(r => r.RowCount > 0)));

                await testBase.Disconnect(queryFile.FilePath);
            }
        }

        [Fact]
        public async Task QueryResultFirstOnPremTest()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                string scenarioName = string.IsNullOrEmpty(TestName) ? "Basic Query Result First Rows On-Prem" : TestName;
                const string query = Scripts.SimpleQuery;

                await Common.ConnectAsync(testBase, TestServerType.OnPrem, query, ownerUri);

                var queryResult = await Common.CalculateRunTime(scenarioName, async () =>
                {
                    await testBase.RunQuery(queryFile.FilePath, query);
                    return await testBase.ExecuteSubset(queryFile.FilePath, 0, 0, 0, 100);
                });

                Assert.NotNull(queryResult);
                Assert.NotNull(queryResult.ResultSubset);
                Assert.True(queryResult.ResultSubset.Rows.Any());

                await testBase.Disconnect(queryFile.FilePath);
            }
        }

        [Fact]
        public async Task CancelQueryOnPremTest()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                string scenarioName = string.IsNullOrEmpty(TestName) ? "Cancel Query On-Prem" : TestName;

                await Common.ConnectAsync(testBase, TestServerType.OnPrem, Scripts.DelayQuery, queryFile.FilePath);
                var queryParams = new QueryExecuteParams
                {
                    OwnerUri = queryFile.FilePath,
                    QuerySelection = null
                };

                var result = await testBase.Driver.SendRequest(QueryExecuteRequest.Type, queryParams);
                if (result != null && string.IsNullOrEmpty(result.Messages))
                {
                    TestTimer timer = new TestTimer();

                    while (true)
                    {
                        var queryTask = await testBase.CancelQuery(queryFile.FilePath);
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

                await testBase.Disconnect(queryFile.FilePath);
            }
        }
    }
}
