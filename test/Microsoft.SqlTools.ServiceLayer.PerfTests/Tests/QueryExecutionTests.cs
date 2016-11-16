using System;
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
        [Fact]
        public async Task QueryResultSummaryOnPremTest()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                const string query = Scripts.SimpleQuery;

                await Common.ConnectAsync(testBase, TestServerType.OnPrem, query, queryFile.FilePath);
                var queryResult = await Common.CalculateRunTime(() => testBase.RunQuery(queryFile.FilePath, query));

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
                const string query = Scripts.SimpleQuery;

                await Common.ConnectAsync(testBase, TestServerType.OnPrem, query, queryFile.FilePath);

                var queryResult = await Common.CalculateRunTime(async () =>
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
                    await Common.ExecuteWithTimeout(timer, 100000,
                        async () => await testBase.CancelConnect(queryFile.FilePath), TimeSpan.FromMilliseconds(10));
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
