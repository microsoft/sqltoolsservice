//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Azure.ResourceManager;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;

namespace Microsoft.SqlTools.ResourceProvider.DefaultImpl
{
    /// <summary>
    /// VS session used by <see cref="AzureResourceManager" />.
    /// Includes all the clients that the resource management needs to get their resources
    /// </summary>
    public class AzureResourceManagementSession : IAzureResourceManagementSession
    {
        /// <summary>
        /// Creates the new session for given clients
        /// </summary>
        /// <param name="armClient">ARM client</param>
        /// <param name="subscriptionContext">Subscription Context</param>
        public AzureResourceManagementSession(ArmClient armClient,
            IAzureUserAccountSubscriptionContext subscriptionContext)
        {
            ArmClient = armClient;
            SubscriptionContext = subscriptionContext;
        }

        /// <summary>
        /// Disposes the session
        /// </summary>
        public void Dispose()
        {
            CloseSession();
        }

        /// <summary>
        /// Closes the session
        /// </summary>
        public bool CloseSession()
        {
            try
            {
                ArmClient = null;
                return true;
            }
            catch (Exception)
            {
                //TODO: trace
                return false;
            }
        }

        /// <summary>
        /// Subscription Context
        /// </summary>
        public IAzureUserAccountSubscriptionContext SubscriptionContext
        {
            get;
            set;
        }

        /// <summary>
        /// ARM client for interacting with Azure Resource Manager
        /// </summary>
        public ArmClient ArmClient
        {
            get; set;
        }
    }
}
