//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ResourceProvider;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Microsoft.SqlTools.ResourceProvider.Core.Contracts;
using Microsoft.SqlTools.ResourceProvider.Core.Firewall;
using Microsoft.SqlTools.ResourceProvider.DefaultImpl;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Formatter
{
    public class ResourceProviderServiceTests
    {
        private const int SqlAzureFirewallBlockedErrorNumber = 40615;
        private const int SqlAzureLoginFailedErrorNumber = 18456;
        private string errorMessageWithIp = "error Message with 1.2.3.4 as IP address";

        public ResourceProviderServiceTests()
        {
            HostMock = new Mock<IProtocolEndpoint>();
            AuthenticationManagerMock = new Mock<IAzureAuthenticationManager>();
            ResourceManagerMock = new Mock<IAzureResourceManager>();
            ServiceProvider = ExtensionServiceProvider.CreateFromAssembliesInDirectory(ResourceProviderHostLoader.GetResourceProviderExtensionDlls());
            ServiceProvider.RegisterSingleService<IAzureAuthenticationManager>(AuthenticationManagerMock.Object);
            ServiceProvider.RegisterSingleService<IAzureResourceManager>(ResourceManagerMock.Object);
            HostLoader.InitializeHostedServices(ServiceProvider, HostMock.Object);
            ResourceProviderService = ServiceProvider.GetService<ResourceProviderService>();
        }

        protected RegisteredServiceProvider ServiceProvider { get; private set; }
        protected Mock<IProtocolEndpoint> HostMock { get; private set; }

        protected Mock<IAzureAuthenticationManager> AuthenticationManagerMock { get; set; }
        protected Mock<IAzureResourceManager> ResourceManagerMock { get; set; }

        protected ResourceProviderService ResourceProviderService { get; private set; }

        [Fact]
        public async Task TestHandleFirewallRuleIgnoresNonMssqlProvider()
        {
            // Given a non-MSSQL provider
            var handleFirewallParams = new HandleFirewallRuleParams()
            {
                ErrorCode = SqlAzureFirewallBlockedErrorNumber,
                ErrorMessage = errorMessageWithIp,
                ConnectionTypeId = "Other"
            };
            // When I ask whether the service can process an error as a firewall rule request
            await TestUtils.RunAndVerify<HandleFirewallRuleResponse>((context) => ResourceProviderService.ProcessHandleFirewallRuleRequest(handleFirewallParams, context), (response) =>
            {
                // Then I expect the response to be false and no IP information to be sent
                Assert.NotNull(response);
                Assert.False(response.Result);
                Assert.Null(response.IpAddress);
                Assert.Equal(Microsoft.SqlTools.ResourceProvider.Core.SR.FirewallRuleUnsupportedConnectionType, response.ErrorMessage);
            });
        }
        
        [Fact]
        public async Task TestHandleFirewallRuleSupportsMssqlProvider()
        {
            // Given a firewall error for the MSSQL provider
            var handleFirewallParams = new HandleFirewallRuleParams()
            {
                ErrorCode = SqlAzureFirewallBlockedErrorNumber,
                ErrorMessage = errorMessageWithIp,
                ConnectionTypeId = "MSSQL"
            };
            // When I ask whether the service can process an error as a firewall rule request
            await TestUtils.RunAndVerify<HandleFirewallRuleResponse>((context) => ResourceProviderService.ProcessHandleFirewallRuleRequest(handleFirewallParams, context), (response) =>
            {
                // Then I expect the response to be true and the IP address to be extracted
                Assert.NotNull(response);
                Assert.True(response.Result);
                Assert.Equal("1.2.3.4", response.IpAddress);
                Assert.Null(response.ErrorMessage);
            });
        }
        
        [Fact]
        public async Task TestHandleFirewallRuleIgnoresNonFirewallErrors()
        {
            // Given a login error for the MSSQL provider
            var handleFirewallParams = new HandleFirewallRuleParams()
            {
                ErrorCode = SqlAzureLoginFailedErrorNumber,
                ErrorMessage = errorMessageWithIp,
                ConnectionTypeId = "MSSQL"
            };
            // When I ask whether the service can process an error as a firewall rule request
            await TestUtils.RunAndVerify<HandleFirewallRuleResponse>((context) => ResourceProviderService.ProcessHandleFirewallRuleRequest(handleFirewallParams, context), (response) =>
            {
                // Then I expect the response to be false and no IP address to be defined
                Assert.NotNull(response);
                Assert.False(response.Result);
                Assert.Equal(string.Empty, response.IpAddress);
                Assert.Null(response.ErrorMessage);
            });
        }

        [Fact]
        public async Task TestHandleFirewallRuleDoesntBreakWithoutIp()
        {
            // Given a firewall error with no IP address in the error message
            var handleFirewallParams = new HandleFirewallRuleParams()
            {
                ErrorCode = SqlAzureFirewallBlockedErrorNumber,
                ErrorMessage = "No IP here",
                ConnectionTypeId = "MSSQL"
            };
            // When I ask whether the service can process an error as a firewall rule request
            await TestUtils.RunAndVerify<HandleFirewallRuleResponse>((context) => ResourceProviderService.ProcessHandleFirewallRuleRequest(handleFirewallParams, context), (response) =>
            {
                // Then I expect the response to be fakse as we require the known IP address to function
                Assert.NotNull(response);
                Assert.False(response.Result);
                Assert.Equal(string.Empty, response.IpAddress);
                Assert.Null(response.ErrorMessage);
            });
        }

        [Fact]
        public async Task TestCreateFirewallRuleBasicRequest()
        {
            // Given a firewall request for a valid subscription
            string serverName = "myserver.database.windows.net";
            var sub1Mock = new Mock<IAzureUserAccountSubscriptionContext>();
            var sub2Mock = new Mock<IAzureUserAccountSubscriptionContext>();
            var server = new SqlAzureResource(new Azure.Management.Sql.Models.Server("Somewhere", 
                "1234", "myserver", "SQLServer", 
                null, null, null, null, null, null, null,
                fullyQualifiedDomainName: serverName));
            var subsToServers = new List<Tuple<IAzureUserAccountSubscriptionContext, IEnumerable<IAzureSqlServerResource>>>()
            {
                Tuple.Create(sub1Mock.Object, Enumerable.Empty<IAzureSqlServerResource>()),
                Tuple.Create(sub2Mock.Object, new IAzureSqlServerResource[] { server }.AsEnumerable())
            };
            var azureRmResponse = new FirewallRuleResponse()
            {
                Created = true,
                StartIpAddress = null,
                EndIpAddress = null
            };
            SetupDependencies(subsToServers, azureRmResponse);

            // When I request the firewall be created
            var createFirewallParams = new CreateFirewallRuleParams()
            {
                ServerName = serverName,
                StartIpAddress = "1.1.1.1",
                EndIpAddress = "1.1.1.255",
                Account = CreateAccount(),
                SecurityTokenMappings = new Dictionary<string, AccountSecurityToken>()
            };
            await TestUtils.RunAndVerify<CreateFirewallRuleResponse>(
                (context) => ResourceProviderService.HandleCreateFirewallRuleRequest(createFirewallParams, context),
                (response) =>
            {
                // Then I expect the response to be fakse as we require the known IP address to function
                Assert.NotNull(response);
                Assert.Null(response.ErrorMessage);
                Assert.True(response.Result);
            });
        }

        private void SetupDependencies(
            IList<Tuple<IAzureUserAccountSubscriptionContext, IEnumerable<IAzureSqlServerResource>>> subsToServers,
            FirewallRuleResponse response)
        {
            SetupCreateSession();
            SetupReturnsSubscriptions(subsToServers.Select(s => s.Item1));
            foreach(var s in subsToServers)
            {
                SetupAzureServers(s.Item1, s.Item2);
            }
            SetupFirewallResponse(response);
        }

        private void SetupReturnsSubscriptions(IEnumerable<IAzureUserAccountSubscriptionContext> subs)
        {
            AuthenticationManagerMock.Setup(a => a.GetSubscriptionsAsync()).Returns(() => Task.FromResult(subs));
        }

        private void SetupCreateSession()
        {
            ResourceManagerMock.Setup(r => r.CreateSessionAsync(It.IsAny<IAzureUserAccountSubscriptionContext>()))
                .Returns((IAzureUserAccountSubscriptionContext sub) =>
                {
                    var sessionMock = new Mock<IAzureResourceManagementSession>();
                    sessionMock.SetupProperty(s => s.SubscriptionContext, sub);
                    return Task.FromResult(sessionMock.Object);
                });
        }

        private void SetupAzureServers(IAzureSubscriptionContext sub, IEnumerable<IAzureSqlServerResource> servers)
        {
            Func<IAzureResourceManagementSession, bool> isExpectedSub = (session) =>
            {
                return session.SubscriptionContext == sub;
            };
            ResourceManagerMock.Setup(r => r.GetSqlServerAzureResourcesAsync(
                It.Is<IAzureResourceManagementSession>((session) => isExpectedSub(session))
            )).Returns(() => Task.FromResult(servers));
        }

        private void SetupFirewallResponse(FirewallRuleResponse response)
        {
            ResourceManagerMock.Setup(r => r.CreateFirewallRuleAsync(
                It.IsAny<IAzureResourceManagementSession>(),
                It.IsAny<IAzureSqlServerResource>(),
                It.IsAny<FirewallRuleRequest>())
            ).Returns(() => Task.FromResult(response));
        }

        private Account CreateAccount(bool needsReauthentication = false)
        {
            return new Account()
            {
                Key = new AccountKey()
                {
                    AccountId = "MyAccount",
                    ProviderId = "MSSQL"
                },
                IsStale = needsReauthentication
            };
        }
    }
}