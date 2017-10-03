//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;

namespace Microsoft.SqlTools.ResourceProvider.DefaultImpl
{
    /// <summary>
    /// Implementation for <see cref="IAzureSubscriptionIdentifier" />
    /// Contains information about an Azure subscription identifier
    /// </summary>
    internal class AzureSubscriptionIdentifier : IAzureSubscriptionIdentifier
    {
        /// <summary>
        /// Default constructor to initialize the subscription identifier
        /// </summary>
        public AzureSubscriptionIdentifier(IAzureUserAccount userAccount, string subscriptionId, Uri serviceManagementEndpoint)
        {
            UserAccount = userAccount;
            SubscriptionId = subscriptionId;
            ServiceManagementEndpoint = serviceManagementEndpoint;
        }

        /// <summary>
        /// Returns true if given subscription identifier equals this class
        /// </summary>
        public bool Equals(IAzureSubscriptionIdentifier other)
        {
            return other != null &&
                   CommonUtil.SameString(SubscriptionId, other.SubscriptionId) &&                   
                   CommonUtil.SameUri(ServiceManagementEndpoint, other.ServiceManagementEndpoint);
        }

        public IAzureUserAccount UserAccount
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns the endpoint url used by the identifier
        /// </summary>
        public Uri ServiceManagementEndpoint
        {
            get;
            private set;
        }

        /// <summary>
        /// Subscription id
        /// </summary>
        public string SubscriptionId
        {
            get;
            private set;
        }        
    }
}
