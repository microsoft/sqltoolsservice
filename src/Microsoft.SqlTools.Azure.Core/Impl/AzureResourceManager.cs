//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.Management.Sql.Models;
using RestFirewallRule = Microsoft.Azure.Management.Sql.Models.FirewallRule;
using Microsoft.SqlTools.Azure.Core.Authentication;
using Microsoft.SqlTools.Azure.Core.FirewallRule;
using Microsoft.SqlTools.Azure.Core.Extensibility;
using Microsoft.SqlTools.Utility;
using Microsoft.Rest;
using System.Globalization;
using Microsoft.Rest.Azure;

namespace Microsoft.SqlTools.Azure.Core.Impl
{
    /// <summary>
    /// Default implementation for <see cref="IAzureResourceManager" />
    /// Provides functionality to get azure resources by making Http request to the Azure REST API
    /// </summary>
    [Exportable(
        ServerTypes.SqlServer,
        Categories.Azure,
        typeof(IAzureResourceManager),
        "Microsoft.SqlServer.ConnectionServices.Azure.Impl.VsAzureResourceManager",
        1)
    ]
    internal class AzureResourceManager : ExportableBase, IAzureResourceManager
    {
        private readonly Uri _resourceManagementUri = new Uri("https://management.azure.com/");

        public AzureResourceManager()
        {
            // Duplicate the exportable attribute as at present we do not support filtering using extensiondescriptor.
            // The attribute is preserved in order to simplify ability to backport into existing tools 
            Metadata = new ExportableMetadata(
                ServerTypes.SqlServer,
                Categories.Azure,
                "Microsoft.SqlServer.ConnectionServices.Azure.Impl.VsAzureResourceManager");
        }

        public async Task<IAzureResourceManagementSession> CreateSessionAsync(IAzureUserAccountSubscriptionContext subscriptionContext)
        {
            CommonUtil.CheckForNull(subscriptionContext, "subscriptionContext");
            try
            {
                ServiceClientCredentials credentials = await CreateCredentialsAsync(subscriptionContext);
                SqlManagementClient sqlManagementClient = new SqlManagementClient(_resourceManagementUri, credentials);
                ResourceManagementClient resourceManagementClient = new ResourceManagementClient(_resourceManagementUri, credentials);
                return new AzureResourceManagementSession(sqlManagementClient, resourceManagementClient, subscriptionContext);
            }
            catch (Exception ex)
            {
                Logger.Write(LogLevel.Error, string.Format(CultureInfo.CurrentCulture, "Failed to get databases {0}", ex));
                throw;
            }
        }

        /// <summary>
        /// Returns a list of azure databases given subscription resource group name and server name
        /// </summary>
        /// <param name="azureResourceManagementSession">Subscription Context which includes credentials to use in the resource manager</param>
        /// <param name="resourceGroupName">Resource Group Name</param>
        /// <param name="serverName">Server name</param>
        /// <returns>The list of databases</returns>
        public async Task<IEnumerable<IAzureResource>> GetAzureDatabasesAsync(
            IAzureResourceManagementSession azureResourceManagementSession, 
            string resourceGroupName,
            string serverName)
        {
            CommonUtil.CheckForNull(azureResourceManagementSession, "azureResourceManagerSession");
            try
            {
                AzureResourceManagementSession vsAzureResourceManagementSession = azureResourceManagementSession as AzureResourceManagementSession;

                if (vsAzureResourceManagementSession != null)
                {
                    try
                    {
                        IEnumerable<Database> databaseListResponse = await vsAzureResourceManagementSession.SqlManagementClient.Databases.ListByServerAsync(resourceGroupName, serverName);
                        return databaseListResponse.Select(
                                    x => new AzureResourceWrapper(x) { ResourceGroupName = resourceGroupName });
                    }
                    catch(HttpOperationException ex)
                    {
                        throw new AzureResourceFailedException(SR.FailedToGetAzureDatabasesErrorMessage, ex.Response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(LogLevel.Error, string.Format(CultureInfo.CurrentCulture, "Failed to get databases {0}", ex.Message));
                throw;
            }

            return null;
        }

        /// <summary>
        /// Returns a list of azure servers given subscription
        /// </summary>
        /// <param name="azureResourceManagementSession">Subscription Context which includes credentials to use in the resource manager</param>
        /// <returns>The list of Sql server resources</returns>
        public async Task<IEnumerable<IAzureSqlServerResource>> GetSqlServerAzureResourcesAsync(
            IAzureResourceManagementSession azureResourceManagementSession)
        {
            CommonUtil.CheckForNull(azureResourceManagementSession, "azureResourceManagerSession");           
            List<IAzureSqlServerResource> sqlServers = new List<IAzureSqlServerResource>();
            try
            {
                AzureResourceManagementSession vsAzureResourceManagementSession = azureResourceManagementSession as AzureResourceManagementSession;
                if(vsAzureResourceManagementSession != null)
                {
                    IEnumerable<ResourceGroup> resourceGroupNames = await GetResourceGroupsAsync(vsAzureResourceManagementSession);
                    if (resourceGroupNames != null)
                    {
                        foreach (ResourceGroup resourceGroupExtended in resourceGroupNames)
                        {
                            try
                            {
                                IServersOperations serverOperations = vsAzureResourceManagementSession.SqlManagementClient.Servers;
                                IPage<Server> servers = await serverOperations.ListByResourceGroupAsync(resourceGroupExtended.Name);
                                if (servers != null)
                                {
                                    sqlServers.AddRange(servers.Select(x =>
                                        new SqlAzureResource(x) { ResourceGroupName = resourceGroupExtended.Name }));
                                }
                            }
                            catch (HttpOperationException ex)
                            {
                                throw new AzureResourceFailedException(SR.FailedToGetAzureSqlServersErrorMessage, ex.Response.StatusCode);
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                TraceException(TraceEventType.Error, (int) TraceId.AzureResource, ex, "Failed to get servers");
                throw;
            }

            return sqlServers;
        }

        public async Task<FirewallRuleResponse> CreateFirewallRuleAsync(
            IAzureResourceManagementSession azureResourceManagementSession,
            IAzureSqlServerResource azureSqlServer, 
            FirewallRuleRequest firewallRuleRequest)
        {
            CommonUtil.CheckForNull(azureResourceManagementSession, "azureResourceManagerSession");
            CommonUtil.CheckForNull(firewallRuleRequest, "firewallRuleRequest");
            CommonUtil.CheckForNull(azureSqlServer, "azureSqlServer");

            try
            {
                AzureResourceManagementSession vsAzureResourceManagementSession = azureResourceManagementSession as AzureResourceManagementSession;

                if (vsAzureResourceManagementSession != null)
                {
                    try
                    {
                        var firewallRule = new RestFirewallRule()
                        {
                            EndIpAddress = firewallRuleRequest.EndIpAddress.ToString(),
                            StartIpAddress = firewallRuleRequest.StartIpAddress.ToString()
                        };
                        IFirewallRulesOperations firewallRuleOperations = vsAzureResourceManagementSession.SqlManagementClient.FirewallRules;
                        var firewallRuleResponse = await firewallRuleOperations.CreateOrUpdateAsync(
                                                                                    azureSqlServer.ResourceGroupName,
                                                                                    azureSqlServer.Name,
                                                                                    firewallRuleRequest.FirewallRuleName,
                                                                                    firewallRule);
                        return new FirewallRuleResponse()
                        {
                            StartIpAddress = firewallRuleResponse.StartIpAddress,
                            EndIpAddress = firewallRuleResponse.EndIpAddress,
                            Created = true
                        };
                    }
                    catch (HttpOperationException ex)
                    {
                        throw new AzureResourceFailedException(SR.FirewallRuleCreationFailed, ex.Response.StatusCode);
                    }
                }
                // else respond with failure case
                return  new FirewallRuleResponse()
                {                    
                    Created = false
                };
            }
            catch (Exception ex)
            {
                TraceException(TraceEventType.Error, (int) TraceId.AzureResource, ex, "Failed to get databases");
                throw;
            }
        }

        /// <summary>
        /// Returns the azure resource groups for given subscription
        /// </summary>
        private async Task<IEnumerable<ResourceGroup>> GetResourceGroupsAsync(AzureResourceManagementSession vsAzureResourceManagementSession)
        {
            try
            {
                if (vsAzureResourceManagementSession != null)
                {
                    try
                    {
                        IResourceGroupsOperations resourceGroupOperations = vsAzureResourceManagementSession.ResourceManagementClient.ResourceGroups;
                        IPage<ResourceGroup> resourceGroupList = await resourceGroupOperations.ListAsync();
                        if (resourceGroupList != null)
                        {
                            return resourceGroupList.AsEnumerable();
                        }

                    }
                    catch (HttpOperationException ex)
                    {
                        throw new AzureResourceFailedException(SR.FailedToGetAzureResourceGroupsErrorMessage, ex.Response.StatusCode);
                    }
                }

                return Enumerable.Empty<ResourceGroup>();
            }
            catch (Exception ex)
            {
                TraceException(TraceEventType.Error, (int)TraceId.AzureResource, ex, "Failed to get azure resource groups");
                throw;
            }
        }


        /// <summary>
        /// Creates credential instance for given subscription
        /// </summary>
        private Task<ServiceClientCredentials> CreateCredentialsAsync(IAzureUserAccountSubscriptionContext subscriptionContext)
        {
            AzureUserAccountSubscriptionContext azureUserSubContext =
                subscriptionContext as AzureUserAccountSubscriptionContext;

            if (azureUserSubContext != null)
            {
                return Task.FromResult(azureUserSubContext.Credentials);
            }
            throw new NotSupportedException("This uses an unknown subscription type");
        }
    }
}
