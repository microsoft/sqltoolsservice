﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class SaveResultsTests
    {
        [Test]
        public async Task TestSaveResultsToCsvTest()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.OnPrem;

                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                using (SelfCleaningTempFile outputTempFile = new SelfCleaningTempFile())
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                {
                    const string query = Scripts.MasterBasicQuery;

                    // Execute a query
                    await testService.ConnectForQuery(serverType, query, queryTempFile.FilePath, SqlTestDb.MasterDatabaseName);
                    await testService.RunQueryAndWaitToComplete(queryTempFile.FilePath, query);
                    await testService.CalculateRunTime(() => testService.SaveAsCsv(queryTempFile.FilePath, outputTempFile.FilePath, 0, 0), timer);
                    await testService.Disconnect(queryTempFile.FilePath);
                }
            });
        }

        [Test]
        public async Task TestSaveResultsToJsonTest()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.OnPrem;

                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                using (SelfCleaningTempFile outputTempFile = new SelfCleaningTempFile())
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                {
                    const string query = Scripts.MasterBasicQuery;

                    // Execute a query
                    await testService.ConnectForQuery(serverType, query, queryTempFile.FilePath, SqlTestDb.MasterDatabaseName);
                    await testService.RunQueryAndWaitToComplete(queryTempFile.FilePath, query);
                    await testService.CalculateRunTime(() => testService.SaveAsJson(queryTempFile.FilePath, outputTempFile.FilePath, 0, 0), timer);
                    await testService.Disconnect(queryTempFile.FilePath);
                }
            });
        }

    }
}
