//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using Microsoft.SqlTools.Hosting.Extensibility;
using Microsoft.SqlTools.Hosting.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.Hosting.UnitTests.ExtensibilityTests
{

    [TestFixture]
    public class ServiceProviderTests
    {
        private RegisteredServiceProvider provider;

        [SetUp]
        public void SetupServiceProviderTests()
        {
            provider = new RegisteredServiceProvider();
        } 

        [Test]
        public void GetServiceShouldReturnNullIfNoServicesRegistered()
        {
            // Given no service registered
            // When I call GetService
            var service = provider.GetService<MyProviderService>();
            // Then I expect null to be returned
            Assert.Null(service);
        }


        [Test]
        public void GetSingleServiceThrowsMultipleServicesRegistered()
        {
            // Given 2 services registered
            provider.Register(() => new[] { new MyProviderService(), new MyProviderService() });
            // When I call GetService
            // Then I expect to throw
            Assert.Throws<InvalidOperationException>(() => provider.GetService<MyProviderService>());
        }

        [Test]
        public void GetServicesShouldReturnEmptyIfNoServicesRegistered()
        {
            // Given no service regisstered
            // When I call GetService
            var services = provider.GetServices<MyProviderService>();
            Assert.That(services, Is.Empty, "provider.GetServices with no service registered");
        }

        [Test]
        public void GetServiceShouldReturnRegisteredService()
        {
            MyProviderService service = new MyProviderService();
            provider.RegisterSingleService(service);

            var returnedService = provider.GetService<MyProviderService>();
            Assert.AreEqual(service, returnedService);            
        }

        // DEVNOTE: The name of this test doesn't match the code, which is pretty much the same as the one above.
        [Test]
        public void GetServicesShouldReturnRegisteredServiceWhenMultipleServicesRegistered()
        {
            MyProviderService service = new MyProviderService();
            provider.RegisterSingleService(service);

            var returnedServices = provider.GetServices<MyProviderService>();
            Assert.AreEqual(service, returnedServices.Single());
        }

        [Test]
        public void RegisterServiceProviderShouldThrowIfServiceIsIncompatible()
        {
            MyProviderService service = new MyProviderService();
            Assert.Throws<InvalidOperationException>(() => provider.RegisterSingleService(typeof(OtherService), service));
        }
        [Test]
        public void RegisterServiceProviderShouldThrowIfServiceAlreadyRegistered()
        {
            MyProviderService service = new MyProviderService();
            provider.RegisterSingleService(service);

            Assert.Throws<InvalidOperationException>(() => provider.RegisterSingleService(service));
        }
        
        [Test]
        public void RegisterShouldThrowIfServiceAlreadyRegistered()
        {
            MyProviderService service = new MyProviderService();
            provider.RegisterSingleService(service);

            Assert.Throws<InvalidOperationException>(() => provider.Register(() => service.AsSingleItemEnumerable()));
        }

        [Test]
        public void RegisterShouldThrowIfServicesAlreadyRegistered()
        {
            provider.Register(() => new [] { new MyProviderService(), new MyProviderService() });
            Assert.Throws<InvalidOperationException>(() => provider.Register(() => new MyProviderService().AsSingleItemEnumerable()));
        }
    }


    public class MyProviderService
    {

    }

    public class OtherService
    {

    }
}
