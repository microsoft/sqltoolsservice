//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Azure.Core.Authentication;
using Microsoft.SqlTools.Azure.Core.Extensibility;

namespace Microsoft.SqlTools.Azure.Core
{
    /// <summary>
    /// Default implementation for <see cref="IDatabaseDiscoveryProvider"/> for Azure Sql databases. 
    /// A discovery provider capable of finding Sql Azure databases for a specific Azure user account. 
    /// </summary>

    [Exportable(
        ServerTypes.SqlServer,
        Categories.Azure,
        typeof(IDatabaseDiscoveryProvider),
        "Microsoft.SqlServer.ConnectionServices.Azure.AzureDatabaseDiscoveryProvider")]
    internal class AzureDatabaseDiscoveryProvider : ExportableBase, IDatabaseDiscoveryProvider, ISecureService, ICacheable<ServiceResponse<DatabaseInstanceInfo>>
    {
        private IAzureResourceManager _azureResourceManagerWrapper;
        private IAzureAuthenticationManager _azureAccountManager;
        private IDatabaseDiscoveryProvider _defaultDatabaseDiscoveryProvider;
        private readonly ConcurrentCache<ServiceResponse<DatabaseInstanceInfo>> _cache = new ConcurrentCache<ServiceResponse<DatabaseInstanceInfo>>();

        public AzureDatabaseDiscoveryProvider()
        {
            // Duplicate the exportable attribute as at present we do not support filtering using extensiondescriptor.
            // The attribute is preserved in order to simplify ability to backport into existing tools 
            Metadata = new ExportableMetadata(
                ServerTypes.SqlServer,
                Categories.Azure,
                "Microsoft.SqlServer.ConnectionServices.Azure.AzureDatabaseDiscoveryProvider");
        }

        /// <summary>
        /// the event to raise when a database is found
        /// </summary>
        public event EventHandler<DatabaseInfoEventArgs> DatabaseFound;

        /// <summary>
        /// Updates the cache for current selected subscriptions
        /// </summary>
        /// <returns>The new cached data</returns>
        public async Task<ServiceResponse<DatabaseInstanceInfo>> RefreshCacheAsync(CancellationToken cancellationToken)
        {
            ServiceResponse<DatabaseInstanceInfo> result = new ServiceResponse<DatabaseInstanceInfo>();

            if (await ClearCacheAsync())
            {
                result = await GetDatabaseInstancesAsync(serverName: null, cancellationToken: cancellationToken);
            }

            return result;
        }

        /// <summary>
        /// Clears the cache for current selected subscriptions
        /// </summary>
        /// <returns>True if cache refreshed successfully. Otherwise returns false</returns>
        public async Task<bool> ClearCacheAsync()
        {
            bool result = false;

            if (AzureResourceManager != null && AccountManager != null && AzureAccountManager != null)
            {
                try
                {
                    IEnumerable<IAzureUserAccountSubscriptionContext> subscriptions = await GetSubscriptionsAsync();
                    _cache.ClearCache(subscriptions.Select(x => x.Subscription.SubscriptionId));
                    result = true;
                }
                catch (Exception ex)
                {
                    TraceException(TraceEventType.Error, TraceId.AzureResource, ex, "Failed to refresh the cache");
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the databases for given connection info. 
        /// The connection info should be used to make the connection for getting databases not the account manager
        /// </summary> 
        //public async Task<ServiceResponse<DatabaseInstanceInfo>> GetDatabaseInstancesAsync(UIConnectionInfo uiConnectionInfo, CancellationToken cancellationToken)
        //{
        //    ServiceResponse<DatabaseInstanceInfo> result = null;
        //    if (DefaultDatabaseDiscoveryProvider != null && DefaultDatabaseDiscoveryProvider != this)
        //    {
        //        result = await DefaultDatabaseDiscoveryProvider.GetDatabaseInstancesAsync(uiConnectionInfo, cancellationToken);
        //    }
        //    else
        //    {
        //        result = new ServiceResponse<DatabaseInstanceInfo>(); //TODO: add error that we couldn't find any default database provider
        //    }
        //    return result;
        //}

        /// <summary>
        /// Returns the databases for given server name. Using the account manager to get the databases
        /// </summary> 
        public async Task<ServiceResponse<DatabaseInstanceInfo>> GetDatabaseInstancesAsync(string serverName, CancellationToken cancellationToken)
        {
            ServiceResponse<DatabaseInstanceInfo> result = null;
            if (AzureResourceManager != null && AccountManager != null && AzureAccountManager != null)
            {
                try
                {
                    //if connection is passed, we need to search all subscriptions not selected ones
                    IEnumerable<IAzureUserAccountSubscriptionContext> subscriptions = await GetSubscriptionsAsync(string.IsNullOrEmpty(serverName));
                    if (!cancellationToken.IsCancellationRequested)
                    {                       
                        result = await AzureUtil.ExecuteGetAzureResourceAsParallel(null, subscriptions, serverName, cancellationToken,
                            GetDatabaseForSubscriptionAsync);
                    }

                }
                catch (Exception ex)
                {
                    result = new ServiceResponse<DatabaseInstanceInfo>(ex);
                }
            }

            result = result ?? new ServiceResponse<DatabaseInstanceInfo>();
            return result;
        }

        /// <summary>
        /// Returns the resource manager that has same metadata as this class
        /// </summary>
        internal IAzureResourceManager AzureResourceManager
        {
            get
            {
                return (_azureResourceManagerWrapper = _azureResourceManagerWrapper ?? GetService<IAzureResourceManager>());
            }
            set
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
                return (_azureAccountManager = _azureAccountManager ?? GetService<IAzureAuthenticationManager>());
            }          
        }

        /// <summary>
        /// Returns the account manager that has same metadata as this class
        /// </summary>
        public IDatabaseDiscoveryProvider DefaultDatabaseDiscoveryProvider
        {
            get
            {
                return (_defaultDatabaseDiscoveryProvider = _defaultDatabaseDiscoveryProvider ?? GetService<IDatabaseDiscoveryProvider>(null));
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

        /// <summary>
        /// Returns azure subscriptions.
        /// </summary>
        private async Task<IEnumerable<IAzureUserAccountSubscriptionContext>> GetSubscriptionsAsync(bool selectedOnly = true)
        {
            try
            {
                return selectedOnly ? await AzureAccountManager.GetSelectedSubscriptionsAsync() : await AzureAccountManager.GetSubscriptionsAsync();
            }
            catch (Exception ex)
            {
                throw new ServiceFailedException(string.Format(CultureInfo.CurrentCulture, SR.AzureSubscriptionFailedErrorMessage, ex));
            }
        }

        /// <summary>
        /// There was a wired nullReferencedException was running the tasks parallel. It only got fixed when I put the getting from cache insed an async method
        /// </summary>       
        private Task<ServiceResponse<DatabaseInstanceInfo>> GetFromCacheAsync(string key)
        {
            return Task.Factory.StartNew(() => _cache.Get(key));
        }   

        /// <summary>
        /// Returns a  list of Azure sql databases for given subscription
        /// </summary>
        private async Task<ServiceResponse<DatabaseInstanceInfo>> GetDatabaseForSubscriptionAsync(IAzureResourceManagementSession parentSession,
            IAzureUserAccountSubscriptionContext input, string serverName,
            CancellationToken cancellationToken, CancellationToken internalCancellationToken)
        {
            ServiceResponse<DatabaseInstanceInfo> result = null;
            bool shouldFilter = !string.IsNullOrEmpty(serverName);
            try
            {
                string key = input.Subscription.SubscriptionId;

                //when the data was coming from cache and no async mthod was called the parallel tasks running crashed so I had to call this line async to fix it
                result = await GetFromCacheAsync(key);
                if (result == null)
                {
                    //this will only get the databases for the given server name
                    result = await GetDatabaseForSubscriptionFromServiceAsync(input, serverName, cancellationToken, internalCancellationToken);
                }
                else if (shouldFilter)
                {
                    //we should filter the result because the cached data includes databases for all servers
                    result = new ServiceResponse<DatabaseInstanceInfo>(result.Data.Where(x => x.ServerInstanceInfo.FullyQualifiedDomainName == serverName), 
                        result.Errors);
                }

                //only update the cache if server name is not passes so the result is not filtered. The cache data supposed to be the data for all server
                if (!shouldFilter && !cancellationToken.IsCancellationRequested)
                {
                    result = _cache.UpdateCache(key, result);
                }                
            }
            catch (Exception ex)
            {
                result = new ServiceResponse<DatabaseInstanceInfo>(ex);
            }

            return result;
        }

        /// <summary>
        /// Returns a  list of Azure sql databases for given subscription
        /// </summary>
        private async Task<ServiceResponse<DatabaseInstanceInfo>> GetDatabaseForSubscriptionFromServiceAsync(
            IAzureUserAccountSubscriptionContext input, string serverName,
            CancellationToken cancellationToken, CancellationToken internalCancellationToken)
        {
            ServiceResponse<DatabaseInstanceInfo> result = null;
            
            try
            {
                if (!cancellationToken.IsCancellationRequested && !internalCancellationToken.IsCancellationRequested)
                {
                    using (IAzureResourceManagementSession session = await AzureResourceManager.CreateSessionAsync(input))
                    {
                        //find the server matches with the given servername which should be only one
                        bool shouldFilter = !string.IsNullOrEmpty(serverName);
                        IEnumerable<IAzureSqlServerResource> sqlAzureServers = await AzureResourceManager.GetSqlServerAzureResourcesAsync(session);
                        IEnumerable<IAzureSqlServerResource> filteredServers = !shouldFilter ? sqlAzureServers : sqlAzureServers.Where(x =>
                                    x.FullyQualifiedDomainName != null &&
                                    x.FullyQualifiedDomainName.Equals(serverName,
                                        StringComparison.OrdinalIgnoreCase));

                        IList<IAzureSqlServerResource> filteredServersList = filteredServers.ToList();
                        result = await GetDatabasesForSubscriptionServersAsync(session, filteredServersList.ToList(), cancellationToken);

                        //Set response Found to true to notify the other tasks to cancel
                        if (shouldFilter && filteredServersList.Any())
                        {
                            result.Found = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result =  new ServiceResponse<DatabaseInstanceInfo>(ex);
            }

            return result ?? new ServiceResponse<DatabaseInstanceInfo>();
        }

        private async Task<ServiceResponse<DatabaseInstanceInfo>> GetDatabasesForSubscriptionServersAsync(IAzureResourceManagementSession session, 
            IList<IAzureSqlServerResource> filteredServersList, CancellationToken cancellationToken)
        {
            ServiceResponse<DatabaseInstanceInfo> result = null;
            AzureServerDatabaseDiscoveryProvider azureServerDatabaseDiscoveryProvider = new AzureServerDatabaseDiscoveryProvider(AzureResourceManager, session, ServerDefinition);
            azureServerDatabaseDiscoveryProvider.DatabaseFound += AzureServerDatabaseDiscoveryProviderOnDatabaseFound;
            if (filteredServersList.Any())
            {
                result = await azureServerDatabaseDiscoveryProvider.GetDatabasesForServers(filteredServersList, cancellationToken);
            }

            return result ?? new ServiceResponse<DatabaseInstanceInfo>();
        }

        private void AzureServerDatabaseDiscoveryProviderOnDatabaseFound(object sender, DatabaseInfoEventArgs databaseInfoEventArgs)
        {
            OnDatabaseFound(databaseInfoEventArgs.Database);
        }

        /// <summary>
        /// Raises DatabaseFound event with the given databases info
        /// </summary>
        private void OnDatabaseFound(DatabaseInstanceInfo databaseInfo)
        {
            if (DatabaseFound != null)
            {
                DatabaseFound(this, new DatabaseInfoEventArgs() { Database = databaseInfo });
            }
        }
    }
}
