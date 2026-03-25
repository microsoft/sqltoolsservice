//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.ResourceProvider.DefaultImpl;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Azure
{
    public class AzureResourceWrapperTest
    {
        [Test]
        public void ShouldParseResourceGroupFromId()
        {
            // Given a resource with a known resource group
            AzureResourceInfo resourceInfo = CreateMockResource(
                "/subscriptions/aaaaaaaa-1234-cccc-dddd-a1234v12c23/resourceGroups/myresourcegroup/providers/Microsoft.Sql/servers/my-server",
                "my-server",
                "Microsoft.Sql");

            // When I get the resource group name
            AzureResourceWrapper resource = new AzureResourceWrapper(resourceInfo);
            string rgName = resource.ResourceGroupName;

            // then I get it as expected
            Assert.AreEqual("myresourcegroup", rgName);
        }

        [Test]
        public void ShouldHandleMissingResourceGroup()
        {
            // Given a resource without resource group in the ID
            AzureResourceInfo resourceInfo = CreateMockResource(
                "/subscriptions/aaaaaaaa-1234-cccc-dddd-a1234v12c23",
                "my-server",
                "Microsoft.Sql");

            // When I get the resource group name
            AzureResourceWrapper resource = new AzureResourceWrapper(resourceInfo);
            string rgName = resource.ResourceGroupName;

            // then I get string.Empty
            Assert.AreEqual(string.Empty, rgName);
        }

        private AzureResourceInfo CreateMockResource(string id = null, string name = null, string type = null)
        {
            return new AzureResourceInfo(name, id, type, "Somewhere");
        }
    }
}
