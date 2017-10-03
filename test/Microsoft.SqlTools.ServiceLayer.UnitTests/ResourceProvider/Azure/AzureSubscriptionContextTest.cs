//------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using Microsoft.SqlTools.ResourceProvider.DefaultImpl;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Azure
{
    /// <summary>
    /// Tests for AzureSubscriptionContextWrapper to verify the wrapper on azure subscription class
    /// </summary>
    public class AzureSubscriptionContextTest
    {
        [Fact]
        public void SubscriptionNameShouldReturnNullGivenNullSubscription()
        {
            AzureSubscriptionContext subscriptionContext = new AzureSubscriptionContext(null);
            Assert.True(subscriptionContext.SubscriptionName == String.Empty);
            Assert.True(subscriptionContext.Subscription != null);
        }

        [Fact]
        public void SubscriptionNameShouldReturnCorrectValueGivenValidSubscription()
        {
            string name = Guid.NewGuid().ToString();

            AzureSubscriptionContext subscriptionContext = new AzureSubscriptionContext(new AzureSubscriptionIdentifier(null, name, null));
            Assert.True(subscriptionContext.SubscriptionName == name);
            Assert.True(subscriptionContext.Subscription != null);
        }
    }
}
