//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
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
            using (TestServiceDriverProvier testService = new TestServiceDriverProvier())
            {
                const string query = Scripts.MasterBasicQuery;

                // Execute a query
                await testService.ConnectForQuery(serverType, query, queryTempFile.FilePath, SqlTestDb.MasterDatabaseName);
                await testService.RunQueryAndWaitToComplete(queryTempFile.FilePath, query);
                await testService.CalculateRunTime(() => testService.SaveAsCsv(queryTempFile.FilePath, outputTempFile.FilePath, 0, 0), true);
                await testService.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task TestSaveResultsToJsonTest()
        {
            TestServerType serverType = TestServerType.OnPrem;

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (SelfCleaningTempFile outputTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvier testService = new TestServiceDriverProvier())
            {
                const string query = Scripts.MasterBasicQuery;

                // Execute a query
                await testService.ConnectForQuery(serverType, query, queryTempFile.FilePath, SqlTestDb.MasterDatabaseName);
                await testService.RunQueryAndWaitToComplete(queryTempFile.FilePath, query);
                await testService.CalculateRunTime(() => testService.SaveAsJson(queryTempFile.FilePath, outputTempFile.FilePath, 0, 0), true);
                await testService.Disconnect(queryTempFile.FilePath);
            }
        }

    }
}
