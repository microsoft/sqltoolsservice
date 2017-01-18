//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class QueryExecutionTests
    {
        [Fact]
        public async Task QueryResultSummaryOnPremTest()
        {
            TestServerType serverType = TestServerType.OnPrem;

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                const string query = Scripts.MasterBasicQuery;

                await testService.ConnectForQuery(serverType, query, queryTempFile.FilePath, SqlTestDb.MasterDatabaseName);
                var queryResult = await testService.CalculateRunTime(() => testService.RunQueryAndWaitToComplete(queryTempFile.FilePath, query), true);

                Assert.NotNull(queryResult);
                Assert.True(queryResult.BatchSummaries.Any(x => x.ResultSetSummaries.Any(r => r.RowCount > 0)));

                await testService.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task QueryResultFirstOnPremTest()
        {
            TestServerType serverType = TestServerType.OnPrem;

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                const string query = Scripts.MasterBasicQuery;

                await testService.ConnectForQuery(serverType, query, queryTempFile.FilePath, SqlTestDb.MasterDatabaseName);

                var queryResult = await testService.CalculateRunTime(async () =>
                {
                    await testService.RunQueryAndWaitToComplete(queryTempFile.FilePath, query);
                    return await testService.ExecuteSubset(queryTempFile.FilePath, 0, 0, 0, 100);
                }, true);

                Assert.NotNull(queryResult);
                Assert.NotNull(queryResult.ResultSubset);
                Assert.True(queryResult.ResultSubset.Rows.Any());

                await testService.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task CancelQueryOnPremTest()
        {
            TestServerType serverType = TestServerType.OnPrem;

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                await testService.ConnectForQuery(serverType, Scripts.DelayQuery, queryTempFile.FilePath, Common.PerfTestDatabaseName);
                var queryParams = new QueryExecuteParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    QuerySelection = null
                };

                var result = await testService.Driver.SendRequest(QueryExecuteRequest.Type, queryParams);
                if (result != null)
                {
                    TestTimer timer = new TestTimer() { PrintResult = true };
                    await testService.ExecuteWithTimeout(timer, 100000, async () => 
                    {
                        var cancelQueryResult = await testService.CancelQuery(queryTempFile.FilePath);
                        return true;
                    },  TimeSpan.FromMilliseconds(10));
                }
                else
                {
                    Assert.True(false, "Failed to run the query");
                }

                await testService.Disconnect(queryTempFile.FilePath);
            }
        }
    }
}
