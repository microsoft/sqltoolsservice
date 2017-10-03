//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Azure.Core.Authentication;
using Microsoft.SqlTools.Azure.Core.Extensibility;

namespace Microsoft.SqlTools.Azure.Core
{
    /// <summary>
    /// Default implementation for <see cref="IServerDiscoveryProvider"/> for Azure Sql servers. 
    /// A discovery provider capable of finding Sql Azure servers for a specific Azure user account. 
    /// </summary>
    [Exportable(
        ServerTypes.SqlServer, 
        Categories.Azure,
        typeof(IServerDiscoveryProvider),
        "Microsoft.SqlServer.ConnectionServices.Azure.AzureServerDiscoveryProvider")]
    internal class AzureSqlServerDiscoveryProvider : ExportableBase, IServerDiscoveryProvider, ISecureService
    {
        private IAzureResourceManager _azureResourceManagerWrapper;
        private IAzureAuthenticationManager _azureAccountManager;

        public AzureSqlServerDiscoveryProvider()
        {
            // Duplicate the exportable attribute as at present we do not support filtering using extensiondescriptor.
            // The attribute is preserved in order to simplify ability to backport into existing tools 
            Metadata = new ExportableMetadata(
                ServerTypes.SqlServer,
                Categories.Azure,
                "Microsoft.SqlServer.ConnectionServices.Azure.AzureServerDiscoveryProvider");
        }

        public async Task<ServiceResponse<ServerInstanceInfo>> GetServerInstancesAsync()
        {
            ServiceResponse<ServerInstanceInfo> result = new ServiceResponse<ServerInstanceInfo>();
            List<ServerInstanceInfo> serverInstances = new List<ServerInstanceInfo>();

            if (AccountManager != null && AzureAccountManager != null && AzureResourceManager != null)
            {
                try
                {
                    IEnumerable<IAzureUserAccountSubscriptionContext> subscriptions =
                        await AzureAccountManager.GetSelectedSubscriptionsAsync();
                    if (subscriptions != null)
                    {
                        foreach (IAzureUserAccountSubscriptionContext subscription in subscriptions)
                        {
                            using (IAzureResourceManagementSession session = await AzureResourceManager.CreateSessionAsync(subscription))
                            {
                                IEnumerable<IAzureSqlServerResource> azureResources =
                                    await AzureResourceManager.GetSqlServerAzureResourcesAsync(session);
                                serverInstances.AddRange(
                                    azureResources.Select(x =>
                                        new ServerInstanceInfo(ServerDefinition)
                                        {
                                            Name = x.Name,
                                            FullyQualifiedDomainName = x.FullyQualifiedDomainName,
                                            AdministratorLogin = x.AdministratorLogin
                                        }));
                            }
                        }
                    }
                    result = new ServiceResponse<ServerInstanceInfo>(serverInstances);
                }
                catch (Exception ex)
                {
                    result = new ServiceResponse<ServerInstanceInfo>(serverInstances, new List<Exception>() {ex});
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the resource manager that has same metadata as this class
        /// </summary>
        public IAzureResourceManager AzureResourceManager
        {
            get
            {
                return (_azureResourceManagerWrapper =
                    _azureResourceManagerWrapper ??
                    GetService<IAzureResourceManager>());
            }
            internal set
            {
                _azureResourceManagerWrapper = value;
            }
        }

        /// <summary>
        /// Returns the account manager that has same metadata as this class
        /// </summary>
        public IAzureAuthenticationManager AzureAccountManager
        {
            get
            {
                return (_azureAccountManager =
                    _azureAccountManager ??
                    GetService<IAzureAuthenticationManager>());
            }
        }

        /// <summary>
        /// Account Manager
        /// </summary>
        public IAccountManager AccountManager
        {
            get
            {
                return AzureAccountManager;

            }
            internal set
            {
                _azureAccountManager = value as IAzureAuthenticationManager;
            }
        }        
    }
}
