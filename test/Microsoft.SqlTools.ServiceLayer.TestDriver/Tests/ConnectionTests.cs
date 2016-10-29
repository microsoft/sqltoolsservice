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
    public class ConnectionTests : TestBase
    {
        /// <summary>
        /// Try to connect with invalid credentials
        /// </summary>
        [Fact]
        public async Task InvalidConnection()
        {
            try
            {            
                string ownerUri = System.IO.Path.GetTempFileName();
                bool connected = await Connect(ownerUri, ConnectionTestUtils.InvalidConnection, 300000);
                Assert.False(connected, "Invalid connection is failed to connect");

                await Connect(ownerUri, ConnectionTestUtils.InvalidConnection, 300000);

                Thread.Sleep(1000);

                await CancelConnect(ownerUri);

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

        /// <summary>
        /// Validate list databases request
        /// </summary>
        [Fact]
        public async Task ListDatabasesTest()
        {
            try
            {            
                string ownerUri = System.IO.Path.GetTempFileName();
                bool connected = await Connect(ownerUri, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection successful");

                var listDatabaseResult = await ListDatabases(ownerUri);
                Assert.True(listDatabaseResult.DatabaseNames.Length > 0);

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

    }
}
