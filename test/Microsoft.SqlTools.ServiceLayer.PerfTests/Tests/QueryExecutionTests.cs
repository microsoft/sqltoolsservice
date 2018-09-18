//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class QueryExecutionTests
    {
        [Fact]
        public async Task QueryResultSummaryOnPremTest()
        {
            await QueryResultSummaryOnPremTest(TestServerType.OnPrem, Scripts.MasterBasicQuery);
        }

        [Fact]
        public async Task QueryResultFirstOnPremTest()
        {
            await QueryResultFirstOnPremTest(TestServerType.OnPrem, Scripts.MasterBasicQuery);
        }

        [Fact]
        public async Task LongQueryResultSummaryOnPremTest()
        {
            await QueryResultSummaryOnPremTest(TestServerType.OnPrem, Scripts.MasterLongQuery);
        }

        [Fact]
        public async Task LongQueryResultFirstOnPremTest()
        {
            await QueryResultFirstOnPremTest(TestServerType.OnPrem, Scripts.MasterLongQuery);
        }

        [Fact]
        public async Task QueryResultSummaryOnAzureTest()
        {
            await QueryResultSummaryOnPremTest(TestServerType.Azure, Scripts.MasterBasicQuery);
        }

        [Fact]
        public async Task QueryResultFirstOnAzureTest()
        {
            await QueryResultFirstOnPremTest(TestServerType.Azure, Scripts.MasterBasicQuery);
        }

        [Fact]
        public async Task LongQueryResultSummaryOnAzureTest()
        {
            await QueryResultSummaryOnPremTest(TestServerType.Azure, Scripts.MasterLongQuery);
        }

        [Fact]
        public async Task LongQueryResultFirstOnAzureTest()
        {
            await QueryResultFirstOnPremTest(TestServerType.Azure, Scripts.MasterLongQuery);
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task CancelQueryOnPremTest()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.OnPrem;

                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                {
                    await testService.ConnectForQuery(serverType, Scripts.DelayQuery, queryTempFile.FilePath, Common.PerfTestDatabaseName);
                    var queryParams = new ExecuteDocumentSelectionParams
                    {
                        OwnerUri = queryTempFile.FilePath,
                        QuerySelection = null
                    };

                    testService.WriteToFile(queryTempFile.FilePath, Scripts.MasterLongQuery);

                    var result = await testService.Driver.SendRequest(ExecuteDocumentSelectionRequest.Type, queryParams);
                    if (result != null)
                    {
                        await testService.ExecuteWithTimeout(timer, 100000, async () =>
                        {
                            var cancelQueryResult = await testService.CancelQuery(queryTempFile.FilePath);
                            return true;
                        }, TimeSpan.FromMilliseconds(10));
                    }
                    else
                    {
                        Assert.True(false, "Failed to run the query");

                        await testService.Disconnect(queryTempFile.FilePath);
                    }
                }
            });

        }

        private async Task QueryResultSummaryOnPremTest(TestServerType serverType, string query, [CallerMemberName] string testName = "")
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                {
                    await testService.ConnectForQuery(serverType, query, queryTempFile.FilePath, SqlTestDb.MasterDatabaseName);
                    testService.WriteToFile(queryTempFile.FilePath, query);
                    var queryResult = await testService.CalculateRunTime(
                        () => testService.RunQueryAndWaitToComplete(queryTempFile.FilePath, 50000),
                        timer);

                    Assert.NotNull(queryResult);
                    Assert.True(queryResult.BatchSummaries.Any(x => x.ResultSetSummaries.Any(r => r.RowCount > 0)));

                    await testService.Disconnect(queryTempFile.FilePath);
                }
            }, testName);
        }

        private async Task QueryResultFirstOnPremTest(TestServerType serverType, string query, [CallerMemberName] string testName = "")
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                {
                    await testService.ConnectForQuery(serverType, query, queryTempFile.FilePath, SqlTestDb.MasterDatabaseName);
                    testService.WriteToFile(queryTempFile.FilePath, query);
                    await testService.RunQueryAndWaitToStart(queryTempFile.FilePath, 50000);
                    await testService.ExecuteWithTimeout(timer, 500000, async () =>
                    {
                        var queryResult = await testService.ExecuteSubset(queryTempFile.FilePath, 0, 0, 0, 50);
                        if (queryResult != null)
                        {
                            Assert.NotNull(queryResult);
                            Assert.NotNull(queryResult.ResultSubset);
                            Assert.True(queryResult.ResultSubset.Rows.Any());
                        }
                        return queryResult != null;
                    }, TimeSpan.FromMilliseconds(10));

                    await testService.Disconnect(queryTempFile.FilePath);
                }
            }, testName);
        }
    }
}
