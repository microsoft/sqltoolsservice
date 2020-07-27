//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using NUnit.Framework;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    [TestFixture]
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

        [Test]
        public void CacheMatchesNullDbNameToEmptyString()
        {
            // Set sqlDw result into cache
            string dataSource = "testDataSource";
            SqlConnectionStringBuilder testSource = new SqlConnectionStringBuilder
            {
                DataSource = dataSource,
                InitialCatalog = string.Empty
            };
            cache.AddOrUpdateCache(testSource, DatabaseEngineEdition.SqlDataWarehouse, CachedServerInfo.CacheVariable.EngineEdition);

            // Expect the same returned result
            Assert.AreEqual(DatabaseEngineEdition.SqlDataWarehouse, cache.TryGetEngineEdition(testSource, out _));

            // And expect the same for the null string
            Assert.AreEqual(DatabaseEngineEdition.SqlDataWarehouse, cache.TryGetEngineEdition(new SqlConnectionStringBuilder
            {
                DataSource = dataSource
                // Initial Catalog is null. Can't set explicitly as this throws
            }, out _));

            Assert.That(cache.TryGetEngineEdition(new SqlConnectionStringBuilder
            {
                DataSource = dataSource,
                InitialCatalog = "OtherDb"
            }, out _), Is.Not.EqualTo(DatabaseEngineEdition.SqlDataWarehouse), "expect NotEqual for a different DB");
        }

        
        [Test]
        public void AddOrUpdateEngineEdition([Values(null, "", "myDb")] string dbName, 
                                              [Values(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)] DatabaseEngineEdition state)
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

            Assert.Multiple(() =>
            {
                Assert.That(cache.TryGetEngineEdition(testSource, out engineEdition), Is.Not.EqualTo(DatabaseEngineEdition.Unknown) );
                Assert.That(engineEdition, Is.EqualTo(state), "Expect the same returned result");
            });
        }

        [Test]
        public void AddOrUpdateEngineEditionToggle([Values(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)] DatabaseEngineEdition state)
        {
            // Set result into cache
            DatabaseEngineEdition engineEdition;
            SqlConnectionStringBuilder testSource = new SqlConnectionStringBuilder
            {
                DataSource = "testDataSource"
            };
            cache.AddOrUpdateCache(testSource, state, CachedServerInfo.CacheVariable.EngineEdition);

            Assert.Multiple(() =>
            {                 
                Assert.That(cache.TryGetEngineEdition(testSource, out engineEdition), Is.Not.EqualTo(DatabaseEngineEdition.Unknown));
                Assert.That(engineEdition, Is.EqualTo(state), "Expect the same returned result");
            });

            DatabaseEngineEdition newState = state == DatabaseEngineEdition.SqlDataWarehouse ?
                DatabaseEngineEdition.SqlOnDemand : DatabaseEngineEdition.SqlDataWarehouse;

            cache.AddOrUpdateCache(testSource, newState, CachedServerInfo.CacheVariable.EngineEdition);

            Assert.Multiple(() =>
            {
                Assert.That(cache.TryGetEngineEdition(testSource, out engineEdition), Is.Not.EqualTo(DatabaseEngineEdition.Unknown));
                Assert.That(engineEdition, Is.EqualTo(newState), "Expect the opposite returned result");
            });
        }

       /* [Test]
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
            Assert.AreEqual(isSqlDwResult, state);
            Assert.AreEqual(isSqlDwResult2, !state);
            Assert.AreEqual(isSqlDwResult3, !state);

        }
        */

        [Test]
        public void AskforEngineEditionBeforeCached()
        {
            Assert.AreEqual(DatabaseEngineEdition.Unknown, cache.TryGetEngineEdition(new SqlConnectionStringBuilder
            {
                DataSource = "testDataSourceUnCached"
            }, 
            out _));
        }
    }
}