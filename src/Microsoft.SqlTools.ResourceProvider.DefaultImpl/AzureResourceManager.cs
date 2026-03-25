//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Sql;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;
using Microsoft.SqlTools.ResourceProvider.Core.Firewall;
using Microsoft.SqlTools.Utility;

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
                string armEndpoint = subscriptionContext.UserAccount.UnderlyingAccount.Properties.ProviderSettings?.Settings?.ArmResource?.Endpoint;
                Uri armUri = null;
                if (armEndpoint != null)
                {
                    try
                    {
                        armUri = new Uri(armEndpoint);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception while parsing URI: {e.Message}");
                    }
                }

                TokenCredential credentials = GetCredentials(subscriptionContext);
                ArmClient armClient = CreateArmClient(credentials, subscriptionContext.Subscription.SubscriptionId, armUri);

                return Task.FromResult<IAzureResourceManagementSession>(new AzureResourceManagementSession(armClient, subscriptionContext));
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format(CultureInfo.CurrentCulture, "Failed to get databases {0}", ex));
                throw;
            }
        }

        /// <summary>
        /// Returns a list of azure databases given subscription resource group name and server name
        /// </summary>
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
                    string subId = vsAzureResourceManagementSession.SubscriptionContext.Subscription.SubscriptionId;
                    SubscriptionResource subRes = vsAzureResourceManagementSession.ArmClient
                        .GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subId));
                    ResourceGroupResource rgRes = (await subRes.GetResourceGroupAsync(resourceGroupName)).Value;
                    SqlServerResource serverRes = (await rgRes.GetSqlServers().GetAsync(serverName, null, CancellationToken.None)).Value;

                    IEnumerable<AzureResourceInfo> databaseList = await ExecuteCloudRequest(async () =>
                    {
                        var list = new List<AzureResourceInfo>();
                        await foreach (SqlDatabaseResource db in serverRes.GetSqlDatabases().GetAllAsync(null))
                        {
                            list.Add(new AzureResourceInfo(
                                db.Data.Name,
                                db.Data.Id?.ToString(),
                                db.Data.ResourceType.ToString(),
                                db.Data.Location.ToString()));
                        }
                        return (IEnumerable<AzureResourceInfo>)list;
                    }, SR.FailedToGetAzureDatabasesErrorMessage);

                    return databaseList.Select(x => new AzureResourceWrapper(x) { ResourceGroupName = resourceGroupName });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format(CultureInfo.CurrentCulture, "Failed to get databases {0}", ex.Message));
                throw;
            }

            return null;
        }

        /// <summary>
        /// Returns a list of azure servers given subscription
        /// </summary>
        public async Task<IEnumerable<IAzureSqlServerResource>> GetSqlServerAzureResourcesAsync(
            IAzureResourceManagementSession azureResourceManagementSession)
        {
            CommonUtil.CheckForNull(azureResourceManagementSession, "azureResourceManagerSession");
            List<IAzureSqlServerResource> sqlServers = new List<IAzureSqlServerResource>();
            try
            {
                AzureResourceManagementSession vsAzureResourceManagementSession = azureResourceManagementSession as AzureResourceManagementSession;
                if (vsAzureResourceManagementSession != null)
                {
                    string subId = vsAzureResourceManagementSession.SubscriptionContext.Subscription.SubscriptionId;
                    SubscriptionResource subRes = vsAzureResourceManagementSession.ArmClient
                        .GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subId));

                    await foreach (SqlServerResource server in subRes.GetSqlServersAsync(null))
                    {
                        sqlServers.Add(new SqlAzureResource(server.Data));
                    }
                }
            }
            catch (Exception ex)
            {
                TraceException(TraceEventType.Error, (int)TraceId.AzureResource, ex, "Failed to get servers");
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
                    string subId = vsAzureResourceManagementSession.SubscriptionContext.Subscription.SubscriptionId;
                    SubscriptionResource subRes = vsAzureResourceManagementSession.ArmClient
                        .GetSubscriptionResource(SubscriptionResource.CreateResourceIdentifier(subId));
                    ResourceGroupResource rgRes = (await subRes.GetResourceGroupAsync(azureSqlServer.ResourceGroupName ?? string.Empty)).Value;
                    SqlServerResource serverRes = (await rgRes.GetSqlServers().GetAsync(azureSqlServer.Name, null, CancellationToken.None)).Value;

                    var firewallData = new SqlFirewallRuleData
                    {
                        StartIPAddress = firewallRuleRequest.StartIpAddress.ToString(),
                        EndIPAddress = firewallRuleRequest.EndIpAddress.ToString()
                    };

                    ArmOperation<SqlFirewallRuleResource> operation = await ExecuteCloudRequest(
                        () => serverRes.GetSqlFirewallRules().CreateOrUpdateAsync(
                            WaitUntil.Completed,
                            firewallRuleRequest.FirewallRuleName,
                            firewallData),
                        SR.FirewallRuleCreationFailedWithError);

                    SqlFirewallRuleData result = operation.Value.Data;
                    return new FirewallRuleResponse()
                    {
                        StartIpAddress = result.StartIPAddress,
                        EndIpAddress = result.EndIPAddress,
                        Created = true
                    };
                }
                // else respond with failure case
                return new FirewallRuleResponse()
                {
                    Created = false
                };
            }
            catch (Exception ex)
            {
                TraceException(TraceEventType.Error, (int)TraceId.AzureResource, ex, "Failed to create firewall rule");
                throw;
            }
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
                TokenCredential credentials = CreateCredentials(azureTenant);
                string armEndpoint = userAccount.UnderlyingAccount.Properties.ProviderSettings?.Settings?.ArmResource?.Endpoint;
                Uri armUri = null;
                if (armEndpoint != null)
                {
                    try
                    {
                        armUri = new Uri(armEndpoint);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception while parsing URI: {e.Message}");
                    }
                }

                ArmClient armClient = CreateArmClient(credentials, null, armUri);

                var contexts = new List<IAzureUserAccountSubscriptionContext>();
                await foreach (SubscriptionResource sub in armClient.GetSubscriptions().GetAllAsync())
                {
                    AzureSubscriptionIdentifier subId = new AzureSubscriptionIdentifier(
                        userAccount, azureTenant.TenantId, sub.Data.SubscriptionId, armUri ?? _resourceManagementUri);
                    contexts.Add(new AzureUserAccountSubscriptionContext(subId, credentials));
                }
                return new ServiceResponse<IAzureUserAccountSubscriptionContext>(contexts);
            }
            return new ServiceResponse<IAzureUserAccountSubscriptionContext>();
        }

        /// <summary>
        /// Creates an <see cref="ArmClient"/> with optional subscription ID and custom ARM endpoint
        /// </summary>
        private static ArmClient CreateArmClient(TokenCredential credentials, string subscriptionId, Uri armUri)
        {
            if (armUri != null)
            {
                ArmClientOptions options = new ArmClientOptions
                {
                    Environment = new ArmEnvironment(armUri, "https://management.azure.com/.default")
                };
                return new ArmClient(credentials, subscriptionId, options);
            }
            return subscriptionId != null
                ? new ArmClient(credentials, subscriptionId)
                : new ArmClient(credentials);
        }

        /// <summary>
        /// Creates a <see cref="TokenCredential"/> for the given tenant
        /// </summary>
        private TokenCredential CreateCredentials(IAzureTenant tenant)
        {
            AzureTenant azureTenant = tenant as AzureTenant;

            if (azureTenant != null)
            {
                if (!string.IsNullOrWhiteSpace(azureTenant.TokenType))
                {
                    return new StaticTokenCredential(azureTenant.AccessToken, azureTenant.TokenType);
                }
                return new StaticTokenCredential(azureTenant.AccessToken);
            }
            throw new NotSupportedException("This uses an unknown subscription type");
        }

        /// <summary>
        /// Gets the <see cref="TokenCredential"/> from the given subscription context
        /// </summary>
        private TokenCredential GetCredentials(IAzureUserAccountSubscriptionContext subscriptionContext)
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
            catch (RequestFailedException ex)
            {
                if (string.Equals(ExpiredTokenCode, ex.ErrorCode, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ExpiredTokenException(ex.Message);
                }
                throw new AzureResourceFailedException(
                    string.Format(CultureInfo.CurrentCulture, errorOccurredMsg, ex.Message), ex.Status);
            }
        }
    }
}
