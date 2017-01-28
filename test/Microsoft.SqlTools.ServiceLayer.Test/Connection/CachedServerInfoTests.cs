//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;

namespace Microsoft.SqlTools.ServiceLayer.Test.Connection
{
    /// <summary>
    /// Tests for Sever Information Caching Class
    /// </summary>
    public class CachedServerInfoTests
    {

        [Theory]
        [InlineData(true)]  // is SqlDW instance
        [InlineData(false)]   // is not a SqlDw Instance
        public void AddOrUpdateIsSqlDw(bool state)
        {
            // Set sqlDw result into cache
            bool isSqlDwResult;
            CachedServerInfo.AddOrUpdateIsSqlDw("testDataSource", state);

            // Expect the same returned result
            CachedServerInfo.TryGetIsSqlDw("testDataSource", out isSqlDwResult);
            Assert.Equal(isSqlDwResult, state);
        }

        [Theory]
        [InlineData(true)]  // is SqlDW instance
        [InlineData(false)]   // is not a SqlDw Instance
        public void AddOrUpdateIsSqlDwFalseToggle(bool state)
        {
            // Set sqlDw result into cache
            bool isSqlDwResult;
            CachedServerInfo.AddOrUpdateIsSqlDw("testDataSource", state);

            // Expect the same returned result
            CachedServerInfo.TryGetIsSqlDw("testDataSource", out isSqlDwResult);
            Assert.Equal(isSqlDwResult, state);

            // Toggle isSqlDw cache state
            bool isSqlDwResultToggle;
            CachedServerInfo.AddOrUpdateIsSqlDw("testDataSource", !state);

            // Expect the oppisite returned result
            CachedServerInfo.TryGetIsSqlDw("testDataSource", out isSqlDwResultToggle);
            Assert.Equal(isSqlDwResultToggle, !state);

        }

        [Fact]
        public void AddOrUpdateIsSqlDwFalseToggle()
        {
            bool state = true;
            // Set sqlDw result into cache
            bool isSqlDwResult;
            bool isSqlDwResult2;
            CachedServerInfo.AddOrUpdateIsSqlDw("testDataSource", state);
            CachedServerInfo.AddOrUpdateIsSqlDw("testDataSource2", !state);

            // Expect the same returned result
            CachedServerInfo.TryGetIsSqlDw("testDataSource", out isSqlDwResult);
            CachedServerInfo.TryGetIsSqlDw("testDataSource2", out isSqlDwResult2);

            // Assert cache is set on a per connection basis
            Assert.Equal(isSqlDwResult, state);
            Assert.Equal(isSqlDwResult2, !state);

        }

        [Fact]
        public void AskforSqlDwBeforeCached()
        {
            bool exceptionThrown = false;
            try
            {
                bool isSqlDwResult;
                // ask for result that has NOT been cached
                CachedServerInfo.TryGetIsSqlDw("testDataSourceCacheMiss", out isSqlDwResult);
            }
            catch (Exception)
            {
                exceptionThrown = true;
            }

            // Assert that the exception has been thrown
            Assert.True(exceptionThrown);
        }
    }
}