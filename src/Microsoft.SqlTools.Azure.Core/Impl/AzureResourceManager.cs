//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.Management.Sql.Models;
using Microsoft.SqlTools.Azure.Core.Authentication;
using Microsoft.SqlTools.Azure.Core.FirewallRule;
using Microsoft.SqlTools.Azure.Core.Extensibility;
using Microsoft.SqlTools.Utility;
using Microsoft.Rest;
using System.Threading;
using System.Globalization;

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
                                    x => new VsAzureResource(x) { ResourceGroupName = resourceGroupName });
                    }
                    catch(HttpOperationException ex)
                    {
                        throw new AzureResourceFailedException(SR.FailedToGetAzureDatabasesErrorMessage, ex.Response.StatusCode);
                    }
                        if (databaseListResponse.StatusCode == HttpStatusCode.OK)
                        {
                            
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
                    IEnumerable<TrackedResource> resourceGroupNames = await GetResourceGroupsAsync(vsAzureResourceManagementSession);
                    if (resourceGroupNames != null)
                    {
                        foreach (ResourceGroupExtended resourceGroupExtended in resourceGroupNames)
                        {
                            IServerOperations serverOperations = vsAzureResourceManagementSession.SqlManagementClient.Servers;
                            ServerListResponse serverListResponse =
                                await serverOperations.ListAsync(resourceGroupExtended.Name);
                            if (serverListResponse != null && serverListResponse.Servers != null &&
                                serverListResponse.StatusCode == HttpStatusCode.OK)
                            {
                                sqlServers.AddRange(serverListResponse.Servers.Select(x =>
                                    new SqlAzureResource(x) {ResourceGroupName = resourceGroupExtended.Name}));
                            }
                            else
                            {
                                throw new AzureResourceFailedException(
                                    SR.FailedToGetAzureSqlServersErrorMessage,
                                    serverListResponse != null ? serverListResponse.StatusCode : HttpStatusCode.InternalServerError);
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                TraceException(TraceEventType.Error, (int) TraceId.AzureResource, ex, "Failed to get databases");
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
                VsAzureResourceManagementSession vsAzureResourceManagementSession = azureResourceManagementSession as VsAzureResourceManagementSession;

                if(vsAzureResourceManagementSession != null)
                {
                    FirewallRuleCreateOrUpdateParameters firewallRuleParameters =
                        new FirewallRuleCreateOrUpdateParameters(new FirewallRuleCreateOrUpdateProperties()
                        {
                            EndIpAddress = firewallRuleRequest.EndIpAddress.ToString(),
                            StartIpAddress = firewallRuleRequest.StartIpAddress.ToString()
                        });
                    IFirewallRuleOperations firewallRuleOperations = vsAzureResourceManagementSession.SqlManagementClient.FirewallRules;
                    FirewallRuleGetResponse firewallRuleGetResponse = await firewallRuleOperations.CreateOrUpdateAsync(
                        azureSqlServer.ResourceGroupName,
                        azureSqlServer.Name,
                        firewallRuleRequest.FirewallRuleName,
                        firewallRuleParameters);
                    if (firewallRuleGetResponse != null && firewallRuleGetResponse.FirewallRule != null)
                    {
                        if (firewallRuleGetResponse.StatusCode == HttpStatusCode.OK ||
                            firewallRuleGetResponse.StatusCode == HttpStatusCode.Created)
                        {
                            return new FirewallRuleResponse()
                            {
                                StartIpAddress = firewallRuleGetResponse.FirewallRule.Properties.StartIpAddress,
                                EndIpAddress = firewallRuleGetResponse.FirewallRule.Properties.EndIpAddress,
                                Created = true
                            };
                        }
                        throw new AzureResourceFailedException(SR.FirewallRuleCreationFailed, firewallRuleGetResponse.StatusCode);
                    }
                }
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
        private async Task<IEnumerable<TrackedResource>> GetResourceGroupsAsync(AzureResourceManagementSession vsAzureResourceManagementSession)
        {
            try
            {
                if (vsAzureResourceManagementSession != null)
                {
                    ResourceGroupListParameters resourceGroupListParameters = new ResourceGroupListParameters();
                    IResourceGroupOperations resourceGroupOperations = vsAzureResourceManagementSession.ResourceManagementClient.ResourceGroups;
                    if (resourceGroupOperations != null)
                    {

                        ResourceGroupListResult resourceGroupList =
                            await resourceGroupOperations.ListAsync(resourceGroupListParameters);
                        if (resourceGroupList != null)
                        {
                            if (resourceGroupList.StatusCode == HttpStatusCode.OK)
                            {
                                return resourceGroupList.ResourceGroups;
                            }
                            throw new AzureResourceFailedException(
                                SR.FailedToGetAzureResourceGroupsErrorMessage,
                                resourceGroupList.StatusCode);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceException(TraceEventType.Error, (int)TraceId.AzureResource, ex, "Failed to get azure resource groups");
                throw;
            }

            return Enumerable.Empty<TrackedResource>();
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
