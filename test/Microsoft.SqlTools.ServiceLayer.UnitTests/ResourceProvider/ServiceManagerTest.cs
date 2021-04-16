//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;
using Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Azure;
using Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider
{
    /// <summary>
    /// Tests for ServiceManager to verify finding services and providers for specific type correctly 
    /// </summary>
    public class ServiceManagerTest
    {
        private IList<IServerDiscoveryProvider> _providers;
        private IList<IAccountManager> _accountManagers;

        public ServiceManagerTest()
        {
            _providers = new List<IServerDiscoveryProvider>()
            {
                new FakeServerDiscoveryProvider2(new ExportableMetadata("SqlServer", "Local", Guid.NewGuid().ToString())),
                new FakeSecureServerDiscoveryProvider(new ExportableMetadata("SqlServer", "Azure", Guid.NewGuid().ToString())),
                new FakeServerDiscoveryProvider(new ExportableMetadata("SqlServer", "Network", Guid.NewGuid().ToString()))
            };

            _accountManagers = new List<IAccountManager>()
            {
                new FakeAccountManager(new ExportableMetadata("SqlServer", "Azure", Guid.NewGuid().ToString())),
                new FakeAccountManager2(new ExportableMetadata("SqlServer", "Network", Guid.NewGuid().ToString()))
            };
        }

        [Test]
        public void GetServiceShouldReturnTheServiceThatHasGivenMetadataCorrectly()
        {
            //given
            var serverDefinition = new ServerDefinition("SqlServer", "Azure");
            IMultiServiceProvider provider = CreateServiceProvider();

            //when
            IServerDiscoveryProvider service = ExtensionUtils.GetService<IServerDiscoveryProvider>(provider, serverDefinition);

            //then
            Assert.NotNull(service);
            Assert.True(service.GetType() == typeof(FakeSecureServerDiscoveryProvider));
        }

        [Test]
        public void GetServiceShouldReturnNullGivenInvalidMetadata()
        {
            //given
            var serverDefinition = new ServerDefinition(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            IMultiServiceProvider provider = CreateServiceProvider();

            //when
            IServerDiscoveryProvider service = ExtensionUtils.GetService<IServerDiscoveryProvider>(provider, serverDefinition);

            //then
            Assert.Null(service);
        }

        [Test]
        public void GetServiceShouldReturnNullGivenUnSupportedMetadata()
        {
            //given
            var serverDefinition = new ServerDefinition("SqlServer", "Local");
            IMultiServiceProvider provider = CreateServiceProvider();

            //when
            IAccountManager service = ExtensionUtils.GetService<IAccountManager>(provider, serverDefinition);

            //then
            Assert.Null(service);
        }

        [Test]
        public void RequiresUserAccountShouldReturnFalseGivenNotSecuredService()
        {
            //given
            var serverDefinition = new ServerDefinition("SqlServer", "Local");
            IMultiServiceProvider provider = CreateServiceProvider();

            //when
            IServerDiscoveryProvider service = ExtensionUtils.GetService<IServerDiscoveryProvider>(provider, serverDefinition);

            //then
            Assert.NotNull(service);
        }

        [Test]
        public void GetShouldReturnDefaultAzureServiceGivenDefaultCatalog()
        {
            // given
            ExtensionServiceProvider provider = ExtensionServiceProvider.Create(new Assembly[]
            {
                typeof(IAccountManager).Assembly,
                typeof(IAzureResourceManager).Assembly
            });
            var serverDefinition = new ServerDefinition("sqlserver", "azure");

            // when I query each provider
            IServerDiscoveryProvider serverDiscoveryProvider = ExtensionUtils.GetService<IServerDiscoveryProvider>(provider, serverDefinition);
            // Then I get a valid provider back
            Assert.NotNull(serverDiscoveryProvider);
            Assert.True(serverDiscoveryProvider is AzureSqlServerDiscoveryProvider);

            IDatabaseDiscoveryProvider databaseDiscoveryProvider =
                ExtensionUtils.GetService<IDatabaseDiscoveryProvider>(provider, serverDefinition);

            // TODO Verify account manager is detected as soon as the account manager has a real implementation
            //IAccountManager accountManager = ((AzureSqlServerDiscoveryProvider)serverDiscoveryProvider).AccountManager;
            //Assert.NotNull(accountManager);
            //Assert.True(accountManager is IAzureAuthenticationManager);

            Assert.NotNull(databaseDiscoveryProvider);
            Assert.True(databaseDiscoveryProvider is AzureDatabaseDiscoveryProvider);
        }


        [Test]
        public void GetShouldReturnImplementedAzureServiceIfFoundInCatalog()
        {
            //given
            ExtensionServiceProvider provider = ExtensionServiceProvider.Create(typeof(FakeAzureServerDiscoveryProvider).SingleItemAsEnumerable());

            //when
            IServerDiscoveryProvider serverDiscoveryProvider = ExtensionUtils.GetService<IServerDiscoveryProvider>(provider, new ServerDefinition("sqlserver", "azure"));

            Assert.NotNull(serverDiscoveryProvider);
            Assert.True(serverDiscoveryProvider is FakeAzureServerDiscoveryProvider);
        }

        [Test]
        public void GetGetServiceOfExportableShouldReturnNullGivenSameTypeAsExportable()
        {
            //given
            ExtensionServiceProvider provider = ExtensionServiceProvider.Create(typeof(FakeAzureServerDiscoveryProvider).SingleItemAsEnumerable());

            //when
            IServerDiscoveryProvider serverDiscoveryProvider = ExtensionUtils.GetService<IServerDiscoveryProvider>(provider, new ServerDefinition("sqlserver", "azure"));

            Assert.NotNull(serverDiscoveryProvider);
            FakeAzureServerDiscoveryProvider fakeAzureServerDiscovery = serverDiscoveryProvider as FakeAzureServerDiscoveryProvider;
            Assert.NotNull(fakeAzureServerDiscovery);
            Assert.Null(fakeAzureServerDiscovery.ServerDiscoveryProvider);
        }

        private IMultiServiceProvider CreateServiceProvider()
        {
            var providerMock = new Mock<IMultiServiceProvider>();
            
            providerMock.Setup(x => x.GetServices<IServerDiscoveryProvider>()).Returns(_providers);
            providerMock.Setup(x => x.GetServices<IAccountManager>()).Returns(_accountManagers);

            return providerMock.Object;
        }
    }
}
