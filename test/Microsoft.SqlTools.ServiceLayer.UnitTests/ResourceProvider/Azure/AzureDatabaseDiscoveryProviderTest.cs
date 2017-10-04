//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Azure
{
    /// <summary>
    /// Tests for AzureDatabaseDiscoveryProvider to verify getting azure databases using azure resource manager
    /// </summary>
    public class AzureDatabaseDiscoveryProviderTest
    {
        [Fact]
        public async Task GetShouldReturnDatabasesSuccessfully()
        {
            string databaseName1 = "server/db1";
            string databaseName2 = "db2";
            string databaseName3 = "server/";
            string databaseName4 = "/db4";
            List<string> databasesForSubscription = new List<string>()
            {
                databaseName1,
                databaseName2
            };

            Dictionary<string, List<string>> subscriptionToDatabaseMap = new Dictionary<string, List<string>>();
            subscriptionToDatabaseMap.Add(Guid.NewGuid().ToString(), databasesForSubscription);
            subscriptionToDatabaseMap.Add(Guid.NewGuid().ToString(), new List<string>()
            {
                databaseName3,
                databaseName4,
            });

            AzureDatabaseDiscoveryProvider databaseDiscoveryProvider = FakeDataFactory.CreateAzureDatabaseDiscoveryProvider(subscriptionToDatabaseMap);
            ServiceResponse<DatabaseInstanceInfo> response = await databaseDiscoveryProvider.GetDatabaseInstancesAsync(serverName: null, cancellationToken: new CancellationToken());
            List<DatabaseInstanceInfo> list = response.Data.ToList();
            Assert.NotNull(list);
            Assert.True(list.Any(x => x.Name == "db1" && x.ServerInstanceInfo.Name == "server"));
            Assert.False(list.Any(x => x.Name == "db2" && x.ServerInstanceInfo.Name == ""));
            Assert.True(list.Any(x => x.Name == "" && x.ServerInstanceInfo.Name == "server"));
            Assert.False(list.Any(x => x.Name == "db4" && x.ServerInstanceInfo.Name == ""));
            Assert.True(list.Count() == 2);
        }

        [Fact]
        public async Task GetShouldReturnDatabasesEvenIfFailsForOneServer()
        {
            string databaseName1 = "server1/db1";
            string databaseName2 = "server1/db2";
            string databaseName3 = "error/db3";
            string databaseName4 = "server2/db4";
            List<string> databasesForSubscription = new List<string>()
            {
                databaseName1,
                databaseName2
            };

            Dictionary<string, List<string>> subscriptionToDatabaseMap = new Dictionary<string, List<string>>();
            subscriptionToDatabaseMap.Add(Guid.NewGuid().ToString(), databasesForSubscription);
            subscriptionToDatabaseMap.Add(Guid.NewGuid().ToString(), new List<string>()
            {
                databaseName3,
                databaseName4,
            });

            AzureDatabaseDiscoveryProvider databaseDiscoveryProvider = FakeDataFactory.CreateAzureDatabaseDiscoveryProvider(subscriptionToDatabaseMap);
            ServiceResponse<DatabaseInstanceInfo> response = await databaseDiscoveryProvider.GetDatabaseInstancesAsync(serverName: null, cancellationToken: new CancellationToken());
            List<DatabaseInstanceInfo> list = response.Data.ToList();
            Assert.NotNull(list);
            Assert.True(list.Any(x => x.Name == "db1" && x.ServerInstanceInfo.Name == "server1"));
            Assert.True(list.Any(x => x.Name == "db2" && x.ServerInstanceInfo.Name == "server1"));
            Assert.True(list.Any(x => x.Name == "db4" && x.ServerInstanceInfo.Name == "server2"));
            Assert.False(list.Any(x => x.Name == "db3" && x.ServerInstanceInfo.Name == "error"));
            Assert.True(list.Count() == 3);
            Assert.NotNull(response.Errors);
            Assert.True(response.Errors.Count() == 1);
        }

        [Fact]
        public async Task GetShouldReturnDatabasesFromCacheIfGetCalledTwice()
        {
            Dictionary<string, List<string>> subscriptionToDatabaseMap = CreateSubscriptonMap(2);
            AddDatabases(subscriptionToDatabaseMap, 3);

            AzureDatabaseDiscoveryProvider databaseDiscoveryProvider = FakeDataFactory.CreateAzureDatabaseDiscoveryProvider(subscriptionToDatabaseMap);
            ServiceResponse<DatabaseInstanceInfo> response = await databaseDiscoveryProvider.GetDatabaseInstancesAsync(serverName: null, cancellationToken: new CancellationToken());
            List<DatabaseInstanceInfo> list = response.Data.ToList();
            ValidateResult(subscriptionToDatabaseMap, list);

            Dictionary<string, List<string>> subscriptionToDatabaseMap2 = CopySubscriptonMap(subscriptionToDatabaseMap);
            AddDatabases(subscriptionToDatabaseMap2, 5);
            AzureTestContext testContext = new AzureTestContext(subscriptionToDatabaseMap2);
            databaseDiscoveryProvider.AccountManager = testContext.AzureAccountManager;
            databaseDiscoveryProvider.AzureResourceManager = testContext.AzureResourceManager;
            response = await databaseDiscoveryProvider.GetDatabaseInstancesAsync(serverName: null, cancellationToken: new CancellationToken());
            list = response.Data.ToList();
            //the provider should get the databases from cache for the list should match the first created data
            ValidateResult(subscriptionToDatabaseMap, list);
        }

        [Fact]
        public async Task GetShouldReturnDatabasesFromServiceIfGetCalledTwiceButRefreshed()
        {
            Dictionary<string, List<string>> subscriptionToDatabaseMap = CreateSubscriptonMap(2);
            AddDatabases(subscriptionToDatabaseMap, 3);

            AzureDatabaseDiscoveryProvider databaseDiscoveryProvider = FakeDataFactory.CreateAzureDatabaseDiscoveryProvider(subscriptionToDatabaseMap);
            ServiceResponse<DatabaseInstanceInfo> response = await databaseDiscoveryProvider.GetDatabaseInstancesAsync(serverName: null, cancellationToken: new CancellationToken());
            List<DatabaseInstanceInfo> list = response.Data.ToList();
            ValidateResult(subscriptionToDatabaseMap, list);

            Dictionary<string, List<string>> subscriptionToDatabaseMap2 = CopySubscriptonMap(subscriptionToDatabaseMap);
            AddDatabases(subscriptionToDatabaseMap2, 5);
            AzureTestContext testContext = new AzureTestContext(subscriptionToDatabaseMap2);
            databaseDiscoveryProvider.AccountManager = testContext.AzureAccountManager;
            databaseDiscoveryProvider.AzureResourceManager = testContext.AzureResourceManager;
            await databaseDiscoveryProvider.ClearCacheAsync();
            response = await databaseDiscoveryProvider.GetDatabaseInstancesAsync(serverName: null, cancellationToken: new CancellationToken());
            list = response.Data.ToList();
            //the provider should get the databases from cache for the list should match the first created data
            ValidateResult(subscriptionToDatabaseMap2, list);
        }

        private void ValidateResult(Dictionary<string, List<string>> subscriptionToDatabaseMap, List<DatabaseInstanceInfo> result)
        {
            Assert.NotNull(result);
            int numberOfDatabases = 0;
            foreach (KeyValuePair<string, List<string>> item in subscriptionToDatabaseMap)
            {
                numberOfDatabases += item.Value.Count;
                foreach (string databaseFullName in item.Value)
                {
                    string serverName = AzureTestContext.GetServerName(databaseFullName);
                    string databaseName = databaseFullName.Replace(serverName + @"/", "");
                    Assert.True(result.Any(x => x.Name == databaseName && x.ServerInstanceInfo.Name == serverName));
                }
            }
            Assert.True(result.Count() == numberOfDatabases);
        }

        private void AddDatabases(Dictionary<string, List<string>> subscriptionToDatabaseMap, int numberOfDatabases)
        {
            foreach (string key in subscriptionToDatabaseMap.Keys.ToList())
            {
                List<string> databases = CreateDatabases(numberOfDatabases);
                subscriptionToDatabaseMap[key] = databases;
            }
        }

        private List<string> CreateDatabases(int numberOfDatabases)
        {
            List<string> databases = new List<string>();
            for (int j = 0; j < numberOfDatabases; j++)
            {
                databases.Add(string.Format(CultureInfo.InvariantCulture, @"{0}/{1}", Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            }
            return databases;
        }

        private Dictionary<string, List<string>> CreateSubscriptonMap(int numberOfSubscriptions)
        {
            Dictionary<string, List<string>> subscriptionToDatabaseMap = new Dictionary<string, List<string>>();
            for (int i = 0; i < numberOfSubscriptions; i++)
            {
                string id = Guid.NewGuid().ToString();                
                subscriptionToDatabaseMap.Add(id, null);
            }
            return subscriptionToDatabaseMap;
        }

        private Dictionary<string, List<string>> CopySubscriptonMap(Dictionary<string, List<string>> subscriptionToDatabaseMap)
        {
            Dictionary<string, List<string>> subscriptionToDatabaseMapCopy = new Dictionary<string, List<string>>();
            foreach (KeyValuePair<string, List<string>> pair in subscriptionToDatabaseMap)
            {
                subscriptionToDatabaseMapCopy.Add(pair.Key, null);
            }
            return subscriptionToDatabaseMapCopy;
        }

        [Fact]
        public async Task GetShouldReturnEmptyGivenNotSubscriptionFound()
        {
            Dictionary<string, List<string>> subscriptionToDatabaseMap = new Dictionary<string, List<string>>();

            AzureDatabaseDiscoveryProvider databaseDiscoveryProvider =
                FakeDataFactory.CreateAzureDatabaseDiscoveryProvider(subscriptionToDatabaseMap);
            ServiceResponse<DatabaseInstanceInfo> response =
                await databaseDiscoveryProvider.GetDatabaseInstancesAsync(serverName: null, cancellationToken: new CancellationToken());
            Assert.NotNull(response);
            Assert.NotNull(response.Data);
            Assert.False(response.Data.Any());
        }
    }
}
