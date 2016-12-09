//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Tests;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class SaveResultsTests
    {
        [Fact]
        public async Task TestSaveResultsToCsvTest()
        {
            TestServerType serverType = TestServerType.OnPrem;

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (SelfCleaningTempFile outputTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                const string query = Scripts.MasterBasicQuery;

                // Execute a query
                await Common.ConnectAsync(testHelper, serverType, query, queryTempFile.FilePath, Common.MasterDatabaseName);
                await testHelper.RunQuery(queryTempFile.FilePath, query);
                await Common.CalculateRunTime(() => testHelper.SaveAsCsv(queryTempFile.FilePath, outputTempFile.FilePath, 0, 0), true);
                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task TestSaveResultsToJsonTest()
        {
            TestServerType serverType = TestServerType.OnPrem;

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (SelfCleaningTempFile outputTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                const string query = Scripts.MasterBasicQuery;

                // Execute a query
                await Common.ConnectAsync(testHelper, serverType, query, queryTempFile.FilePath, Common.MasterDatabaseName);
                await testHelper.RunQuery(queryTempFile.FilePath, query);
                await Common.CalculateRunTime(() => testHelper.SaveAsJson(queryTempFile.FilePath, outputTempFile.FilePath, 0, 0), true);
                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

    }
}
