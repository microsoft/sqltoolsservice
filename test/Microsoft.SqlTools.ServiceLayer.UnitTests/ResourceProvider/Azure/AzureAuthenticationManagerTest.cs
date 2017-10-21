//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Microsoft.SqlTools.ResourceProvider.Core.Contracts;
using Microsoft.SqlTools.ResourceProvider.DefaultImpl;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Azure
{
    public class AzureAuthenticationManagerTest
    {
        private Mock<IAzureResourceManager> resourceManagerMock;
        private RegisteredServiceProvider serviceProvider;

        public AzureAuthenticationManagerTest()
        {
            resourceManagerMock = new Mock<IAzureResourceManager>();
            serviceProvider = new RegisteredServiceProvider();
            serviceProvider.RegisterSingleService<IAzureResourceManager>(resourceManagerMock.Object);
        }

        [Fact]
        public async Task CurrentUserShouldBeNullWhenUserIsNotSignedIn()
        {
            IAzureAuthenticationManager accountManager = await CreateAccountManager(null, null);
            Assert.Null(await accountManager.GetCurrentAccountAsync());
        }

        [Fact]
        public async Task GetSubscriptionShouldReturnEmptyWhenUserIsNotSignedIn()
        {
            IAzureAuthenticationManager accountManager = await CreateAccountManager(null, null);
            IEnumerable<IAzureUserAccountSubscriptionContext> result =
                await accountManager.GetSelectedSubscriptionsAsync();
            Assert.False(result.Any());
        }
        
        [Fact]
        public async Task GetSubscriptionShouldThrowWhenUserNeedsAuthentication()
        {
            var currentUserAccount = CreateAccount();
            currentUserAccount.Account.IsStale = true;
            IAzureAuthenticationManager accountManager = await CreateAccountManager(currentUserAccount, null);
            await Assert.ThrowsAsync<ExpiredTokenException>(() => accountManager.GetSelectedSubscriptionsAsync());
        }

        [Fact]
        public async Task GetSubscriptionShouldThrowIfFailed()
        {
            var currentUserAccount = CreateAccount();
            IAzureAuthenticationManager accountManager = await CreateAccountManager(currentUserAccount, null, true);
            await Assert.ThrowsAsync<ServiceFailedException>(() => accountManager.GetSelectedSubscriptionsAsync());
        }

        [Fact]
        public async Task GetSubscriptionShouldReturnTheListSuccessfully()
        {
            List<IAzureUserAccountSubscriptionContext> subscriptions = new List<IAzureUserAccountSubscriptionContext> {
                new Mock<IAzureUserAccountSubscriptionContext>().Object
            };
            var currentUserAccount = CreateAccount();
            IAzureAuthenticationManager accountManager = await CreateAccountManager(currentUserAccount, subscriptions, false);
            IEnumerable<IAzureUserAccountSubscriptionContext> result =
                await accountManager.GetSelectedSubscriptionsAsync();
            Assert.True(result.Any());
        }

        private AccountTokenWrapper CreateAccount(bool needsReauthentication = false)
        {
            return new AccountTokenWrapper(new Account()
                {
                    Key = new AccountKey()
                    {
                        AccountId = "MyAccount",
                        ProviderId = "MSSQL"
                    },
                    IsStale = needsReauthentication
                },
                new Dictionary<string, AccountSecurityToken>());
        }
        private async Task<AzureAuthenticationManager> CreateAccountManager(AccountTokenWrapper currentAccount,
            IEnumerable<IAzureUserAccountSubscriptionContext> subscriptions, bool shouldFail = false)
        {
            AzureAuthenticationManager azureAuthenticationManager = new AzureAuthenticationManager();
            azureAuthenticationManager.SetServiceProvider(serviceProvider);
            if (currentAccount != null)
            {
                await azureAuthenticationManager.SetCurrentAccountAsync(currentAccount);
            }

            if (!shouldFail)
            {
                resourceManagerMock.Setup(x => x.GetSubscriptionContextsAsync(It.IsAny<IAzureUserAccount>())).Returns(Task.FromResult(subscriptions));
            }
            else
            {
                resourceManagerMock.Setup(x => x.GetSubscriptionContextsAsync(It.IsAny<IAzureUserAccount>())).Throws(new Exception());
            }

            return azureAuthenticationManager;
        }
    }
}
