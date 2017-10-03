//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;

namespace Microsoft.SqlTools.ResourceProvider.Core.FirewallRule
{

    internal interface IFirewallRuleService
    {
        /// <summary>
        /// Creates firewall rule for given server name and IP address range. Throws exception if operation fails
        /// </summary>   
        Task<FirewallRuleResponse> CreateFirewallRuleAsync(string serverName, string startIpAddressValue, string endIpAddressValue);


        /// <summary>
        /// Creates firewall rule for given server name and IP address range. Throws exception if operation fails
        /// </summary> 
        Task<FirewallRuleResponse> CreateFirewallRuleAsync(string serverName, IPAddress startIpAddress, IPAddress endIpAddress);


        /// <summary>
        /// Sets and gets azure resource manager instance. can be used by unit tests
        /// </summary>
        IAzureResourceManager ResourceManager
        {
            get; set;
        }

        /// <summary>
        /// Sets and gets authentication manager instance. can be used by unit tests
        /// </summary>
        IAzureAuthenticationManager AuthenticationManager
        {
            get; set;
        }
    }

    /// <summary>
    /// Service to be used by the controls to create firewall rule
    /// </summary>
    internal class FirewallRuleService : IFirewallRuleService
    {
        /// <summary>
        /// Creates firewall rule for given server name and IP address range. Throws exception if operation fails
        /// </summary>     
        public async Task<FirewallRuleResponse> CreateFirewallRuleAsync(string serverName, string startIpAddressValue, string endIpAddressValue)
        {
            IPAddress startIpAddress = ConvertToIpAddress(startIpAddressValue);
            IPAddress endIpAddress = ConvertToIpAddress(endIpAddressValue);
            return await CreateFirewallRuleAsync(serverName, startIpAddress, endIpAddress);
        }

        /// <summary>
        /// Creates firewall rule for given server name and IP address range. Throws exception if operation fails
        /// </summary> 
        public async Task<FirewallRuleResponse> CreateFirewallRuleAsync(string serverName,  IPAddress startIpAddress, IPAddress endIpAddress)
        {
            try
            {
                FirewallRuleResponse firewallRuleResponse = new FirewallRuleResponse() { Created = false };
                CommonUtil.CheckStringForNullOrEmpty(serverName, "serverName");
                CommonUtil.CheckForNull(startIpAddress, "startIpAddress");
                CommonUtil.CheckForNull(endIpAddress, "endIpAddress");

                IAzureAuthenticationManager authenticationManager = AuthenticationManager;

                if (authenticationManager != null && !await authenticationManager.GetUserNeedsReauthenticationAsync())
                {                    
                    FirewallRuleResource firewallRuleResource = await FindAzureResourceAsync(serverName);
                    firewallRuleResponse = await CreateFirewallRule(firewallRuleResource, startIpAddress, endIpAddress);
                }
                if (firewallRuleResponse == null || !firewallRuleResponse.Created)
                {
                    throw new FirewallRuleException(SR.FirewallRuleCreationFailed);
                }
                return firewallRuleResponse;
            }
            catch (ServiceExceptionBase)
            {                
                throw;
            }
            catch (Exception ex)
            {
                throw new FirewallRuleException(SR.FirewallRuleCreationFailed, ex);
            }
        }

        /// <summary>
        /// Sets and gets azure resource manager instance. can be used by unit tests
        /// </summary>
        public IAzureResourceManager ResourceManager { get; set; }

        /// <summary>
        /// Sets and gets authentication manager instance. can be used by unit tests
        /// </summary>
        public IAzureAuthenticationManager AuthenticationManager { get; set; }

        /// <summary>
        /// Creates firewall rule for given subscription and IP address range
        /// </summary>        
        private async Task<FirewallRuleResponse> CreateFirewallRule(FirewallRuleResource firewallRuleResource, IPAddress startIpAddress, IPAddress endIpAddress)
        {
            CommonUtil.CheckForNull(firewallRuleResource, "firewallRuleResource");

            try
            {
                if (firewallRuleResource.IsValid)
                {

                    FirewallRuleRequest request = new FirewallRuleRequest()
                    {
                        StartIpAddress = startIpAddress,
                        EndIpAddress = endIpAddress
                    };
                    using (IAzureResourceManagementSession session = await ResourceManager.CreateSessionAsync(firewallRuleResource.SubscriptionContext))
                    {
                        return await ResourceManager.CreateFirewallRuleAsync(
                            session,
                            firewallRuleResource.AzureResource,
                            request);
                    }
                }
            }
            catch (ServiceExceptionBase)
            {
                throw;
            }          
            catch (Exception ex)
            {
                throw new FirewallRuleException(SR.FirewallRuleCreationFailed, ex);
            }

            return new FirewallRuleResponse()
            {
                Created = false
            };
        }

        /// <summary>
        /// Finds an azure resource for given server name under user's subscriptions
        /// </summary>
        private async Task<FirewallRuleResource> FindAzureResourceAsync(string serverName)
        {
            try
            {
                IEnumerable<IAzureUserAccountSubscriptionContext> subscriptions = await AuthenticationManager.GetSubscriptionsAsync();

                if (subscriptions == null)
                {
                    throw new FirewallRuleException(SR.FirewallRuleCreationFailed);
                }

                foreach (IAzureUserAccountSubscriptionContext subscription in subscriptions)
                {
                    using (IAzureResourceManagementSession session = await ResourceManager.CreateSessionAsync(subscription))
                    {
                        IAzureSqlServerResource azureSqlServer = await FindAzureResourceForSubscriptionAsync(serverName, session);

                        if (azureSqlServer != null)
                        {
                            return new FirewallRuleResource()
                            {
                                SubscriptionContext = subscription,
                                AzureResource = azureSqlServer
                            };
                        }
                    }
                }
                var currentUser = await AuthenticationManager.GetCurrentAccountAsync();

                throw new FirewallRuleException(string.Format(CultureInfo.CurrentCulture, SR.AzureServerNotFound, serverName, currentUser != null ? currentUser.UniqueId : string.Empty));
            }
            catch (ServiceExceptionBase)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FirewallRuleException(SR.FirewallRuleCreationFailed, ex);
            }
        }

        /// <summary>
        /// Throws a firewallRule exception based on give status code
        /// </summary>
        private void HandleError(ServiceExceptionBase exception, string serverName, 
            IAzureUserAccountSubscriptionContext subscription)
        {
            var accountName = subscription != null && subscription.UserAccount != null &&
                              subscription.UserAccount.DisplayInfo != null
                ? subscription.UserAccount.DisplayInfo.UserName
                : string.Empty;

            switch (exception.HttpStatusCode)
            {
                case HttpStatusCode.NotFound:
                    throw new FirewallRuleException(string.Format(CultureInfo.CurrentCulture, SR.AzureServerNotFound, serverName, accountName), exception);
                case HttpStatusCode.Forbidden:
                    throw new FirewallRuleException(string.Format(CultureInfo.CurrentCulture, SR.FirewallRuleAccessForbidden, accountName), exception);
                default:
                    throw new FirewallRuleException(SR.FirewallRuleCreationFailed, exception.HttpStatusCode, exception);
            }
        }

        /// <summary>
        /// Finds Azure resource for the given subscription and server name
        /// </summary>
        private async Task<IAzureSqlServerResource> FindAzureResourceForSubscriptionAsync(
            string serverName,
            IAzureResourceManagementSession session)
        {
            try
            {
                IEnumerable<IAzureSqlServerResource> resources = await ResourceManager.GetSqlServerAzureResourcesAsync(session);

                if (resources == null)
                {
                    return null;
                }
                foreach (IAzureSqlServerResource resource in resources)
                {
                    if (serverName.Equals(resource.FullyQualifiedDomainName, StringComparison.OrdinalIgnoreCase))
                    {
                        return resource;
                    }
                }       
            }
            catch (ServiceExceptionBase ex)
            {
                HandleError(ex, serverName, session.SubscriptionContext);
            } 
            catch (Exception ex)
            {
                throw new FirewallRuleException(SR.FirewallRuleCreationFailed, ex);
            }            
            return null;
        }
      
        private IPAddress ConvertToIpAddress(string ipAddressValue)
        {
            IPAddress ipAddress;
            if (!IPAddress.TryParse(ipAddressValue, out ipAddress))
            {
                throw new FirewallRuleException(SR.InvalidIpAddress);
            }
            return ipAddress;
        }
    }
}
