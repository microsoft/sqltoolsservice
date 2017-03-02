//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
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
            CachedServerInfo.AddOrUpdateCache("testDataSource", state, CachedServerInfo.CacheVariable.IsSqlDw);

            // Expect the same returned result
            Assert.True(CachedServerInfo.TryGetIsSqlDw("testDataSource", out isSqlDwResult));
            Assert.Equal(isSqlDwResult, state);
        }

        [Theory]
        [InlineData(true)]  // is SqlDW instance
        [InlineData(false)]   // is not a SqlDw Instance
        public void AddOrUpdateIsSqlDwFalseToggle(bool state)
        {
            // Set sqlDw result into cache
            bool isSqlDwResult;
            CachedServerInfo.AddOrUpdateCache("testDataSource", state, CachedServerInfo.CacheVariable.IsSqlDw);

            // Expect the same returned result
            Assert.True(CachedServerInfo.TryGetIsSqlDw("testDataSource", out isSqlDwResult));
            Assert.Equal(isSqlDwResult, state);

            // Toggle isSqlDw cache state
            bool isSqlDwResultToggle;
            CachedServerInfo.AddOrUpdateCache("testDataSource", !state, CachedServerInfo.CacheVariable.IsSqlDw);

            // Expect the oppisite returned result
            Assert.True(CachedServerInfo.TryGetIsSqlDw("testDataSource", out isSqlDwResultToggle));
            Assert.Equal(isSqlDwResultToggle, !state);

        }

        [Fact]
        public void AddOrUpdateIsSqlDwFalseToggle()
        {
            bool state = true;
            // Set sqlDw result into cache
            bool isSqlDwResult;
            bool isSqlDwResult2;
            CachedServerInfo.AddOrUpdateCache("testDataSource", state, CachedServerInfo.CacheVariable.IsSqlDw);
            CachedServerInfo.AddOrUpdateCache("testDataSource2", !state, CachedServerInfo.CacheVariable.IsSqlDw);

            // Expect the same returned result
            Assert.True(CachedServerInfo.TryGetIsSqlDw("testDataSource", out isSqlDwResult));
            Assert.True(CachedServerInfo.TryGetIsSqlDw("testDataSource2", out isSqlDwResult2));

            // Assert cache is set on a per connection basis
            Assert.Equal(isSqlDwResult, state);
            Assert.Equal(isSqlDwResult2, !state);

        }

        [Fact]
        public void AskforSqlDwBeforeCached()
        {
            bool isSqlDwResult;
            Assert.False(CachedServerInfo.TryGetIsSqlDw("testDataSourceWithNoCache", out isSqlDwResult));
        }
    }
}