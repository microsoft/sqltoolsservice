//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.Azure.Core.Authentication
{
    /// <summary>
    /// Provides functionality to authenticate to Azure and discover associated accounts and subscriptions 
    /// </summary>
    public interface IAzureAuthenticationManager : IAccountManager
    {
        /// <summary>
        /// User accounts associated to the logged in user
        /// </summary>
        IEnumerable<IAzureUserAccount> UserAccounts
        {
            get;
        }

        /// <summary>
        /// Azure subscriptions associated to the logged in user
        /// </summary>
        Task<IEnumerable<IAzureUserAccountSubscriptionContext>> GetSubscriptionsAsync();

        /// <summary>
        /// Returns user's azure subscriptions
        /// </summary>
        Task<IEnumerable<IAzureUserAccountSubscriptionContext>> GetSelectedSubscriptionsAsync();

        /// <summary>
        /// Finds a subscription given subscription id
        /// </summary>
        bool TryParseSubscriptionIdentifier(string value, out IAzureSubscriptionIdentifier subscription);

        /// <summary>
        /// Stores the selected subscriptions given the ids
        /// </summary>
        Task<bool> SetSelectedSubscriptionsAsync(IEnumerable<string> subscriptionIds);        
    }
}
