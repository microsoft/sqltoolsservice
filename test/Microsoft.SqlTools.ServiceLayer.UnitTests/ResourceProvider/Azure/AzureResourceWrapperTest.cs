//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Azure.Management.Sql.Models;
using Microsoft.SqlTools.ResourceProvider.DefaultImpl;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Azure
{
    public class AzureResourceWrapperTest
    {
        [Fact]
        public void ShouldParseResourceGroupFromId()
        {
            // Given a resource with a known resource group
            TrackedResource trackedResource = CreateMockResource(
                "/subscriptions/aaaaaaaa-1234-cccc-dddd-a1234v12c23/resourceGroups/myresourcegroup/providers/Microsoft.Sql/servers/my-server",
                "my-server",
                "Microsoft.Sql");

            // When I get the resource group name
            AzureResourceWrapper resource = new AzureResourceWrapper(trackedResource);
            string rgName = resource.ResourceGroupName;

            // then I get it as expected
            Assert.Equal("myresourcegroup", rgName);
        }

        [Fact]
        public void ShouldHandleMissingResourceGroup()
        {
            // Given a resource without resource group in the ID
            TrackedResource trackedResource = CreateMockResource(
                "/subscriptions/aaaaaaaa-1234-cccc-dddd-a1234v12c23",
                "my-server",
                "Microsoft.Sql");

            // When I get the resource group name
            AzureResourceWrapper resource = new AzureResourceWrapper(trackedResource);
            string rgName = resource.ResourceGroupName;

            // then I get string.Empty
            Assert.Equal(string.Empty, rgName);
        }

        private TrackedResource CreateMockResource(string id = null, string name = null, string type = null)
        {
            return new TrackedResource("Somewhere", id, name, type);
        }
    }
}
