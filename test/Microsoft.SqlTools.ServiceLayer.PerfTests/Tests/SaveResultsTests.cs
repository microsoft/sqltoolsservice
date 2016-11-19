//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Tests;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests.Tests
{
    public class SaveResultsTests
    {
        [Fact]
        public async Task TestSaveResultsToCsvTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (SelfCleaningTempFile outputTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                const string query = Scripts.SimpleQuery;

                // Execute a query
                await Common.ConnectAsync(testHelper, TestServerType.OnPrem, query, queryTempFile.FilePath);
                await testHelper.RunQuery(queryTempFile.FilePath, query);
                await Common.CalculateRunTime(() => testHelper.SaveAsCsv(queryTempFile.FilePath, outputTempFile.FilePath, 0, 0));
                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task TestSaveResultsToJsonTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (SelfCleaningTempFile outputTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                const string query = Scripts.SimpleQuery;
                const TestServerType serverType = TestServerType.OnPrem;

                // Execute a query
                await Common.ConnectAsync(testHelper, serverType, query, queryTempFile.FilePath);
                await testHelper.RunQuery(queryTempFile.FilePath, query);
                await Common.CalculateRunTime(() => testHelper.SaveAsJson(queryTempFile.FilePath, outputTempFile.FilePath, 0, 0));
                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

    }
}
