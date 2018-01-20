//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.Dmp.Contracts;
using Microsoft.SqlTools.Dmp.Contracts.Hosting;
using Microsoft.SqlTools.Dmp.Hosting.Channels;
using Microsoft.SqlTools.Dmp.Hosting.Extensibility;
using Microsoft.SqlTools.Dmp.Hosting.Protocol;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.Dmp.Hosting.UnitTests.ServiceHostTests
{
    public class ExtensibleServiceHostTest
    {
        [Fact]
        public void CreateExtensibleHostNullProvider()
        {
            // If: I create an extensible host with a null provider
            // Then: I should get an exception
            var cb = new Mock<ChannelBase>();
            Assert.Throws<ArgumentNullException>(() => new ExtensibleServiceHost(null, cb.Object, new ProviderDetails(), new LanguageServiceCapabilities()));
        }
        
        [Fact]
        public void CreateExtensibleHost()
        {
            // Setup: 
            // ... Create a mock hosted service that can initialize
            var hs = new Mock<IHostedService>();
            var mockType = typeof(Mock<IHostedService>);
            hs.Setup(o => o.InitializeService(It.IsAny<IServiceHost>()));
            hs.SetupGet(o => o.ServiceType).Returns(mockType);
            
            // ... Create a service provider mock that will return some stuff
            var sp = new Mock<RegisteredServiceProvider>();
            sp.Setup(o => o.GetServices<IHostedService>())
                .Returns(new[] {hs.Object});
            sp.Setup(o => o.RegisterSingleService(mockType, hs.Object));
            
            // If: I create an extensible host with a custom provider
            var cb = new Mock<ChannelBase>();
            var esh = new ExtensibleServiceHost(sp.Object, cb.Object, new ProviderDetails(), new LanguageServiceCapabilities());
            
            // Then:
            // ... The service provider should have had the IHostedService registered
            sp.Verify(o => o.RegisterSingleService(It.IsAny<Type>(), It.IsAny<IHostedService>()), Times.Once);
            
            // ... The service should have been initialized
            hs.Verify(o => o.InitializeService(esh), Times.Once());
            
            // ... The service host should have it's provider exposed
            Assert.Equal(sp.Object, esh.ServiceProvider);
        }

        [Fact]
        public void CreateDefaultExtensibleHostNullAssemblyList()
        {
            // If: I create a default server extensible host with a null provider
            // Then: I should get an exception
            var cb = new Mock<ChannelBase>();
            Assert.Throws<ArgumentNullException>(() => ExtensibleServiceHost.CreateDefaultExtensibleServer(".", null, new ProviderDetails(), new LanguageServiceCapabilities()));
        }
        
        [Fact]
        public void CreateDefaultExtensibleHost()
        {
            // If: I create a default server extensible host
            var esh = ExtensibleServiceHost.CreateDefaultExtensibleServer(".", new string[] { }, new ProviderDetails(), new LanguageServiceCapabilities());
            
            // Then: 
            // ... The service provider should be setup
            Assert.NotNull(esh.ServiceProvider);
            
            // ... The underlying rpc host should be using the stdio server channel 
            var jh = esh.jsonRpcHost as JsonRpcHost;
            Assert.NotNull(jh);
            Assert.IsType<StdioServerChannel>(jh.protocolChannel);
            Assert.False(jh.protocolChannel.IsConnected);
        }
    }
}