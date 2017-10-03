//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Rest;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;

namespace Microsoft.SqlTools.ResourceProvider.DefaultImpl
{
    /// <summary>
    /// Implementation for <see cref="IAzureUserAccountSubscriptionContext" /> using built-in services
    /// Contains information about an Azure account subscription
    /// </summary>
    internal class AzureUserAccountSubscriptionContext : IAzureUserAccountSubscriptionContext
    {

        /// <summary>
        /// Default constructor to initializes user account and subscription
        /// </summary>
        public AzureUserAccountSubscriptionContext(AzureSubscriptionIdentifier subscription, ServiceClientCredentials credentials)
        {
            CommonUtil.CheckForNull(subscription, nameof(subscription));
            CommonUtil.CheckForNull(credentials, nameof(credentials));
            Subscription = subscription;
            Credentials = credentials;
        }

        /// <summary>
        /// Creates a subscription context for connecting with a known access token. This creates a <see cref="TokenCredentials"/> object for use 
        /// in a request
        /// </summary>
        public static AzureUserAccountSubscriptionContext CreateStringTokenContext(AzureSubscriptionIdentifier subscription, string accessToken)
        {
            CommonUtil.CheckForNull(subscription, nameof(subscription));
            CommonUtil.CheckStringForNullOrEmpty(accessToken, nameof(accessToken));
            TokenCredentials credentials = new TokenCredentials(accessToken);
            return new AzureUserAccountSubscriptionContext(subscription, credentials);
        }

        public bool Equals(IAzureSubscriptionContext other)
        {
            return other != null && other.Equals(this);
        }

        /// <summary>
        /// Returns the wraper for the subscription identifier
        /// </summary>
        public IAzureSubscriptionIdentifier Subscription
        {
            get;
            private set;
        }

        /// <summary>
        /// Subscription name
        /// </summary>
        public string SubscriptionName
        {
            get { return Subscription != null ? Subscription.SubscriptionId : string.Empty; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Equals(IAzureUserAccountSubscriptionContext other)
        {
            return other != null &&
                CommonUtil.SameSubscriptionIdentifier(Subscription, other.Subscription) &&
                CommonUtil.SameUserAccount(UserAccount, other.UserAccount);
        }

        /// <summary>
        /// User Account
        /// </summary>
        public IAzureUserAccount UserAccount
        {
            get
            {
                return Subscription != null ?
                    new AzureUserAccount(Subscription.UserAccount) : null;
            }
        }

        public ServiceClientCredentials Credentials
        {
            get;
            private set;
        }
    }
}
