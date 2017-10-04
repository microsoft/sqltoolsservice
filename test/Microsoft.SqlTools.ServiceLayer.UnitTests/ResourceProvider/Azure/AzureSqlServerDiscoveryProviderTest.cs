//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Azure
{
    /// <summary>
    /// Tests for AzureServerDiscoveryProvider to verify getting azure servers using azure resource manager
    /// </summary>
    public class AzureSqlServerDiscoveryProviderTest
    {
        [Fact]
        public async Task GetShouldReturnServersSuccessfully()
        {
            string serverName = "server";
            List<string> serversForSubscription = new List<string>()
            {
                Guid.NewGuid().ToString(),
                serverName
            };

            Dictionary<string, List<string>> subscriptionToDatabaseMap = new Dictionary<string, List<string>>();
            subscriptionToDatabaseMap.Add(Guid.NewGuid().ToString(), serversForSubscription);
            subscriptionToDatabaseMap.Add(Guid.NewGuid().ToString(), new List<string>()
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
            });

            AzureSqlServerDiscoveryProvider discoveryProvider =
                FakeDataFactory.CreateAzureServerDiscoveryProvider(subscriptionToDatabaseMap);
            ServiceResponse<ServerInstanceInfo> response = await discoveryProvider.GetServerInstancesAsync();
            IEnumerable<ServerInstanceInfo> servers = response.Data;
            Assert.NotNull(servers);
            Assert.True(servers.Any(x => x.Name == serverName));
            Assert.True(servers.Count() == 4);
        }

        [Fact]
        public async Task GetShouldReturnEmptyGivenNotSubscriptionFound()
        {
            Dictionary<string, List<string>> subscriptionToDatabaseMap = new Dictionary<string, List<string>>();

            AzureSqlServerDiscoveryProvider discoveryProvider =
                FakeDataFactory.CreateAzureServerDiscoveryProvider(subscriptionToDatabaseMap);
            ServiceResponse<ServerInstanceInfo> response = await discoveryProvider.GetServerInstancesAsync();
            IEnumerable<ServerInstanceInfo> servers = response.Data;
            Assert.NotNull(servers);
            Assert.False(servers.Any());
        }
    }
}
