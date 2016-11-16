//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Xunit;

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
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                bool connected = await testBase.Connect(queryFile.FilePath, ConnectionTestUtils.InvalidConnection, 300000);
                Assert.False(connected, "Invalid connection is failed to connect");

                await testBase.Connect(queryFile.FilePath, ConnectionTestUtils.InvalidConnection, 300000);

                Thread.Sleep(1000);

                await testBase.CancelConnect(queryFile.FilePath);

                await testBase.Disconnect(queryFile.FilePath);
            }
        }

        /// <summary>
        /// Validate list databases request
        /// </summary>
        [Fact]
        public async Task ListDatabasesTest()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                bool connected = await testBase.Connect(queryFile.FilePath, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection successful");

                var listDatabaseResult = await testBase.ListDatabases(queryFile.FilePath);
                Assert.True(listDatabaseResult.DatabaseNames.Length > 0);

                await testBase.Disconnect(queryFile.FilePath);
            }
        }
    }
}
