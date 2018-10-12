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
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Microsoft.SqlTools.ResourceProvider.Core.Firewall;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;
using Microsoft.SqlTools.Utility;
using Microsoft.Rest;
using System.Globalization;
using Microsoft.Rest.Azure;
using Microsoft.SqlTools.ResourceProvider.Core;
using System.Threading;

namespace Microsoft.SqlTools.ResourceProvider.DefaultImpl
{
    /// <summary>
    /// Default implementation for <see cref="IAzureResourceManager" />
    /// Provides functionality to get azure resources by making Http request to the Azure REST API
    /// </summary>
    [Exportable(
        ServerTypes.SqlServer,
        Categories.Azure,
        typeof(IAzureResourceManager),
        "Microsoft.SqlTools.ResourceProvider.DefaultImpl.AzureResourceManager",
        1)
    ]
    public class AzureResourceManager : ExportableBase, IAzureResourceManager
    {
        private readonly Uri _resourceManagementUri = new Uri("https://management.azure.com/");
        private const string ExpiredTokenCode = "ExpiredAuthenticationToken";
        
        public AzureResourceManager()
        {
            // Duplicate the exportable attribute as at present we do not support filtering using extensiondescriptor.
            // The attribute is preserved in order to simplify ability to backport into existing tools 
            Metadata = new ExportableMetadata(
                ServerTypes.SqlServer,
                Categories.Azure,
                "Microsoft.SqlTools.ResourceProvider.DefaultImpl.AzureResourceManager");
        }

        public Task<IAzureResourceManagementSession> CreateSessionAsync(IAzureUserAccountSubscriptionContext subscriptionContext)
        {
            CommonUtil.CheckForNull(subscriptionContext, "subscriptionContext");
            try
            {
                ServiceClientCredentials credentials = CreateCredentials(subscriptionContext);
                SqlManagementClient sqlManagementClient = new SqlManagementClient(credentials)
                {
                    SubscriptionId = subscriptionContext.Subscription.SubscriptionId
                };
                ResourceManagementClient resourceManagementClient = new ResourceManagementClient(credentials)
                {
                    SubscriptionId = subscriptionContext.Subscription.SubscriptionId
                };
                return Task.FromResult<IAzureResourceManagementSession>(new AzureResourceManagementSession(sqlManagementClient, resourceManagementClient, subscriptionContext));
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Error, string.Format(CultureInfo.CurrentCulture, "Failed to get databases {0}", ex));
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
                    IEnumerable<Database> databaseListResponse = await ExecuteCloudRequest(
                        () => vsAzureResourceManagementSession.SqlManagementClient.Databases.ListByServerAsync(resourceGroupName, serverName),
                        SR.FailedToGetAzureDatabasesErrorMessage);
                    return databaseListResponse.Select(x => new AzureResourceWrapper(x) { ResourceGroupName = resourceGroupName });
                }
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Error, string.Format(CultureInfo.CurrentCulture, "Failed to get databases {0}", ex.Message));
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
                    IServersOperations serverOperations = vsAzureResourceManagementSession.SqlManagementClient.Servers;
                    IPage<Server> servers = await ExecuteCloudRequest(
                        () => serverOperations.ListAsync(),
                        SR.FailedToGetAzureSqlServersWithError);
                    if (servers != null)
                    {
                        sqlServers.AddRange(servers.Select(server => {
                            var serverResource = new SqlAzureResource(server);
                            // TODO ResourceGroup name
                            return serverResource;
                        }));
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
                    var firewallRule = new RestFirewallRule()
                    {
                        EndIpAddress = firewallRuleRequest.EndIpAddress.ToString(),
                        StartIpAddress = firewallRuleRequest.StartIpAddress.ToString()
                    };
                    IFirewallRulesOperations firewallRuleOperations = vsAzureResourceManagementSession.SqlManagementClient.FirewallRules;
                    var firewallRuleResponse = await ExecuteCloudRequest(
                        () => firewallRuleOperations.CreateOrUpdateWithHttpMessagesAsync(
                                                                                azureSqlServer.ResourceGroupName ?? string.Empty,
                                                                                azureSqlServer.Name,
                                                                                firewallRuleRequest.FirewallRuleName,
                                                                                firewallRule,
                                                                                GetCustomHeaders()),
                        SR.FirewallRuleCreationFailedWithError);
                    var response = firewallRuleResponse.Body;
                    return new FirewallRuleResponse()
                    {
                        StartIpAddress = response.StartIpAddress,
                        EndIpAddress = response.EndIpAddress,
                        Created = true
                    };
                }
                // else respond with failure case
                return  new FirewallRuleResponse()
                {                    
                    Created = false
                };
            }
            catch (Exception ex)
            {
                TraceException(TraceEventType.Error, (int) TraceId.AzureResource, ex, "Failed to create firewall rule");
                throw;
            }
        }

        private Dictionary<string,List<string>> GetCustomHeaders()
        {
            // For some unknown reason the firewall rule method defaults to returning XML. Fixes this by adding an Accept header
            // ensuring it's always JSON
            var headers = new Dictionary<string,List<string>>();
            headers["Accept"] = new List<string>() {
                "application/json"
            };
            return headers;
        }

        /// <summary>
        /// Gets all subscription contexts under a specific user account. Queries all tenants for the account and uses these to log in 
        /// and retrieve subscription information as needed
        /// </summary>
        public async Task<IEnumerable<IAzureUserAccountSubscriptionContext>> GetSubscriptionContextsAsync(IAzureUserAccount userAccount)
        {
            List<IAzureUserAccountSubscriptionContext> contexts = new List<IAzureUserAccountSubscriptionContext>();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            ServiceResponse<IAzureUserAccountSubscriptionContext> response = await AzureUtil.ExecuteGetAzureResourceAsParallel(
                userAccount, userAccount.AllTenants, string.Empty, CancellationToken.None, GetSubscriptionsForTentantAsync);
            
            if (response.HasError)
            {
                var ex = response.Errors.First();
                throw new AzureResourceFailedException(
                            string.Format(CultureInfo.CurrentCulture, SR.FailedToGetAzureSubscriptionsErrorMessage, ex.Message));
            }
            contexts.AddRange(response.Data);
            stopwatch.Stop();
            TraceEvent(TraceEventType.Verbose, (int)TraceId.AzureResource, "Time taken to get all subscriptions was {0}ms", stopwatch.ElapsedMilliseconds.ToString());
            return contexts;
        }

        private async Task<ServiceResponse<IAzureUserAccountSubscriptionContext>> GetSubscriptionsForTentantAsync(
            IAzureUserAccount userAccount, IAzureTenant tenant, string lookupKey,
            CancellationToken cancellationToken, CancellationToken internalCancellationToken)
        {

            AzureTenant azureTenant = tenant as AzureTenant;
            if (azureTenant != null)
            {
                ServiceClientCredentials credentials = CreateCredentials(azureTenant);
                using (SubscriptionClient client = new SubscriptionClient(_resourceManagementUri, credentials))
                {
                    IEnumerable<Subscription> subs = await GetSubscriptionsAsync(client);
                    return new ServiceResponse<IAzureUserAccountSubscriptionContext>(subs.Select(sub =>
                    {
                        AzureSubscriptionIdentifier subId = new AzureSubscriptionIdentifier(userAccount, azureTenant.TenantId, sub.SubscriptionId, _resourceManagementUri);
                        AzureUserAccountSubscriptionContext context = new AzureUserAccountSubscriptionContext(subId, credentials);
                        return context;
                    }));
                }
            }
            return new ServiceResponse<IAzureUserAccountSubscriptionContext>();
        }

        /// <summary>
        /// Returns the azure resource groups for given subscription
        /// </summary>
        private async Task<IEnumerable<Subscription>> GetSubscriptionsAsync(SubscriptionClient subscriptionClient)
        {
            try
            {
                if (subscriptionClient != null)
                {
                    IPage<Subscription> subscriptionList = await ExecuteCloudRequest(
                        () => subscriptionClient.Subscriptions.ListAsync(),
                        SR.FailedToGetAzureSubscriptionsErrorMessage);
                    if (subscriptionList != null)
                    {
                        return subscriptionList.AsEnumerable();
                    }
                }

                return Enumerable.Empty<Subscription>();
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
        private ServiceClientCredentials CreateCredentials(IAzureTenant tenant)
        {
            AzureTenant azureTenant = tenant as AzureTenant;

            if (azureTenant != null)
            {
                TokenCredentials credentials;
                if (!string.IsNullOrWhiteSpace(azureTenant.TokenType))
                {
                    credentials = new TokenCredentials(azureTenant.AccessToken, azureTenant.TokenType);
                }
                else
                {
                    credentials = new TokenCredentials(azureTenant.AccessToken);
                }

                return credentials;
            }
            throw new NotSupportedException("This uses an unknown subscription type");
        }

        /// <summary>
        /// Creates credential instance for given subscription
        /// </summary>
        private ServiceClientCredentials CreateCredentials(IAzureUserAccountSubscriptionContext subscriptionContext)
        {
            AzureUserAccountSubscriptionContext azureUserSubContext =
                subscriptionContext as AzureUserAccountSubscriptionContext;

            if (azureUserSubContext != null)
            {
                return azureUserSubContext.Credentials;
            }
            throw new NotSupportedException("This uses an unknown subscription type");
        }

        private async Task<T> ExecuteCloudRequest<T>(Func<Task<T>> operation, string errorOccurredMsg)
        {
            try
            {
                return await operation();
            }
            catch(CloudException ex)
            {
                if (ex.Body != null && string.Equals(ExpiredTokenCode, ex.Body.Code, StringComparison.OrdinalIgnoreCase))
                {
                    // Throw an expired token exception, which indicates that the operation could succeed if the user reauthenticates
                    throw new ExpiredTokenException(ex.Message);
                }
                throw new AzureResourceFailedException(
                    string.Format(CultureInfo.CurrentCulture, errorOccurredMsg, ex.Message), ex.Response.StatusCode);
            }
            catch (HttpOperationException ex)
            {
                throw new AzureResourceFailedException(
                    string.Format(CultureInfo.CurrentCulture, errorOccurredMsg, ex.Message), ex.Response.StatusCode);
            }

        }
    }
}
