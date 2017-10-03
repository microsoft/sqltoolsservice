////------------------------------------------------------------------------------
//// <copyright company="Microsoft">
////   Copyright (c) Microsoft Corporation.  All rights reserved.
//// </copyright>
////------------------------------------------------------------------------------

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.SqlTools.ResourceProvider.Core;
//using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
//using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;
//using Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes;
//using Xunit;

//namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider
//{
//    /// <summary>
//    /// Tests for ServiceManager to verify finding services and providers for specific type correctly 
//    /// </summary>
//    public class ServiceManagerTest
//    {
//        private IList<Lazy<IServerDiscoveryProvider, IExportableMetadata>> _providers;
//        private IList<Lazy<IAccountManager, IExportableMetadata>> _accountManagers;
        
//        public ServiceManagerTest()
//        {
//            _providers = new List<Lazy<IServerDiscoveryProvider, IExportableMetadata>>()
//            {
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => new FakeServerDiscoveryProvider2(),
//                new ExportableAttribute("SqlServer", "Local", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString())),
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => new FakeSecureServerDiscoveryProvider(),
//                new ExportableAttribute("SqlServer", "Azure", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString())),
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => new FakeServerDiscoveryProvider(), 
//                new ExportableAttribute("SqlServer", "Network", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString()))
//            };

//            _accountManagers = new List<Lazy<IAccountManager, IExportableMetadata>>()
//            {               
//                new Lazy<IAccountManager, IExportableMetadata>(() => new FakeAccountManager(), 
//                new ExportableAttribute("SqlServer", "Azure", typeof(IAccountManager), Guid.NewGuid().ToString())),
//                new Lazy<IAccountManager, IExportableMetadata>(() => new FakeAccountManager2(),
//                new ExportableAttribute("SqlServer", "Network", typeof(IAccountManager), Guid.NewGuid().ToString()))
//            };
//        }

//        [Fact]
//        public void GetServiceShouldReturnTheServiceThatHasGivenMetadataCorrectly()
//        {
//            //given
//            var serverDefinition = new ServerDefinition("SqlServer", "Azure");
//            IDependencyManager dependencyManager = CreateDependencyManager();
//            ServiceManager<IServerDiscoveryProvider> serverManager = new ServiceManager<IServerDiscoveryProvider>(dependencyManager);

//            //when
//            var service = serverManager.GetService(serverDefinition);

//            //then
//            Assert.NotNull(service);
//            Assert.True(service.GetType() == typeof(FakeSecureServerDiscoveryProvider));
//            Assert.True(serverManager.RequiresUserAccount(serverDefinition));
//        }

//        [Fact]
//        public void GetServiceShouldReturnNullGivenInvalidMetadata()
//        {
//            //given
//            var serverDefinition = new ServerDefinition(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
//            IDependencyManager dependencyManager = CreateDependencyManager();
//            ServiceManager<IServerDiscoveryProvider> serverManager = new ServiceManager<IServerDiscoveryProvider>(dependencyManager);

//            //when
//            var service = serverManager.GetService(serverDefinition);

//            //then
//            Assert.Null(service);
//            Assert.False(serverManager.RequiresUserAccount(serverDefinition));
//        }

//        [Fact]
//        public void GetServiceShouldReturnNullGivenUnSupportedMetadata()
//        {
//            //given
//            var serverDefinition = new ServerDefinition("SqlServer", "Local");
//            IDependencyManager dependencyManager = CreateDependencyManager();
//            ServiceManager<IAccountManager> serverManager = new ServiceManager<IAccountManager>(dependencyManager);

//            //when
//            var service = serverManager.GetService(serverDefinition);

//            //then
//            Assert.Null(service);
//            Assert.False(serverManager.RequiresUserAccount(serverDefinition));
//        }

//        [Fact]
//        public void RequiresUserAccountShouldReturnFalseGivenNotSecuredService()
//        {
//            //given
//            var serverDefinition = new ServerDefinition("SqlServer", "Local");
//            IDependencyManager dependencyManager = CreateDependencyManager();
//            ServiceManager<IServerDiscoveryProvider> serverManager = new ServiceManager<IServerDiscoveryProvider>(dependencyManager);

//            //when
//            var service = serverManager.GetService(serverDefinition);

//            //then
//            Assert.NotNull(service);
//            Assert.False(serverManager.RequiresUserAccount(serverDefinition));
//        }

//        [Fact]
//        public void GetShouldReturnDefaultAzureServiceGivenDefaultCatalog()
//        {
//            //given
//            SafeAssemblyCatalog assemblyCatalog = new SafeAssemblyCatalog(typeof(DependencyManager).Assembly);
//            SafeAssemblyCatalog assemblyCatalog2 = new SafeAssemblyCatalog(typeof(VsAzureAuthenticationManager).Assembly);
//            ExtensionProperties serviceProperties = new ExtensionProperties(false);
//            serviceProperties.AddCatalog(assemblyCatalog);
//            serviceProperties.AddCatalog(assemblyCatalog2);
//            var serverDefinition = new ServerDefinition("sqlserver", "azure");
//            DependencyManager dependencyManager = new DependencyManager(serviceProperties);
//            ServiceManager<IServerDiscoveryProvider> serverProviderServiceManager =
//                new ServiceManager<IServerDiscoveryProvider>(dependencyManager);
//            ServiceManager<IDatabaseDiscoveryProvider> databaseProviderServiceManager =
//                new ServiceManager<IDatabaseDiscoveryProvider>(dependencyManager);
//            ServiceManager<IServerConnectionProvider> serverConnectionProviderServiceManager =
//                new ServiceManager<IServerConnectionProvider>(dependencyManager);

//            //when
//            IServerDiscoveryProvider serverDiscoveryProvider = serverProviderServiceManager.GetService(serverDefinition);
//            IDatabaseDiscoveryProvider databaseDiscoveryProvider =
//                databaseProviderServiceManager.GetService(serverDefinition);
//            IServerConnectionProvider serverConnectionProvider =
//                serverConnectionProviderServiceManager.GetService(serverDefinition);

//            Assert.NotNull(serverDiscoveryProvider);
//            Assert.True(serverDiscoveryProvider is AzureSqlServerDiscoveryProvider);
//            Assert.True(serverProviderServiceManager.RequiresUserAccount(serverDefinition));

//            IAccountManager accountManager = serverProviderServiceManager.GetAccountManager(serverDefinition);
//            Assert.NotNull(accountManager);
//            Assert.True(accountManager is IAzureAuthenticationManager);

//            Assert.NotNull(databaseDiscoveryProvider);
//            Assert.True(databaseDiscoveryProvider is AzureDatabaseDiscoveryProvider);
//            Assert.True(databaseProviderServiceManager.RequiresUserAccount(serverDefinition));

//            Assert.NotNull(serverConnectionProvider);
//            Assert.True(serverConnectionProvider is SqlServerConnectionProvider);
//        }


//        [Fact]
//        public void GetShouldReturnImplementedAzureServiceIfFoundInCatalog()
//        {
//            //given
//            ExtensionProperties serviceProperties = new ExtensionProperties(true);
//            TypeCatalog typeCatalog = new TypeCatalog(typeof(FakeAzureServerDiscoveryProvider));
//            serviceProperties.AddCatalog(typeCatalog);
//            DependencyManager dependencyManager = new DependencyManager(serviceProperties);
//            ServiceManager<IServerDiscoveryProvider> serviceManager = new ServiceManager<IServerDiscoveryProvider>(dependencyManager);

//            //when
//            IServerDiscoveryProvider serverDiscoveryProvider = serviceManager.GetService(new ServerDefinition("sqlserver", "azure"));

//            Assert.NotNull(serverDiscoveryProvider);
//            Assert.True(serverDiscoveryProvider is FakeAzureServerDiscoveryProvider);
//        }

//        [Fact]
//        public void GetGetServiceOfExportableShouldReturnNullGivenSameTypeAsExportable()
//        {
//            //given
//            ExtensionProperties serviceProperties = new ExtensionProperties(true);
//            TypeCatalog typeCatalog = new TypeCatalog(typeof(FakeAzureServerDiscoveryProvider));
//            serviceProperties.AddCatalog(typeCatalog);
//            DependencyManager dependencyManager = new DependencyManager(serviceProperties);
//            ServiceManager<IServerDiscoveryProvider> serviceManager = new ServiceManager<IServerDiscoveryProvider>(dependencyManager);

//            //when
//            IServerDiscoveryProvider serverDiscoveryProvider = serviceManager.GetService(new ServerDefinition("sqlserver", "azure"));

//            Assert.NotNull(serverDiscoveryProvider);
//            FakeAzureServerDiscoveryProvider fakeAzureServerDiscovery = serverDiscoveryProvider as FakeAzureServerDiscoveryProvider;
//            Assert.NotNull(fakeAzureServerDiscovery);
//            Assert.Null(fakeAzureServerDiscovery.ServerDiscoveryProvider);
//        }

//        private IDependencyManager CreateDependencyManager()
//        {
//            IDependencyManager dependencyManager = new Mock<IDependencyManager>();

//            IEnumerable<ExportableDescriptor<IServerDiscoveryProvider>> serviceDescriptorsForServerDiscoveryProviders =
//               _providers.Select(
//                   x =>
//                       new ExportableDescriptorImpl<IServerDiscoveryProvider>(
//                           new ExtensionDescriptor<IServerDiscoveryProvider, IExportableMetadata>(x)));

//            dependencyManager.Setup(x => x.GetServiceDescriptors<IServerDiscoveryProvider>()).Returns(serviceDescriptorsForServerDiscoveryProviders);

//            IEnumerable<ExportableDescriptor<IAccountManager>> serviceDescriptorsForAccountManagers =
//              _accountManagers.Select(
//                  x =>
//                      new ExportableDescriptorImpl<IAccountManager>(
//                          new ExtensionDescriptor<IAccountManager, IExportableMetadata>(x)));

//            dependencyManager.Setup(x => x.GetServiceDescriptors<IAccountManager>()).Returns(serviceDescriptorsForAccountManagers);

//            return dependencyManager;
//        }
//    }
//}
