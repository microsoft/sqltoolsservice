//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using System.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.Test.Connection
{
    /// <summary>
    /// Tests for Sever Information Caching Class
    /// </summary>
    public class CachedServerInfoTests
    {
        private CachedServerInfo cache;

        public CachedServerInfoTests()
        {
            cache = new CachedServerInfo();
        }

        [Fact]
        public void CacheMatchesNullDbNameToEmptyString()
        {
            // Set sqlDw result into cache
            string dataSource = "testDataSource";
            bool isSqlDwResult;
            SqlConnectionStringBuilder testSource = new SqlConnectionStringBuilder
            {
                DataSource = dataSource,
                InitialCatalog = string.Empty
            };
            cache.AddOrUpdateCache(testSource, true, CachedServerInfo.CacheVariable.IsSqlDw);

            // Expect the same returned result
            Assert.True(cache.TryGetIsSqlDw(testSource, out isSqlDwResult));
            Assert.True(isSqlDwResult);

            // And expect the same for the null string
            Assert.True(cache.TryGetIsSqlDw(new SqlConnectionStringBuilder
            {
                DataSource = dataSource
                // Initial Catalog is null. Can't set explicitly as this throws
            }, out isSqlDwResult));
            Assert.True(isSqlDwResult);

            // But expect false for a different DB
            Assert.False(cache.TryGetIsSqlDw(new SqlConnectionStringBuilder
            {
                DataSource = dataSource,
                InitialCatalog = "OtherDb"
            }, out isSqlDwResult));
        }

        [Theory]
        [InlineData(null, true)]  // is SqlDW instance
        [InlineData("", true)]  // is SqlDW instance
        [InlineData("myDb", true)]  // is SqlDW instance
        [InlineData(null, false)]   // is not a SqlDw Instance
        [InlineData("", false)]   // is not a SqlDw Instance
        [InlineData("myDb", false)]  // is not SqlDW instance
        public void AddOrUpdateIsSqlDw(string dbName, bool state)
        {
            // Set sqlDw result into cache
            bool isSqlDwResult;
            SqlConnectionStringBuilder testSource = new SqlConnectionStringBuilder
            {
                DataSource = "testDataSource"
            };
            if (dbName != null)
            {
                testSource.InitialCatalog = dbName;
            }

            cache.AddOrUpdateCache(testSource, state, CachedServerInfo.CacheVariable.IsSqlDw);

            // Expect the same returned result
            Assert.True(cache.TryGetIsSqlDw(testSource, out isSqlDwResult));
            Assert.Equal(isSqlDwResult, state);
        }

        [Theory]
        [InlineData(true)]  // is SqlDW instance
        [InlineData(false)]   // is not a SqlDw Instance
        public void AddOrUpdateIsSqlDwFalseToggle(bool state)
        {
            // Set sqlDw result into cache
            bool isSqlDwResult;
            SqlConnectionStringBuilder testSource = new SqlConnectionStringBuilder
            {
                DataSource = "testDataSource"
            };
            cache.AddOrUpdateCache(testSource, state, CachedServerInfo.CacheVariable.IsSqlDw);

            // Expect the same returned result
            Assert.True(cache.TryGetIsSqlDw(testSource, out isSqlDwResult));
            Assert.Equal(isSqlDwResult, state);

            // Toggle isSqlDw cache state
            bool isSqlDwResultToggle;
            cache.AddOrUpdateCache(testSource, !state, CachedServerInfo.CacheVariable.IsSqlDw);

            // Expect the oppisite returned result
            Assert.True(cache.TryGetIsSqlDw(testSource, out isSqlDwResultToggle));
            Assert.Equal(isSqlDwResultToggle, !state);

        }

        [Fact]
        public void AddOrUpdateIsSqlDwFalseToggle()
        {
            bool state = true;

            SqlConnectionStringBuilder testSource = new SqlConnectionStringBuilder
            {
                DataSource = "testDataSource"
            };

            SqlConnectionStringBuilder sameServerDifferentDb = new SqlConnectionStringBuilder
            {
                DataSource = "testDataSource",
                InitialCatalog = "myDb"          
            };
            SqlConnectionStringBuilder differentServerSameDb = new SqlConnectionStringBuilder
            {
                DataSource = "testDataSource2",
                InitialCatalog = ""
            };

            cache.AddOrUpdateCache(testSource, state, CachedServerInfo.CacheVariable.IsSqlDw);
            cache.AddOrUpdateCache(sameServerDifferentDb, !state, CachedServerInfo.CacheVariable.IsSqlDw);
            cache.AddOrUpdateCache(differentServerSameDb, !state, CachedServerInfo.CacheVariable.IsSqlDw);

            // Expect the same returned result
            // Set sqlDw result into cache
            bool isSqlDwResult;
            bool isSqlDwResult2;
            bool isSqlDwResult3;
            Assert.True(cache.TryGetIsSqlDw(testSource, out isSqlDwResult));
            Assert.True(cache.TryGetIsSqlDw(sameServerDifferentDb, out isSqlDwResult2));
            Assert.True(cache.TryGetIsSqlDw(differentServerSameDb, out isSqlDwResult3));

            // Assert cache is set on a per connection basis
            Assert.Equal(isSqlDwResult, state);
            Assert.Equal(isSqlDwResult2, !state);
            Assert.Equal(isSqlDwResult3, !state);

        }

        [Fact]
        public void AskforSqlDwBeforeCached()
        {
            bool isSqlDwResult;
            Assert.False(cache.TryGetIsSqlDw(new SqlConnectionStringBuilder
            {
                DataSource = "testDataSourceUnCached"
            }, 
            out isSqlDwResult));
        }
    }
}