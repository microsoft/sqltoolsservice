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

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Formatter
{
    public class ResourceProviderServiceTests
    {
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


        
    }
}