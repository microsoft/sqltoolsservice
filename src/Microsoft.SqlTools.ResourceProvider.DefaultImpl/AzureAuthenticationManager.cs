//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Microsoft.SqlTools.ResourceProvider.Core.Contracts;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ResourceProvider.DefaultImpl
{

    /// <summary>
    /// Implementation for <see cref="IAzureAuthenticationManager" />. 
    /// Provides functionality to authenticate to Azure and discover associated accounts and subscriptions
    /// </summary>
    [Exportable(
        ServerTypes.SqlServer,
        Categories.Azure,
        typeof(IAzureAuthenticationManager),
        "Microsoft.SqlTools.ResourceProvider.DefaultImpl.AzureAuthenticationManager",
        1)
    ]
    class AzureAuthenticationManager : ExportableBase, IAzureAuthenticationManager
    {
        private Dictionary<string, AzureUserAccount> accountsMap;
        private string currentAccountId = null;
        private IEnumerable<IAzureUserAccountSubscriptionContext> _selectedSubscriptions = null;
        private readonly object _selectedSubscriptionsLockObject = new object();
        private readonly ConcurrentCache<IEnumerable<IAzureUserAccountSubscriptionContext>> _subscriptionCache =
            new ConcurrentCache<IEnumerable<IAzureUserAccountSubscriptionContext>>();


        public AzureAuthenticationManager()
        {
            Metadata = new ExportableMetadata(
                ServerTypes.SqlServer,
                Categories.Azure,
                "Microsoft.SqlTools.ResourceProvider.DefaultImpl.AzureAuthenticationManager",
                1);
            accountsMap = new Dictionary<string, AzureUserAccount>();

        }


        public IEnumerable<IAzureUserAccount> UserAccounts
        {
            get { return accountsMap.Values; }
        }

        public bool HasLoginDialog
        {
            get { return false; }
        }

        /// <summary>
        /// Set current logged in user
        /// </summary>
        public async Task<IUserAccount> SetCurrentAccountAsync(object account)
        {
            CommonUtil.CheckForNull(account, nameof(account));
            AccountTokenWrapper accountTokenWrapper = account as AccountTokenWrapper;
            if (accountTokenWrapper != null)
            {
                AzureUserAccount userAccount = CreateUserAccount(accountTokenWrapper);
                accountsMap[userAccount.UniqueId] = userAccount;
                currentAccountId = userAccount.UniqueId;
            }
            else
            {
                throw new ServiceFailedException(string.Format(CultureInfo.CurrentCulture, SR.UnsupportedAuthType, account.GetType().Name));
            }
            OnCurrentAccountChanged();
            return await GetCurrentAccountAsync();
        }
        
        /// <summary>
        /// Public for testing purposes. Creates an Azure account with the correct set of mappings for tenants etc.
        /// </summary>
        /// <param name="accountTokenWrapper"></param>
        /// <returns></returns>
        public AzureUserAccount CreateUserAccount(AccountTokenWrapper accountTokenWrapper)
        {
            Account account = accountTokenWrapper.Account;
            CommonUtil.CheckForNull(accountTokenWrapper.Account, nameof(account));
            CommonUtil.CheckForNull(accountTokenWrapper.SecurityTokenMappings, nameof(account) + ".SecurityTokenMappings");
            AzureUserAccount userAccount = new AzureUserAccount();
            userAccount.UniqueId = account.Key.AccountId;
            userAccount.DisplayInfo = ToDisplayInfo(account);
            IList<IAzureTenant> tenants = new List<IAzureTenant>();
            foreach (Tenant tenant in account.Properties.Tenants)
            {
                string token;
                if (accountTokenWrapper.SecurityTokenMappings.TryGetValue(tenant.Id, out token))
                {
                    AzureTenant azureTenant = new AzureTenant()
                    {
                        TenantId = tenant.Id,
                        AccountDisplayableId = tenant.DisplayName,
                        AccessToken = token
                    };
                    tenants.Add(azureTenant);
                }
                // else ignore for now as we can't handle a request to get a tenant without an access key
            }
            userAccount.AllTenants = tenants;
            return userAccount;
        }

        private AzureUserAccountDisplayInfo ToDisplayInfo(Account account)
        {
            return new AzureUserAccountDisplayInfo()
            {
                AccountDisplayName = account.DisplayInfo.DisplayName,
                ProviderDisplayName = account.Key.ProviderId
            };
        }

        private void OnCurrentAccountChanged()
        {
            lock (_selectedSubscriptionsLockObject)
            {
                _selectedSubscriptions = null;
            }
            if (CurrentAccountChanged != null)
            {
                CurrentAccountChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// The event to be raised when the current account is changed
        /// </summary>
        public event EventHandler CurrentAccountChanged;

        public Task<IUserAccount> AddUserAccountAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IUserAccount> AuthenticateAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<IUserAccount> GetCurrentAccountAsync()
        {
            var account = await GetCurrentAccountInternalAsync();
            return account;
        }

        private Task<AzureUserAccount> GetCurrentAccountInternalAsync()
        {

            AzureUserAccount account = null;
            if (currentAccountId != null
                && accountsMap.TryGetValue(currentAccountId, out account))
            {
                // TODO is there more needed here?
            }
            return Task.FromResult<AzureUserAccount>(account);
        }

        public async Task<IEnumerable<IAzureUserAccountSubscriptionContext>> GetSelectedSubscriptionsAsync()
        {
            return _selectedSubscriptions ?? await GetSubscriptionsAsync();
        }
        
        /// <summary>
        /// Returns user's subscriptions
        /// </summary>
        public async Task<IEnumerable<IAzureUserAccountSubscriptionContext>> GetSubscriptionsAsync()
        {
            var result = Enumerable.Empty<IAzureUserAccountSubscriptionContext>();
            bool userNeedsAuthentication = await GetUserNeedsReauthenticationAsync();
            if (!userNeedsAuthentication)
            {
                AzureUserAccount currentUser = await GetCurrentAccountInternalAsync();
                if (currentUser != null)
                {
                    try
                    {
                        result = await GetSubscriptionsFromCacheAsync(currentUser);
                    }
                    catch (ServiceExceptionBase)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new ServiceFailedException(SR.AzureSubscriptionFailedErrorMessage, ex);
                    }
                }
                result = result ?? Enumerable.Empty<IAzureUserAccountSubscriptionContext>();
            }
            return result;
        }

        private async Task<IEnumerable<IAzureUserAccountSubscriptionContext>> GetSubscriptionsFromCacheAsync(AzureUserAccount user)
        {
            var result = Enumerable.Empty<IAzureUserAccountSubscriptionContext>();
            
            if (user != null)
            {
                result = _subscriptionCache.Get(user.UniqueId);
                if (result == null)
                {
                    result = await GetSubscriptionFromServiceAsync(user);
                    _subscriptionCache.UpdateCache(user.UniqueId, result);
                }
            }
            result = result ?? Enumerable.Empty<IAzureUserAccountSubscriptionContext>();
            return result;
        }

        private async Task<IEnumerable<IAzureUserAccountSubscriptionContext>> GetSubscriptionFromServiceAsync(AzureUserAccount userAccount)
        {
            List<IAzureUserAccountSubscriptionContext> subscriptionList = new List<IAzureUserAccountSubscriptionContext>();

            try
            {
                if (userAccount != null && !userAccount.NeedsReauthentication)
                {
                    IAzureResourceManager resourceManager = ServiceProvider.GetService<IAzureResourceManager>();
                    IEnumerable<IAzureUserAccountSubscriptionContext> contexts = await resourceManager.GetSubscriptionContextsAsync(userAccount);
                    subscriptionList = contexts.ToList();
                }
                else
                {
                    throw new UserNeedsAuthenticationException(SR.AzureSubscriptionFailedErrorMessage);
                }
            }
            // TODO handle stale tokens
            //catch (MissingSecurityTokenException missingSecurityTokenException)
            //{
            //    //User needs to reauthenticate
            //    if (userAccount != null)
            //    {
            //        userAccount.NeedsReauthentication = true;
            //    }
            //    throw new UserNeedsAuthenticationException(SR.AzureSubscriptionFailedErrorMessage, missingSecurityTokenException);
            //}
            catch (ServiceExceptionBase)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ServiceFailedException(SR.AzureSubscriptionFailedErrorMessage, ex);
            }
            return subscriptionList;
        }


        public Task<bool> GetUserNeedsReauthenticationAsync()
        {
            // for now, we don't support handling stale auth objects
            return Task.FromResult(false);
        }

        /// <summary>
        /// Stores the selected subscriptions given the ids
        /// </summary>
        public async Task<bool> SetSelectedSubscriptionsAsync(IEnumerable<string> subscriptionIds)
        {
            IEnumerable<IAzureUserAccountSubscriptionContext> subscriptions = await GetSubscriptionsAsync();
            List<IAzureUserAccountSubscriptionContext> subscriptionList = subscriptions.ToList();

            List<IAzureUserAccountSubscriptionContext> newSelectedSubscriptions = subscriptionIds == null
                ? subscriptionList
                : subscriptionList.Where(x => subscriptionIds.Contains(x.Subscription.SubscriptionId)).ToList();

            //If the current account changes during setting selected subscription, none of the ids should be found 
            //so we just reset the selected subscriptions
            if (subscriptionIds != null && subscriptionIds.Any() && newSelectedSubscriptions.Count == 0)
            {
                newSelectedSubscriptions = subscriptionList;
            }
            lock (_selectedSubscriptionsLockObject)
            {
                if (!SelectedSubscriptionsEquals(newSelectedSubscriptions))
                {
                    _selectedSubscriptions = newSelectedSubscriptions;
                    return true;
                }
            }
            return false;
        }

        private bool SelectedSubscriptionsEquals(List<IAzureUserAccountSubscriptionContext> newSelectedSubscriptions)
        {
            if (_selectedSubscriptions != null && _selectedSubscriptions.Count() == newSelectedSubscriptions.Count)
            {
                return newSelectedSubscriptions.All(subscription => _selectedSubscriptions.Contains(subscription));
            }
            return false;
        }

        /// <summary>
        /// Tries to find a subscription given subscription id
        /// </summary>
        public bool TryParseSubscriptionIdentifier(string value, out IAzureSubscriptionIdentifier subscription)
        {
            // TODO can port this over from the VS implementation if needed, but for now disabling as we don't serialize / deserialize subscriptions
            throw new NotImplementedException();
        }
    }
}
