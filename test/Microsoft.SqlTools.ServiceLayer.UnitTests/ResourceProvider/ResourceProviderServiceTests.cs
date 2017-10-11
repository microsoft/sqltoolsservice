//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ResourceProvider.Core;
using Moq;
using Microsoft.SqlTools.ResourceProvider;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Xunit;
using System.Threading.Tasks;
using Microsoft.SqlTools.ResourceProvider.Core.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;

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
    }
}