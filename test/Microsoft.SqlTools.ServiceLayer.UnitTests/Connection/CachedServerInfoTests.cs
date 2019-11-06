//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Xunit;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
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
            DatabaseEngineEdition engineEdition;
            SqlConnectionStringBuilder testSource = new SqlConnectionStringBuilder
            {
                DataSource = dataSource,
                InitialCatalog = string.Empty
            };
            cache.AddOrUpdateCache(testSource, DatabaseEngineEdition.SqlDataWarehouse, CachedServerInfo.CacheVariable.EngineEdition);

            // Expect the same returned result
            Assert.Equal(cache.TryGetEngineEdition(testSource, out engineEdition), DatabaseEngineEdition.SqlDataWarehouse);

            // And expect the same for the null string
            Assert.Equal(cache.TryGetEngineEdition(new SqlConnectionStringBuilder
            {
                DataSource = dataSource
                // Initial Catalog is null. Can't set explicitly as this throws
            }, out engineEdition), DatabaseEngineEdition.SqlDataWarehouse);

            // But expect NotEqual for a different DB
            Assert.NotEqual(cache.TryGetEngineEdition(new SqlConnectionStringBuilder
            {
                DataSource = dataSource,
                InitialCatalog = "OtherDb"
            }, out engineEdition), DatabaseEngineEdition.SqlDataWarehouse);
        }

        [Theory]
        [InlineData(null, DatabaseEngineEdition.SqlDataWarehouse)]  // is SqlDW instance
        [InlineData("", DatabaseEngineEdition.SqlDataWarehouse)]  // is SqlDW instance
        [InlineData("myDb", DatabaseEngineEdition.SqlDataWarehouse)]  // is SqlDW instance
        [InlineData(null, DatabaseEngineEdition.SqlOnDemand)]   // is SqlOnDemand Instance
        [InlineData("", DatabaseEngineEdition.SqlOnDemand)]   // is SqlOnDemand Instance
        [InlineData("myDb", DatabaseEngineEdition.SqlOnDemand)]  // is SqlOnDemand instance
        public void AddOrUpdateEngineEditiopn(string dbName, DatabaseEngineEdition state)
        {
            // Set result into cache
            DatabaseEngineEdition engineEdition;
            SqlConnectionStringBuilder testSource = new SqlConnectionStringBuilder
            {
                DataSource = "testDataSource"
            };
            if (dbName != null)
            {
                testSource.InitialCatalog = dbName;
            }

            cache.AddOrUpdateCache(testSource, state, CachedServerInfo.CacheVariable.EngineEdition);

            // Expect the same returned result
            Assert.NotEqual(cache.TryGetEngineEdition(testSource, out engineEdition), DatabaseEngineEdition.Unknown);
            Assert.Equal(engineEdition, state);
        }

        [Theory]
        [InlineData(DatabaseEngineEdition.SqlDataWarehouse)]  // is SqlDW instance
        [InlineData(DatabaseEngineEdition.SqlOnDemand)]   // is SqlOnDemand Instance
        public void AddOrUpdateEngineEditionToggle(DatabaseEngineEdition state)
        {
            // Set result into cache
            DatabaseEngineEdition engineEdition;
            SqlConnectionStringBuilder testSource = new SqlConnectionStringBuilder
            {
                DataSource = "testDataSource"
            };
            cache.AddOrUpdateCache(testSource, state, CachedServerInfo.CacheVariable.EngineEdition);

            // Expect the same returned result
            Assert.NotEqual(cache.TryGetEngineEdition(testSource, out engineEdition), DatabaseEngineEdition.Unknown);
            Assert.Equal(engineEdition, state);

            DatabaseEngineEdition newState = state == DatabaseEngineEdition.SqlDataWarehouse ?
                DatabaseEngineEdition.SqlOnDemand : DatabaseEngineEdition.SqlDataWarehouse;

            cache.AddOrUpdateCache(testSource, newState, CachedServerInfo.CacheVariable.EngineEdition);

            // Expect the opposite returned result
            Assert.NotEqual(cache.TryGetEngineEdition(testSource, out engineEdition), DatabaseEngineEdition.Unknown);
            Assert.Equal(engineEdition, newState);
        }

       /* [Fact]
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
        */

        [Fact]
        public void AskforEngineEditionBeforeCached()
        {
            DatabaseEngineEdition engineEdition;
            Assert.Equal(cache.TryGetEngineEdition(new SqlConnectionStringBuilder
            {
                DataSource = "testDataSourceUnCached"
            }, 
            out engineEdition), DatabaseEngineEdition.Unknown);
        }
    }
}