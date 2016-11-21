//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Tests;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class ConnectionTests
    {

        [Fact]
        public async Task ConnectAzureTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                const string query = Scripts.SimpleQuery;
                testHelper.WriteToFile(queryTempFile.FilePath, query);

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

                await testHelper.RequestOpenDocumentNotification(openParams);

                Thread.Sleep(500);
                var connected = await Common.CalculateRunTime(async () =>
                {
                    var connectParams = await testHelper.GetDatabaseConnectionAsync(TestServerType.Azure);
                    return await testHelper.Connect(queryTempFile.FilePath, connectParams);
                });
                Assert.True(connected, "Connection was not successful");
            }
        }

        [Fact]
        public async Task ConnectOnPremTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                const string query = Scripts.SimpleQuery;
                testHelper.WriteToFile(queryTempFile.FilePath, query);

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

                await testHelper.RequestOpenDocumentNotification(openParams);

                Thread.Sleep(500);
                var connected = await Common.CalculateRunTime(async () =>
                {
                    var connectParams = await testHelper.GetDatabaseConnectionAsync(TestServerType.OnPrem);
                    return await testHelper.Connect(queryTempFile.FilePath, connectParams);
                });
                Assert.True(connected, "Connection was not successful");
            }
        }

        [Fact]
        public async Task DisconnectTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                await Common.ConnectAsync(testHelper, TestServerType.OnPrem, Scripts.SimpleQuery, queryTempFile.FilePath);
                Thread.Sleep(1000);
                var connected = await Common.CalculateRunTime(() => testHelper.Disconnect(queryTempFile.FilePath));
                Assert.True(connected);
            }
        }

    }
}
