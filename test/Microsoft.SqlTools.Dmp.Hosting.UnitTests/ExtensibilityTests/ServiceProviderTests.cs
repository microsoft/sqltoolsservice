//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using Microsoft.SqlTools.Dmp.Hosting.Extensibility;
using Microsoft.SqlTools.Dmp.Hosting.Utility;
using Xunit;

namespace Microsoft.SqlTools.Dmp.Hosting.UnitTests.ExtensibilityTests
{
    public class ServiceProviderTests
    {
        private readonly RegisteredServiceProvider provider;
        public ServiceProviderTests()
        {
            provider = new RegisteredServiceProvider();
        }

        [Fact]
        public void GetServiceShouldReturnNullIfNoServicesRegistered()
        {
            // Given no service registered
            // When I call GetService
            var service = provider.GetService<MyProviderService>();
            // Then I expect null to be returned
            Assert.Null(service);
        }


        [Fact]
        public void GetSingleServiceThrowsMultipleServicesRegistered()
        {
            // Given 2 services registered
            provider.Register(() => new[] { new MyProviderService(), new MyProviderService() });
            // When I call GetService
            // Then I expect to throw
            Assert.Throws<InvalidOperationException>(() => provider.GetService<MyProviderService>());
        }

        [Fact]
        public void GetServicesShouldReturnEmptyIfNoServicesRegistered()
        {
            // Given no service regisstered
            // When I call GetService
            var services = provider.GetServices<MyProviderService>();
            // Then I expect empty enumerable to be returned
            Assert.NotNull(services);
            Assert.Equal(0, services.Count());
        }

        [Fact]
        public void GetServiceShouldReturnRegisteredService()
        {
            MyProviderService service = new MyProviderService();
            provider.RegisterSingleService(service);

            var returnedService = provider.GetService<MyProviderService>();
            Assert.Equal(service, returnedService);            
        }

        [Fact]
        public void GetServicesShouldReturnRegisteredServiceWhenMultipleServicesRegistered()
        {
            MyProviderService service = new MyProviderService();
            provider.RegisterSingleService(service);          

            var returnedServices = provider.GetServices<MyProviderService>();
            Assert.Equal(service, returnedServices.Single());
        }

//        [Fact]
//        public void RegisterServiceProviderShouldThrowIfServiceIsIncompatible()
//        {
//            MyProviderService service = new MyProviderService();
//            Assert.Throws<InvalidOperationException>(() => provider.RegisterSingleService(typeof(OtherService), service));
//        }
//        [Fact]
//        public void RegisterServiceProviderShouldThrowIfServiceAlreadyRegistered()
//        {
//            MyProviderService service = new MyProviderService();
//            provider.RegisterSingleService(service);
//
//            Assert.Throws<InvalidOperationException>(() => provider.RegisterSingleService(service));
//        }
//        
//        [Fact]
//        public void RegisterShouldThrowIfServiceAlreadyRegistered()
//        {
//            MyProviderService service = new MyProviderService();
//            provider.RegisterSingleService(service);
//
//            Assert.Throws<InvalidOperationException>(() => provider.Register(() => service.AsSingleItemEnumerable()));
//        }
//
//        [Fact]
//        public void RegisterShouldThrowIfServicesAlreadyRegistered()
//        {
//            provider.Register(() => new [] { new MyProviderService(), new MyProviderService() });
//            Assert.Throws<InvalidOperationException>(() => provider.Register(() => new MyProviderService().AsSingleItemEnumerable()));
//        }
    }


    public class MyProviderService
    {

    }

    public class OtherService
    {

    }
}
