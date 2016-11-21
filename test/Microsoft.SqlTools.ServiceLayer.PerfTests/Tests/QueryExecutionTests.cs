//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Tests;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class QueryExecutionTests
    {
        [Fact]
        public async Task QueryResultSummaryOnPremTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                const string query = Scripts.SimpleQuery;

                await Common.ConnectAsync(testHelper, TestServerType.OnPrem, query, queryTempFile.FilePath);
                var queryResult = await Common.CalculateRunTime(() => testHelper.RunQuery(queryTempFile.FilePath, query));

                Assert.NotNull(queryResult);
                Assert.True(queryResult.BatchSummaries.Any(x => x.ResultSetSummaries.Any(r => r.RowCount > 0)));

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task QueryResultFirstOnPremTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                const string query = Scripts.SimpleQuery;

                await Common.ConnectAsync(testHelper, TestServerType.OnPrem, query, queryTempFile.FilePath);

                var queryResult = await Common.CalculateRunTime(async () =>
                {
                    await testHelper.RunQuery(queryTempFile.FilePath, query);
                    return await testHelper.ExecuteSubset(queryTempFile.FilePath, 0, 0, 0, 100);
                });

                Assert.NotNull(queryResult);
                Assert.NotNull(queryResult.ResultSubset);
                Assert.True(queryResult.ResultSubset.Rows.Any());

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task CancelQueryOnPremTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                await Common.ConnectAsync(testHelper, TestServerType.OnPrem, Scripts.DelayQuery, queryTempFile.FilePath);
                var queryParams = new QueryExecuteParams
                {
                    OwnerUri = queryTempFile.FilePath,
                    QuerySelection = null
                };

                var result = await testHelper.Driver.SendRequest(QueryExecuteRequest.Type, queryParams);
                if (result != null && string.IsNullOrEmpty(result.Messages))
                {
                    TestTimer timer = new TestTimer();
                    await Common.ExecuteWithTimeout(timer, 100000,
                        async () => await testHelper.CancelConnect(queryTempFile.FilePath), TimeSpan.FromMilliseconds(10));
                }
                else
                {
                    Assert.True(false, "Failed to run the query");
                }

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }
    }
}
