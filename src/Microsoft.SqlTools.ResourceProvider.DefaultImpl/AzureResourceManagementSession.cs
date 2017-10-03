//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.Sql;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;

namespace Microsoft.SqlTools.ResourceProvider.DefaultImpl
{
    /// <summary>
    /// VS session used by <see cref="AzureResourceManager" />. 
    /// Includes all the clients that the resource management needs to get ther resources
    /// </summary>
    internal class AzureResourceManagementSession : IAzureResourceManagementSession
    {
        /// <summary>
        /// Creates the new session for given clients
        /// </summary>
        /// <param name="sqlManagementClient">Sql Management Client</param>
        /// <param name="resourceManagementClient">Resource Management Client</param>
        /// <param name="subscriptionContext">Subscription Context</param>
        public AzureResourceManagementSession(SqlManagementClient sqlManagementClient, 
            ResourceManagementClient resourceManagementClient,
            IAzureUserAccountSubscriptionContext subscriptionContext)
        {
            SqlManagementClient = sqlManagementClient;
            ResourceManagementClient = resourceManagementClient;
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
        /// Closes the session by disposing the clients
        /// </summary>
        /// <returns></returns>
        public bool CloseSession()
        {
            try
            {            
                if (ResourceManagementClient != null)
                {
                    ResourceManagementClient.Dispose();
                }

                if (SqlManagementClient != null)
                {
                    SqlManagementClient.Dispose();
                }
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
        /// Resource Management Client
        /// </summary>
        public ResourceManagementClient ResourceManagementClient
        {
            get; set;
        }

        /// <summary>
        /// Sql Management Client
        /// </summary>
        public SqlManagementClient SqlManagementClient
        {
            get; set;
        }
    }
}
