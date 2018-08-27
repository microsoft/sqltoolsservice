//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class ConnectionTests
    {

        [Fact]
        [CreateTestDb(TestServerType.Azure)]
        public async Task ConnectAzureTest()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.Azure;
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                {
                    const string query = Scripts.TestDbSimpleSelectQuery;
                    testService.WriteToFile(queryTempFile.FilePath, query);

                    DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification
                    {
                        TextDocument = new TextDocumentItem
                        {
                            Uri = queryTempFile.FilePath,
                            LanguageId = "enu",
                            Version = 1,
                            Text = query
                        }
                    };

                    await testService.RequestOpenDocumentNotification(openParams);

                    Thread.Sleep(500);
                    var connected = await testService.CalculateRunTime(async () =>
                    {
                        var connectParams = testService.GetConnectionParameters(serverType, Common.PerfTestDatabaseName);
                        return await testService.Connect(queryTempFile.FilePath, connectParams);
                    }, timer);
                    Assert.True(connected, "Connection was not successful");
                }
            });
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task ConnectOnPremTest()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.OnPrem;

                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                {
                    const string query = Scripts.TestDbSimpleSelectQuery;
                    testService.WriteToFile(queryTempFile.FilePath, query);

                    DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification
                    {
                        TextDocument = new TextDocumentItem
                        {
                            Uri = queryTempFile.FilePath,
                            LanguageId = "enu",
                            Version = 1,
                            Text = query
                        }
                    };

                    await testService.RequestOpenDocumentNotification(openParams);

                    Thread.Sleep(500);
                    var connected = await testService.CalculateRunTime(async () =>
                    {
                        var connectParams = testService.GetConnectionParameters(serverType, Common.PerfTestDatabaseName);
                        return await testService.Connect(queryTempFile.FilePath, connectParams);
                    }, timer);
                    Assert.True(connected, "Connection was not successful");
                }
            });
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task DisconnectTest()
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                TestServerType serverType = TestServerType.OnPrem;

                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                {
                    await testService.ConnectForQuery(serverType, Scripts.TestDbSimpleSelectQuery, queryTempFile.FilePath, Common.PerfTestDatabaseName);
                    Thread.Sleep(1000);
                    var connected = await testService.CalculateRunTime(() => testService.Disconnect(queryTempFile.FilePath), timer);
                    Assert.True(connected);
                }
            });
        }

    }
}
