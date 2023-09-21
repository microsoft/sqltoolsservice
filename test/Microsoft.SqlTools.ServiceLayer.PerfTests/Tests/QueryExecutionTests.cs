﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class QueryExecutionTests
    {
        [Test]
        public async Task QueryResultSummaryOnPremTest()
        {
            await QueryResultSummaryOnPremTestCore(TestServerType.OnPrem, Scripts.MasterBasicQuery);
        }

        [Test]
        public async Task QueryResultFirstOnPremTest()
        {
            await QueryResultFirstOnPremTestCore(TestServerType.OnPrem, Scripts.MasterBasicQuery);
        }

        [Test]
        public async Task LongQueryResultSummaryOnPremTest()
        {
            await QueryResultSummaryOnPremTestCore(TestServerType.OnPrem, Scripts.MasterLongQuery);
        }

        [Test]
        public async Task LongQueryResultFirstOnPremTest()
        {
            await QueryResultFirstOnPremTestCore(TestServerType.OnPrem, Scripts.MasterLongQuery);
        }

        [Test]
        public async Task QueryResultSummaryOnAzureTest()
        {
            await QueryResultSummaryOnPremTestCore(TestServerType.Azure, Scripts.MasterBasicQuery);
        }

        [Test]
        public async Task QueryResultFirstOnAzureTest()
        {
            await QueryResultFirstOnPremTestCore(TestServerType.Azure, Scripts.MasterBasicQuery);
        }

        [Test]
        public async Task LongQueryResultSummaryOnAzureTest()
        {
            await QueryResultSummaryOnPremTestCore(TestServerType.Azure, Scripts.MasterLongQuery);
        }

        [Test]
        public async Task LongQueryResultFirstOnAzureTest()
        {
            await QueryResultFirstOnPremTestCore(TestServerType.Azure, Scripts.MasterLongQuery);
        }

        [Test]
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
                        Assert.Fail("Failed to run the query");

                        await testService.Disconnect(queryTempFile.FilePath);
                    }
                }
            });

        }

        private async Task QueryResultSummaryOnPremTestCore(TestServerType serverType, string query, [CallerMemberName] string testName = "")
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

        private async Task QueryResultFirstOnPremTestCore(TestServerType serverType, string query, [CallerMemberName] string testName = "")
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
