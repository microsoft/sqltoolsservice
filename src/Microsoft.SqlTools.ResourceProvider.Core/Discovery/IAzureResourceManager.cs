//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Microsoft.SqlTools.ResourceProvider.Core.Firewall;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;

namespace Microsoft.SqlTools.ResourceProvider.Core
{
    /// <summary>
    /// Provides functionality to get azure resources by making Http request to the Azure REST API
    /// </summary>
    public interface IAzureResourceManager : IExportable
    {
        /// <summary>
        /// Returns a list of azure databases given subscription resource group name and server name
        /// </summary>
        /// <param name="azureResourceManagementSession">Subscription Context which includes credentials to use in the resource manager</param>
        /// <param name="resourceGroupName">Resource Group Name</param>
        /// <param name="serverName">Server name</param>
        /// <returns>The list of databases</returns>
        Task<IEnumerable<IAzureResource>> GetAzureDatabasesAsync(
            IAzureResourceManagementSession azureResourceManagementSession,
            string resourceGroupName, 
            string serverName);

        /// <summary>
        /// Returns a list of azure servers given subscription
        /// </summary>
        /// <param name="azureResourceManagementSession">Subscription Context which includes credentials to use in the resource manager</param>
        /// <returns>The list of Sql server resources</returns>
        Task<IEnumerable<IAzureSqlServerResource>> GetSqlServerAzureResourcesAsync(
            IAzureResourceManagementSession azureResourceManagementSession);

        /// <summary>
        /// Create new firewall rule given user subscription, Sql server resource and the firewall rule request
        /// </summary>
        /// <param name="azureResourceManagementSession">Subscription Context which includes credentials to use in the resource manager</param>
        /// <param name="azureSqlServer">Sql server resource to create firewall rule for</param>
        /// <param name="firewallRuleRequest">Firewall rule request including the name and IP address range</param>
        /// <returns></returns>
        Task<FirewallRuleResponse> CreateFirewallRuleAsync(
            IAzureResourceManagementSession azureResourceManagementSession, 
            IAzureSqlServerResource azureSqlServer, 
            FirewallRuleRequest firewallRuleRequest
            );

        Task<IAzureResourceManagementSession> CreateSessionAsync(IAzureUserAccountSubscriptionContext subscriptionContext);


        /// <summary>
        /// Gets all subscription contexts under a specific user account. Queries all tenants for the account and uses these to log in 
        /// and retrieve subscription information as needed
        /// <param name="userAccount">Account whose subscriptions should be queried</param>
        /// </summary>
        Task<IEnumerable<IAzureUserAccountSubscriptionContext>> GetSubscriptionContextsAsync(IAzureUserAccount userAccount);
    }
}
