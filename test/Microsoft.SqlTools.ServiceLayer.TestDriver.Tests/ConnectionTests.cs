//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    /// <summary>
    /// Language Service end-to-end integration tests
    /// </summary>
    public class ConnectionTest
    {
        /// <summary>
        /// Try to connect with invalid credentials
        /// </summary>
        [Fact]
        public async Task InvalidConnection()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                bool connected = await testService.Connect(queryTempFile.FilePath, InvalidConnectParams, 300000);
                Assert.False(connected, "Invalid connection is failed to connect");

                await testService.Connect(queryTempFile.FilePath, InvalidConnectParams, 300000);

                Thread.Sleep(1000);

                await testService.CancelConnect(queryTempFile.FilePath);

                await testService.Disconnect(queryTempFile.FilePath);
            }
        }

        /// <summary>
        /// Validate list databases request
        /// </summary>
        [Fact]
        public async Task ListDatabasesTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
            {
                bool connected = await testService.Connect(TestServerType.OnPrem, queryTempFile.FilePath);
                Assert.True(connected, "Connection successful");

                var listDatabaseResult = await testService.ListDatabases(queryTempFile.FilePath);
                Assert.True(listDatabaseResult.DatabaseNames.Length > 0);

                await testService.Disconnect(queryTempFile.FilePath);
            }
        }

        private static ConnectParams InvalidConnectParams
        {
            get
            {
                return new ConnectParams()
                {
                    Connection = new ConnectionDetails()
                    {
                        DatabaseName = "master",
                        ServerName = "localhost",
                        AuthenticationType = "SqlLogin",
                        UserName = "invalid",
                        Password = ".."
                    }
                };
            }
        }
    }
}
