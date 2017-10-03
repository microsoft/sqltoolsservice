//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.ResourceProvider.Core.Authentication;

namespace Microsoft.SqlTools.ResourceProvider.DefaultImpl
{
    /// <summary>
    /// Implementation for <see cref="IAzureSubscriptionContext" /> using VS services
    /// Contains information about an Azure subscription
    /// </summary>
    internal class AzureSubscriptionContext : IAzureSubscriptionContext
    {
        private readonly IAzureSubscriptionIdentifier _azureSubscriptionIdentifier;

        /// <summary>
        /// Default constructor to initialize the subscription
        /// </summary>
        public AzureSubscriptionContext(IAzureSubscriptionIdentifier azureSubscriptionIdentifier)
        {
            _azureSubscriptionIdentifier = azureSubscriptionIdentifier;
        }

        /// <summary>
        /// Returns true if given subscription equals this class
        /// </summary>
        public bool Equals(IAzureSubscriptionContext other)
        {
            return (other == null && Subscription == null) || (other != null && other.Subscription.Equals(Subscription));
        }

        /// <summary>
        /// Returns the wraper for the subscription identifier
        /// </summary>
        public IAzureSubscriptionIdentifier Subscription
        {
            get
            {
                return _azureSubscriptionIdentifier;
            }
        }

        /// <summary>
        /// Returns subscription name
        /// </summary>
        public string SubscriptionName
        {
            get
            {
                return _azureSubscriptionIdentifier != null ? 
                    _azureSubscriptionIdentifier.SubscriptionId : null;
            }
        }
    }
}
