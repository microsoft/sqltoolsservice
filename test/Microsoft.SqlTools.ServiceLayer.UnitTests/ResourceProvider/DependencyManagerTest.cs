//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// TODO Ideally would reenable these but using ExtensionServiceProvider

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.SqlTools.ResourceProvider.Core;
//using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;
//using Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes;
//using Moq;
//using NUnit.Framework;

//namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider
//{
//    /// <summary>
//    /// Tests for DependencyManager to verify the services and providers can be created given different types of catalogs
//    /// </summary>
//    public class DependencyManagerTest
//    {
//        private ExtensionProperties _serviceProperties;
//        private DependencyManager _dependencyManager;
//        private IList<Lazy<IServerDiscoveryProvider, IExportableMetadata>> _providers;

//        private readonly List<ServerInstanceInfo> _localSqlServers = new List<ServerInstanceInfo>()
//        {
//            new ServerInstanceInfo(),
//            new ServerInstanceInfo(),
//        };

//        public DependencyManagerTest()
//        {
//            var provider1 = new Mock<IServerDiscoveryProvider>();
//            var provider2 = new Mock<IServerDiscoveryProvider>();
//            provider1.Setup(x => x.GetServerInstancesAsync()).Returns(Task.FromResult(new ServiceResponse<ServerInstanceInfo>(_localSqlServers.AsEnumerable())));
//            _providers = new List<Lazy<IServerDiscoveryProvider, IExportableMetadata>>()
//            {
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => provider1.Object,
//                new ExportableAttribute("SqlServer", "Local", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString())),
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => provider2.Object,
//                new ExportableAttribute("SqlServer", "Network", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString())),
//            };

//            _serviceProperties = FakeDataFactory.CreateServiceProperties(_providers);
//            _dependencyManager = new DependencyManager(_serviceProperties);
//        }

//        [Test]
//        public void GetShouldReturnProvidersFromTheCatalog()
//        {
//            IEnumerable<ExportableDescriptor<IServerDiscoveryProvider>> providers =
//                _dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>();
//            Assert.NotNull(providers);
//        }

//        [Test]
//        public void GetShouldReturnEmptyListGivenInvalidCategory()
//        {
//            Assert.False(_dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>(new ServerDefinition(null, "invalid category")).Any());
//        }

//        [Test]
//        public void GetShouldReturnEmptyListGivenInvalidServerType()
//        {
//            Assert.False(_dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>(new ServerDefinition("invalid server type", null)).Any());
//        }

//        [Test]
//        public void GetShouldReturnAllProvidersGivenNoParameter()
//        {
//            IEnumerable<ExportableDescriptor<IServerDiscoveryProvider>> providers =
//                _dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>();
//            Assert.NotNull(providers);
//            Assert.True(providers.Count() == _providers.Count());
//        }

//        [Test]
//        public void GetShouldReturnProvidersGivenServerType()
//        {
//            var serverType = "sqlServer";
//            IEnumerable<ExportableDescriptor<IServerDiscoveryProvider>> providers =
//                _dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>(new ServerDefinition(serverType, null));
//            Assert.NotNull(providers);
//            Assert.True(providers.Any());
//            Assert.True(providers.Count() == _providers.Count(x => x.Metadata.ServerType.Equals(serverType, StringComparison.OrdinalIgnoreCase)));
//        }

//        [Test]
//        public void GetShouldReturnProvidersGivenCategory()
//        {
//            IEnumerable<ExportableDescriptor<IServerDiscoveryProvider>> providers =
//                _dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>(new ServerDefinition(null, "local"));
//            Assert.NotNull(providers);
//            Assert.True(providers.Count() == 1);
//        }

//        [Test]
//        public void GetShouldReturnProviderForEmptyCategoryGivenEmptyCategory()
//        {
//            // Given choice of 2 providers, one with empty category and other with specified one

//            IServerDiscoveryProvider provider1 = new Mock<IServerDiscoveryProvider>();
//            IServerDiscoveryProvider provider2 = new Mock<IServerDiscoveryProvider>();
//            provider1.Setup(x => x.GetServerInstancesAsync()).Returns(Task.FromResult(new ServiceResponse<ServerInstanceInfo>(_localSqlServers.AsEnumerable())));
//            var providers = new List<Lazy<IServerDiscoveryProvider, IExportableMetadata>>()
//            {
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => provider1,
//                new ExportableAttribute("SqlServer", "Azure", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString())),
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => provider2,
//                new ExportableAttribute("SqlServer", "", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString())),
//            };

//            var serviceProperties = FakeDataFactory.CreateServiceProperties(providers);
//            var dependencyManager = new DependencyManager(serviceProperties);

//            // When getting the correct descriptor

//            IEnumerable<ExportableDescriptor<IServerDiscoveryProvider>> foundProviders =
//                dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>(new ServerDefinition("SqlServer", ""));

//            // Then expect only the provider with the empty categorty to be returned 
//            Assert.NotNull(foundProviders);
//            Assert.True(foundProviders.Count() == 1);
//        }

//        [Test]
//        public void GetShouldReturnProviderGivenServerTypeAndLocationWithValidProvider()
//        {
//            IEnumerable<ExportableDescriptor<IServerDiscoveryProvider>> providers =
//                _dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>(new ServerDefinition("SqlServer", "local"));
//            Assert.NotNull(providers);
//            Assert.True(providers.Count() == 1);
//        }

//        [Test]

//        public void GetShouldReturnTheServiceWithTheHighestPriorityIdMultipleFound()
//        {
//            IServerDiscoveryProvider expectedProvider = new Mock<IServerDiscoveryProvider>();
//            List<Lazy<IServerDiscoveryProvider, IExportableMetadata>> providers = new List<Lazy<IServerDiscoveryProvider, IExportableMetadata>>()
//            {
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => new Mock<IServerDiscoveryProvider>(),
//                new ExportableAttribute("SqlServer", "Local", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString())),
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => new Mock<IServerDiscoveryProvider>(),
//                new ExportableAttribute("SqlServer", "Network", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString(), 1)),
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => expectedProvider,
//                new ExportableAttribute("SqlServer", "Network", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString(), 2))
//            };

//            ExtensionProperties serviceProperties = FakeDataFactory.CreateServiceProperties(providers);
//            DependencyManager dependencyManager = new DependencyManager(serviceProperties);

//            IEnumerable<ExportableDescriptor<IServerDiscoveryProvider>> descriptors =
//                dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>();
//            Assert.NotNull(descriptors);

//            ExportableDescriptor<IServerDiscoveryProvider> descriptor = descriptors.FindMatchedDescriptor(new ServerDefinition("SqlServer", "network"));
//            Assert.NotNull(descriptor);
//            Assert.True(descriptor.Exportable == expectedProvider);
//        }

//        [Test]
//        public void GetShouldReturnTheServiceEvenIfTheServerTypeNotSet()
//        {
//            IServerDiscoveryProvider expectedProvider = new Mock<IServerDiscoveryProvider>();
//            List<Lazy<IServerDiscoveryProvider, IExportableMetadata>> providers = new List<Lazy<IServerDiscoveryProvider, IExportableMetadata>>()
//            {
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => new Mock<IServerDiscoveryProvider>(),
//                new ExportableAttribute("SqlServer", "", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString())),
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => new Mock<IServerDiscoveryProvider>(),
//                new ExportableAttribute("SqlServer", "Network", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString(), 1)),
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => expectedProvider,
//                new ExportableAttribute("", "Network", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString(), 2))
//            };

//            ExtensionProperties serviceProperties = FakeDataFactory.CreateServiceProperties(providers);
//            DependencyManager dependencyManager = new DependencyManager(serviceProperties);

//            IEnumerable<ExportableDescriptor<IServerDiscoveryProvider>> descriptors =
//                dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>();
//            Assert.NotNull(descriptors);

//            ExportableDescriptor<IServerDiscoveryProvider> descriptor = descriptors.FindMatchedDescriptor(new ServerDefinition("", "network"));
//            Assert.NotNull(descriptor);
//            Assert.True(descriptor.Exportable == expectedProvider);
//        }

//        [Test]
//        public void GetShouldReturnTheServiceThatMatchedExactlyIfServerTypeSpecified()
//        {
//            IServerDiscoveryProvider expectedProvider = new Mock<IServerDiscoveryProvider>();
//            List<Lazy<IServerDiscoveryProvider, IExportableMetadata>> providers = new List<Lazy<IServerDiscoveryProvider, IExportableMetadata>>()
//            {
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => new Mock<IServerDiscoveryProvider>(),
//                new ExportableAttribute("SqlServer", "", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString())),
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => expectedProvider,
//                new ExportableAttribute("SqlServer", "Network", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString(), 1)),
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => new Mock<IServerDiscoveryProvider>(),
//                new ExportableAttribute("", "Network", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString(), 2))
//            };

//            ExtensionProperties serviceProperties = FakeDataFactory.CreateServiceProperties(providers);
//            DependencyManager dependencyManager = new DependencyManager(serviceProperties);

//            IEnumerable<ExportableDescriptor<IServerDiscoveryProvider>> descriptors =
//                dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>();
//            Assert.NotNull(descriptors);

//            ExportableDescriptor<IServerDiscoveryProvider> descriptor = descriptors.FindMatchedDescriptor(new ServerDefinition("SqlServer", "network"));
//            Assert.NotNull(descriptor);
//            Assert.True(descriptor.Exportable == expectedProvider);
//        }

//        [Test]
//        public void GetShouldReturnTheServiceThatMatchedExactlyIfCategorySpecified()
//        {
//            IServerDiscoveryProvider expectedProvider = new Mock<IServerDiscoveryProvider>();
//            List<Lazy<IServerDiscoveryProvider, IExportableMetadata>> providers = new List<Lazy<IServerDiscoveryProvider, IExportableMetadata>>()
//            {
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => new Mock<IServerDiscoveryProvider>(),
//                new ExportableAttribute("SqlServer", "", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString())),
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => expectedProvider,
//                new ExportableAttribute("SqlServer", "Network", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString(), 1)),
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => new Mock<IServerDiscoveryProvider>(),
//                new ExportableAttribute("", "Network", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString(), 2))
//            };

//            ExtensionProperties serviceProperties = FakeDataFactory.CreateServiceProperties(providers);
//            DependencyManager dependencyManager = new DependencyManager(serviceProperties);

//            IEnumerable<ExportableDescriptor<IServerDiscoveryProvider>> descriptors =
//                dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>();
//            Assert.NotNull(descriptors);

//            ExportableDescriptor<IServerDiscoveryProvider> descriptor = descriptors.FindMatchedDescriptor(new ServerDefinition("SqlServer", "network"));
//            Assert.NotNull(descriptor);
//            Assert.True(descriptor.Exportable == expectedProvider);
//        }

//        [Test]

//        public void GetShouldReturnTheServiceEvenIfTheCategoryNotSet()
//        {
//            IServerDiscoveryProvider expectedProvider = new Mock<IServerDiscoveryProvider>();
//            List<Lazy<IServerDiscoveryProvider, IExportableMetadata>> providers = new List<Lazy<IServerDiscoveryProvider, IExportableMetadata>>()
//            {
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => new Mock<IServerDiscoveryProvider>(),
//                new ExportableAttribute("SqlServer", "Local", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString())),
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => new Mock<IServerDiscoveryProvider>(),
//                new ExportableAttribute("SqlServer", "Network", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString(), 1)),
//                new Lazy<IServerDiscoveryProvider, IExportableMetadata>(() => expectedProvider,
//                new ExportableAttribute("SqlServer", "", typeof(IServerDiscoveryProvider), Guid.NewGuid().ToString(), 2))
//            };

//            ExtensionProperties serviceProperties = FakeDataFactory.CreateServiceProperties(providers);
//            DependencyManager dependencyManager = new DependencyManager(serviceProperties);

//            IEnumerable<ExportableDescriptor<IServerDiscoveryProvider>> descriptors =
//                dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>();
//            Assert.NotNull(descriptors);

//            ExportableDescriptor<IServerDiscoveryProvider> descriptor = descriptors.FindMatchedDescriptor(new ServerDefinition("SqlServer", ""));
//            Assert.NotNull(descriptor);
//            Assert.True(descriptor.Exportable == expectedProvider);
//        }

//        [Test]
//        public void GetShouldReturnProvidersGivenServerTypeAndMoreThanOneLocation()
//        {
//            var serverType = "sqlServer";
//            IEnumerable<ExportableDescriptor<IServerDiscoveryProvider>> providers =
//                _dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>(new ServerDefinition(serverType, null));
//            Assert.NotNull(providers);
//            Assert.True(providers.Count() == _providers.Count(x => x.Metadata.ServerType.Equals(serverType, StringComparison.OrdinalIgnoreCase)));
//        }

//        [Test]
//        public async Task ProviderCreatedByFactoryShouldReturnServersSuccessfully()
//        {
//            List<ServerInstanceInfo> expectedServers = _localSqlServers;
//            IEnumerable<ExportableDescriptor<IServerDiscoveryProvider>> providers =
//                _dependencyManager.GetServiceDescriptors<IServerDiscoveryProvider>(new ServerDefinition("SqlServer",
//                    "local"));
//            ExportableDescriptor<IServerDiscoveryProvider> provider = providers.First();
//            Assert.NotNull(provider);
//            ServiceResponse<ServerInstanceInfo> result = await provider.Exportable.GetServerInstancesAsync();
//            var servers = result.Data;
//            Assert.NotNull(servers);
//            Assert.AreEqual(expectedServers, servers);
//        }
//    }
//}
