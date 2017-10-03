//------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Microsoft.SqlTools.ResourceProvider.Core.FirewallRule;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider
{
    /// <summary>
    /// Tests to verify FirewallRuleService by mocking the azure authentication and resource managers
    /// </summary>
    public class FirewallRuleServiceTest
    {
        [Fact]
        public async Task CreateShouldThrowExceptionGivenNullServerName()
        {
            string serverName = null;

            ServiceTestContext testContext = new ServiceTestContext();

            await Assert.ThrowsAsync<FirewallRuleException>(() => VerifyCreateAsync(testContext, serverName));
        }

        [Fact]
        public async Task CreateShouldThrowExceptionGivenNullStartIp()
        {
            string serverName = "serverName";

            ServiceTestContext testContext = new ServiceTestContext();
            testContext.StartIpAddress = null;
            await Assert.ThrowsAsync<FirewallRuleException>(() => VerifyCreateAsync(testContext, serverName));
        }

        [Fact]
        public async Task CreateShouldThrowExceptionGivenInvalidEndIp()
        {
            string serverName = "serverName";

            ServiceTestContext testContext = new ServiceTestContext();
            testContext.EndIpAddress = "invalid ip";
            await Assert.ThrowsAsync<FirewallRuleException>(() => VerifyCreateAsync(testContext, serverName));
        }

        [Fact]
        public async Task CreateShouldThrowExceptionGivenInvalidStartIp()
        {
            string serverName = "serverName";

            ServiceTestContext testContext = new ServiceTestContext();
            testContext.StartIpAddress = "invalid ip";
            await Assert.ThrowsAsync<FirewallRuleException>(() => VerifyCreateAsync(testContext, serverName));
        }

        [Fact]
        public async Task CreateShouldThrowExceptionGivenNullEndIp()
        {
            ServiceTestContext testContext = new ServiceTestContext();
            testContext.EndIpAddress = null;
            await Assert.ThrowsAsync<FirewallRuleException>(() => VerifyCreateAsync(testContext, testContext.ServerName));
        }

        [Fact]
        public async Task CreateShouldThrowExceptionIfUserIsNotLoggedIn()
        {
            var applicationAuthenticationManagerMock = new Mock<IAzureAuthenticationManager>();
            applicationAuthenticationManagerMock.Setup(x => x.GetUserNeedsReauthenticationAsync()).Throws(new ApplicationException());
            var azureResourceManagerMock = new Mock<IAzureResourceManager>();
            
            ServiceTestContext testContext = new ServiceTestContext();
            testContext.ApplicationAuthenticationManagerMock = applicationAuthenticationManagerMock;
            testContext.AzureResourceManagerMock = azureResourceManagerMock;

            await Assert.ThrowsAsync<FirewallRuleException>(() => VerifyCreateAsync(testContext, testContext.ServerName));
            azureResourceManagerMock.Verify(x => x.CreateFirewallRuleAsync(
                 It.IsAny<IAzureResourceManagementSession>(), It.IsAny<IAzureSqlServerResource>(), It.IsAny<FirewallRuleRequest>()),
                 Times.AtLeastOnce);
        }

        [Fact]
        public async Task CreateShouldThrowExceptionIfUserDoesNotHaveSubscriptions()
        {
            var applicationAuthenticationManagerMock =
                new Mock<IAzureAuthenticationManager>();
            applicationAuthenticationManagerMock.Setup(x => x.GetUserNeedsReauthenticationAsync()).Returns(Task.FromResult(false));
            applicationAuthenticationManagerMock.Setup(x => x.GetSubscriptionsAsync())
                .Returns(Task.FromResult(Enumerable.Empty<IAzureUserAccountSubscriptionContext>()));
            var azureResourceManagerMock = new Mock<IAzureResourceManager>();

            ServiceTestContext testContext = new ServiceTestContext();
            testContext.ApplicationAuthenticationManagerMock = applicationAuthenticationManagerMock;
            testContext.AzureResourceManagerMock = azureResourceManagerMock;

            await Assert.ThrowsAsync<FirewallRuleException>(() => VerifyCreateAsync(testContext, testContext.ServerName));
            azureResourceManagerMock.Verify(x => x.CreateFirewallRuleAsync(
                It.IsAny<IAzureResourceManagementSession>(), It.IsAny<IAzureSqlServerResource>(), It.IsAny<FirewallRuleRequest>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateShouldThrowExceptionIfAuthenticationManagerFailsToReturnSubscription()
        {
            var applicationAuthenticationManagerMock = new Mock<IAzureAuthenticationManager>();
            applicationAuthenticationManagerMock.Setup(x => x.GetUserNeedsReauthenticationAsync()).Returns(Task.FromResult(false));
            applicationAuthenticationManagerMock.Setup(x => x.GetSubscriptionsAsync()).Throws(new Exception());
            var azureResourceManagerMock = new Mock<IAzureResourceManager>();

            ServiceTestContext testContext = new ServiceTestContext();
            testContext.ApplicationAuthenticationManagerMock = applicationAuthenticationManagerMock;
            testContext.AzureResourceManagerMock = azureResourceManagerMock;
            await Assert.ThrowsAsync<FirewallRuleException>(() => VerifyCreateAsync(testContext, "invalid server"));
            
            azureResourceManagerMock.Verify(x => x.CreateFirewallRuleAsync(
                It.IsAny<IAzureResourceManagementSession>(), It.IsAny<IAzureSqlServerResource>(), It.IsAny<FirewallRuleRequest>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateShouldThrowExceptionGivenNoSubscriptionFound()
        {
            ServiceTestContext testContext = new ServiceTestContext();
            testContext = CreateMocks(testContext);
            await Assert.ThrowsAsync<FirewallRuleException>(() => VerifyCreateAsync(testContext, "invalid server"));
           
        }

        [Fact]
        public async Task CreateShouldCreateFirewallSuccessfullyGivenValidUserAccount()
        {
            ServiceTestContext testContext = new ServiceTestContext();
            testContext = CreateMocks(testContext);

            await VerifyCreateAsync(testContext, testContext.ServerName);           
        }

        [Fact]
        public async Task CreateShouldFindTheRightSubscriptionGivenValidSubscriptionInFirstPlace()
        {
            ServiceTestContext testContext = new ServiceTestContext();
            testContext.Subscriptions = new List<IAzureUserAccountSubscriptionContext>
            {
                testContext.ValidSubscription,
                ServiceTestContext.CreateSubscriptionContext(),
                ServiceTestContext.CreateSubscriptionContext(),
            };

            testContext = CreateMocks(testContext);

            await VerifyCreateAsync(testContext, testContext.ServerName);            
        }

        [Fact]
        public async Task CreateShouldFindTheRightSubscriptionGivenValidSubscriptionInSecondPlace()
        {
            ServiceTestContext testContext = new ServiceTestContext();
            testContext.Subscriptions = new List<IAzureUserAccountSubscriptionContext>
            {
                ServiceTestContext.CreateSubscriptionContext(),
                testContext.ValidSubscription,
                ServiceTestContext.CreateSubscriptionContext(),
            };
            testContext.Initialize();
            testContext = CreateMocks(testContext);
            await VerifyCreateAsync(testContext, testContext.ServerName);            
        }

        [Fact]
        public async Task CreateShouldFindTheRightSubscriptionGivenValidSubscriptionInLastPlace()
        {
            ServiceTestContext testContext = new ServiceTestContext();
            testContext.Subscriptions = new List<IAzureUserAccountSubscriptionContext>
            {
                ServiceTestContext.CreateSubscriptionContext(),
                ServiceTestContext.CreateSubscriptionContext(),
                testContext.ValidSubscription
            };
            testContext.Initialize();

            testContext = CreateMocks(testContext);

            await VerifyCreateAsync(testContext, testContext.ServerName);           
        }

        [Fact]
        public async Task CreateShouldFindTheRightResourceGivenValidResourceInLastPlace()
        {
            ServiceTestContext testContext = new ServiceTestContext();
            var resources = new List<IAzureSqlServerResource>
            {
                ServiceTestContext.CreateAzureSqlServer(Guid.NewGuid().ToString()),
                ServiceTestContext.CreateAzureSqlServer(testContext.ServerName),
            };
            testContext.SubscriptionToResourcesMap[testContext.ValidSubscription.Subscription.SubscriptionId] = resources; 

            testContext = CreateMocks(testContext);

            await VerifyCreateAsync(testContext, testContext.ServerName);           
        }

        [Fact]
        public async Task CreateShouldFindTheRightResourceGivenValidResourceInFirstPlace()
        {
            ServiceTestContext testContext = new ServiceTestContext();
            var resources = new List<IAzureSqlServerResource>
            {
                ServiceTestContext.CreateAzureSqlServer(testContext.ServerName),
                ServiceTestContext.CreateAzureSqlServer(Guid.NewGuid().ToString()),
            };
            testContext.SubscriptionToResourcesMap[testContext.ValidSubscription.Subscription.SubscriptionId] = resources;

            testContext = CreateMocks(testContext);

            await VerifyCreateAsync(testContext, testContext.ServerName);           
        }

        [Fact]
        public async Task CreateShouldFindTheRightResourceGivenValidResourceInMiddle()
        {
            ServiceTestContext testContext = new ServiceTestContext();
            var resources = new List<IAzureSqlServerResource>
            {
                ServiceTestContext.CreateAzureSqlServer(Guid.NewGuid().ToString()),
                ServiceTestContext.CreateAzureSqlServer(testContext.ServerName),
                ServiceTestContext.CreateAzureSqlServer(Guid.NewGuid().ToString())
            };
            testContext.SubscriptionToResourcesMap[testContext.ValidSubscription.Subscription.SubscriptionId] = resources;

            testContext = CreateMocks(testContext);

            await VerifyCreateAsync(testContext, testContext.ServerName);
        }

        [Fact]
        public async Task CreateThrowExceptionIfResourceNotFound()
        {
            ServiceTestContext testContext = new ServiceTestContext();
            var resources = new List<IAzureSqlServerResource>
            {
                ServiceTestContext.CreateAzureSqlServer(Guid.NewGuid().ToString()),
                ServiceTestContext.CreateAzureSqlServer(Guid.NewGuid().ToString()),
            };
            testContext.SubscriptionToResourcesMap[testContext.ValidSubscription.Subscription.SubscriptionId] = resources;

            testContext = CreateMocks(testContext);

            await Assert.ThrowsAsync<FirewallRuleException>(() => VerifyCreateAsync(testContext, testContext.ServerName));
        }

        [Fact]
        public async Task CreateThrowExceptionIfResourcesIsEmpty()
        {
            ServiceTestContext testContext = new ServiceTestContext();
          
            testContext.SubscriptionToResourcesMap[testContext.ValidSubscription.Subscription.SubscriptionId] = new List<IAzureSqlServerResource>();
            testContext = CreateMocks(testContext);

            await Assert.ThrowsAsync<FirewallRuleException>(() => VerifyCreateAsync(testContext, testContext.ServerName, false));
        }

        [Fact]
        public async Task CreateShouldThrowExceptionIfThereIsNoSubscriptionForUser()
        {
            ServiceTestContext testContext = new ServiceTestContext();
            testContext.Subscriptions = new List<IAzureUserAccountSubscriptionContext>();

            testContext = CreateMocks(testContext);

            await Assert.ThrowsAsync<FirewallRuleException>(() => VerifyCreateAsync(testContext, testContext.ServerName, false));
        }


        [Fact]
        public async Task CreateShouldThrowExceptionIfSubscriptionIsInAnotherAccount()
        {
            ServiceTestContext testContext = new ServiceTestContext();
            testContext.Subscriptions = new List<IAzureUserAccountSubscriptionContext>
            {
                ServiceTestContext.CreateSubscriptionContext(),
                ServiceTestContext.CreateSubscriptionContext(),
            };

            testContext = CreateMocks(testContext);
            await Assert.ThrowsAsync<FirewallRuleException>(() => VerifyCreateAsync(testContext, testContext.ServerName, false));
        }       

        [Fact]
        public async Task CreateShouldCreateFirewallForTheRightServerFullyQualifiedName()
        {
            ServiceTestContext testContext = new ServiceTestContext();
            string serverNameWithDifferentDomain = testContext.ServerNameWithoutDomain + ".myaliased.domain.name";

            testContext.ServerName = serverNameWithDifferentDomain;
            testContext.Initialize();
            testContext = CreateMocks(testContext);

            await VerifyCreateAsync(testContext, testContext.ServerName);
        }        

        private async Task<FirewallRuleResponse> VerifyCreateAsync(ServiceTestContext testContext, string serverName, bool verifyFirewallRuleCreated = true)
        {
            try
            {
                FirewallRuleService service = new FirewallRuleService();
                service.AuthenticationManager = testContext.ApplicationAuthenticationManager;
                service.ResourceManager = testContext.AzureResourceManager;
                FirewallRuleResponse response = await service.CreateFirewallRuleAsync(serverName, testContext.StartIpAddress, testContext.EndIpAddress);
                if (verifyFirewallRuleCreated)
                {
                    testContext.AzureResourceManagerMock.Verify(x => x.CreateFirewallRuleAsync(
                            It.Is<IAzureResourceManagementSession>(s => s.SubscriptionContext.Subscription.SubscriptionId == testContext.ValidSubscription.Subscription.SubscriptionId),
                            It.Is<IAzureSqlServerResource>(r => r.FullyQualifiedDomainName == serverName),
                            It.Is<FirewallRuleRequest>(y => y.EndIpAddress.ToString().Equals(testContext.EndIpAddress) && y.StartIpAddress.ToString().Equals(testContext.StartIpAddress))),
                            Times.AtLeastOnce);
                }
                else
                {
                    testContext.AzureResourceManagerMock.Verify(x => x.CreateFirewallRuleAsync(
                           It.Is<IAzureResourceManagementSession>(s => s.SubscriptionContext.Subscription.SubscriptionId == testContext.ValidSubscription.Subscription.SubscriptionId),
                           It.Is<IAzureSqlServerResource>(r => r.FullyQualifiedDomainName == serverName),
                           It.Is<FirewallRuleRequest>(y => y.EndIpAddress.ToString().Equals(testContext.EndIpAddress) && y.StartIpAddress.ToString().Equals(testContext.StartIpAddress))),
                        Times.Never);
                }

                return response;
            }
            catch (Exception ex)
            {
                if (ex is FirewallRuleException)
                {
                    Assert.True(ex.InnerException == null || !(ex.InnerException is FirewallRuleException));
                }
                throw;
            }
        }

        private ServiceTestContext CreateMocks(ServiceTestContext testContext)
        {
            var accountMock = new Mock<IUserAccount>();
            accountMock.Setup(x => x.UniqueId).Returns(Guid.NewGuid().ToString());
            var applicationAuthenticationManagerMock = new Mock<IAzureAuthenticationManager>();
            applicationAuthenticationManagerMock.Setup(x => x.GetUserNeedsReauthenticationAsync())
                .Returns(Task.FromResult(false));
            applicationAuthenticationManagerMock.Setup(x => x.GetCurrentAccountAsync()).Returns(Task.FromResult(accountMock.Object));
            applicationAuthenticationManagerMock.Setup(x => x.GetSubscriptionsAsync()).Returns(Task.FromResult(testContext.Subscriptions as IEnumerable<IAzureUserAccountSubscriptionContext>));

            var azureResourceManagerMock = new Mock<IAzureResourceManager>();

            CreateMocksForResources(testContext, azureResourceManagerMock);

            testContext.ApplicationAuthenticationManagerMock = applicationAuthenticationManagerMock;
            testContext.AzureResourceManagerMock = azureResourceManagerMock;
            return testContext;
        }

        private void CreateMocksForResources(
            ServiceTestContext testContext,
            Mock<IAzureResourceManager> azureResourceManagerMock)
        {
            foreach (IAzureUserAccountSubscriptionContext subscription in testContext.Subscriptions)
            {
                var sessionMock = new Mock<IAzureResourceManagementSession>();
                sessionMock.Setup(x => x.SubscriptionContext).Returns(subscription);
                azureResourceManagerMock.Setup(x => x.CreateSessionAsync(subscription)).Returns(Task.FromResult(sessionMock.Object));

                List<IAzureSqlServerResource> resources;
                if (testContext.SubscriptionToResourcesMap.TryGetValue(subscription.Subscription.SubscriptionId,
                    out resources))
                {
                    azureResourceManagerMock.Setup(x => x.GetSqlServerAzureResourcesAsync(It.Is<IAzureResourceManagementSession>(
                                                        m => m.SubscriptionContext.Subscription.SubscriptionId == subscription.Subscription.SubscriptionId)))
                                            .Returns(Task.FromResult(resources as IEnumerable<IAzureSqlServerResource>));
                }
                else
                {
                    azureResourceManagerMock.Setup(x => x.GetSqlServerAzureResourcesAsync(
                            It.Is<IAzureResourceManagementSession>(m => m.SubscriptionContext.Subscription.SubscriptionId == subscription.Subscription.SubscriptionId)))
                        .Returns(Task.FromResult<IEnumerable<IAzureSqlServerResource>>(null));
                }
            }

            azureResourceManagerMock
            .Setup(x => x.CreateFirewallRuleAsync(
                It.IsAny<IAzureResourceManagementSession>(),
                It.IsAny<IAzureSqlServerResource>(),
                It.Is<FirewallRuleRequest>(
                    y => y.EndIpAddress.ToString().Equals(testContext.EndIpAddress)
                    && y.StartIpAddress.ToString().Equals(testContext.StartIpAddress))))
            .Returns(Task.FromResult(new FirewallRuleResponse() {Created = true}));
        }
    }

    internal class ServiceTestContext
    {
        private string _validServerName = "validServerName.database.windows.net";
        private string _startIpAddressValue = "1.2.3.6";
        private string _endIpAddressValue = "1.2.3.6";
        private Dictionary<string, List<IAzureSqlServerResource>> _subscriptionToResourcesMap;

        public ServiceTestContext()
        {
            StartIpAddress = _startIpAddressValue;
            EndIpAddress = _endIpAddressValue;
            ServerName = _validServerName;
            Initialize();
        }

        internal void Initialize()
        {
            CreateSubscriptions();
            CreateAzureResources();
        }

        internal static IAzureUserAccountSubscriptionContext CreateSubscriptionContext()
        {
            var subscriptionContext = new Mock<IAzureUserAccountSubscriptionContext>();
            var subscriptionMock = new Mock<IAzureSubscriptionIdentifier>();
            subscriptionMock.Setup(x => x.SubscriptionId).Returns(Guid.NewGuid().ToString());
            subscriptionContext.Setup(x => x.Subscription).Returns(subscriptionMock.Object);
            return subscriptionContext.Object;
        }

        private void CreateSubscriptions()
        {
            if (Subscriptions == null || Subscriptions.Count == 0)
            {

                ValidSubscriptionMock = new Mock<IAzureUserAccountSubscriptionContext>();
                var subscriptionMock = new Mock<IAzureSubscriptionIdentifier>();
                subscriptionMock.Setup(x => x.SubscriptionId).Returns(Guid.NewGuid().ToString());
                ValidSubscriptionMock.Setup(x => x.Subscription).Returns(subscriptionMock.Object);

                Subscriptions = new List<IAzureUserAccountSubscriptionContext>
                {
                    ValidSubscription,
                    CreateSubscriptionContext(),
                    CreateSubscriptionContext()
                };
            }
        }

        internal void CreateAzureResources(Dictionary<string, List<IAzureSqlServerResource>> subscriptionToResourcesMap = null)
        {
            _subscriptionToResourcesMap = new Dictionary<string, List<IAzureSqlServerResource>>();

            if (subscriptionToResourcesMap == null)
            {
                foreach (var subscriptionDetails in Subscriptions)
                {
                    if (subscriptionDetails.Subscription.SubscriptionId == ValidSubscription.Subscription.SubscriptionId)
                    {
                        var resources = new List<IAzureSqlServerResource>();
                        resources.Add(CreateAzureSqlServer(Guid.NewGuid().ToString()));
                        resources.Add(CreateAzureSqlServer(Guid.NewGuid().ToString()));
                        resources.Add(CreateAzureSqlServer(ServerName));
                        _subscriptionToResourcesMap.Add(ValidSubscription.Subscription.SubscriptionId, resources);
                    }
                    else
                    {
                        var resources = new List<IAzureSqlServerResource>();
                        resources.Add(CreateAzureSqlServer(Guid.NewGuid().ToString()));
                        resources.Add(CreateAzureSqlServer(Guid.NewGuid().ToString()));
                        _subscriptionToResourcesMap.Add(subscriptionDetails.Subscription.SubscriptionId, resources);
                    }
                }
            }
            else
            {
                _subscriptionToResourcesMap = subscriptionToResourcesMap;
            }
        }

        internal static IAzureSqlServerResource CreateAzureSqlServer(string serverName)
        {
            var azureSqlServer =
                new Mock<IAzureSqlServerResource>();
            azureSqlServer.Setup(x => x.Name).Returns(GetServerNameWithoutDomain(serverName));
            azureSqlServer.Setup(x => x.FullyQualifiedDomainName).Returns(serverName);
            return azureSqlServer.Object;
        }       

        internal Dictionary<string, List<IAzureSqlServerResource>> SubscriptionToResourcesMap
        {            
            get { return _subscriptionToResourcesMap; }
        }

        internal static string GetServerNameWithoutDomain(string serverName)
        {
            int index = serverName.IndexOf('.');
            if (index > 0)
            {
                return serverName.Substring(0, index);
            }
            return serverName;
        }

        internal string StartIpAddress { get; set; }

        internal string EndIpAddress { get; set; }

        internal IList<IAzureUserAccountSubscriptionContext> Subscriptions { get; set; }

        internal Mock<IAzureUserAccountSubscriptionContext> ValidSubscriptionMock { get; set; }
        internal IAzureUserAccountSubscriptionContext ValidSubscription { get { return ValidSubscriptionMock.Object; } }

        internal string ServerName { get; set; }

        internal string ServerNameWithoutDomain
        {
            get { return GetServerNameWithoutDomain(ServerName); }
        }

        internal Mock<IAzureAuthenticationManager> ApplicationAuthenticationManagerMock { get; set; }
        internal IAzureAuthenticationManager ApplicationAuthenticationManager { get { return ApplicationAuthenticationManagerMock.Object; } }

        internal Mock<IAzureResourceManager> AzureResourceManagerMock { get; set; }

        internal IAzureResourceManager AzureResourceManager { get { return AzureResourceManagerMock.Object; } }
    }

}
