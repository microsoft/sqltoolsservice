//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Broker;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.SqlCore.ObjectExplorer;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.Nodes;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{
    /// <summary>
    /// Unit tests for Service Broker object explorer nodes and queriers.
    /// </summary>
    public class ServiceBrokerTests
    {
        private string fakeConnectionString = "Data Source=server;Initial Catalog=database;Integrated Security=False;User Id=user";
        private IMultiServiceProvider serviceProvider;

        [SetUp]
        public void Setup()
        {
            var assembly = typeof(SmoQuerier).Assembly;
            serviceProvider = ExtensionServiceProvider.CreateFromAssembliesInDirectory(
                Path.GetDirectoryName(assembly.Location),
                new[] { Path.GetFileName(assembly.Location) });
        }

        // Querier registration

        [Test]
        public void SqlMessageTypeQuerierShouldBeRegistered()
        {
            var querier = serviceProvider.GetService<SmoQuerier>(q => q.SupportedObjectTypes.Contains(typeof(MessageType)));
            Assert.NotNull(querier);
            Assert.AreEqual(typeof(SqlMessageTypeQuerier), querier.GetType());
        }

        [Test]
        public void SqlContractQuerierShouldBeRegistered()
        {
            var querier = serviceProvider.GetService<SmoQuerier>(q => q.SupportedObjectTypes.Contains(typeof(ServiceContract)));
            Assert.NotNull(querier);
            Assert.AreEqual(typeof(SqlContractQuerier), querier.GetType());
        }

        [Test]
        public void SqlQueueQuerierShouldBeRegistered()
        {
            var querier = serviceProvider.GetService<SmoQuerier>(q => q.SupportedObjectTypes.Contains(typeof(ServiceQueue)));
            Assert.NotNull(querier);
            Assert.AreEqual(typeof(SqlQueueQuerier), querier.GetType());
        }

        [Test]
        public void SqlServiceQuerierShouldBeRegistered()
        {
            var querier = serviceProvider.GetService<SmoQuerier>(q => q.SupportedObjectTypes.Contains(typeof(BrokerService)));
            Assert.NotNull(querier);
            Assert.AreEqual(typeof(SqlServiceQuerier), querier.GetType());
        }

        // Querier supported types

        [Test]
        public void SqlMessageTypeQuerierSupportedTypesShouldContainMessageType()
        {
            var querier = new SqlMessageTypeQuerier();
            Assert.That(querier.SupportedObjectTypes, Contains.Item(typeof(MessageType)));
        }

        [Test]
        public void SqlContractQuerierSupportedTypesShouldContainServiceContract()
        {
            var querier = new SqlContractQuerier();
            Assert.That(querier.SupportedObjectTypes, Contains.Item(typeof(ServiceContract)));
        }

        [Test]
        public void MessageTypesChildFactoryShouldIncludeSystemMessageTypesSubfolder()
        {
            var context = new SmoQueryContext(
                new Server(new ServerConnection(new SqlConnection(fakeConnectionString))),
                serviceProvider);
            context.ValidFor = ValidForFlag.AllOnPrem;

            var parent = new TestFolderNode(context)
            {
                NodeValue = Microsoft.SqlTools.SqlCore.SR.SchemaHierarchy_MessageTypes,
                NodeTypeId = NodeTypes.MessageTypes,
            };

            var factory = new MessageTypesChildFactory();
            var children = factory.Expand(parent, refresh: false, name: null, includeSystemObjects: true, CancellationToken.None).ToList();

            var systemFolder = children.OfType<FolderNode>().SingleOrDefault(n => n.NodeTypeId == NodeTypes.SystemMessageTypes);
            Assert.NotNull(systemFolder, "System Message Types subfolder should exist under Message Types");
            Assert.AreEqual(Microsoft.SqlTools.SqlCore.SR.SchemaHierarchy_SystemMessageTypes, systemFolder.NodeValue);
            Assert.IsFalse(systemFolder.IsSystemObject, "System subfolder itself should not be marked IsSystemObject");
        }

        [Test]
        public void MessageTypesChildFactoryShouldFilterOutSystemObjects()
        {
            var factory = new MessageTypesChildFactory();
            var isSystemFilter = factory.Filters.OfType<NodePropertyFilter>().SingleOrDefault(f => f.Property == "IsSystemObject");
            Assert.NotNull(isSystemFilter, "MessageTypes factory should have an IsSystemObject filter");
            CollectionAssert.AreEqual(new object[] { 0 }, isSystemFilter.Values,
                "MessageTypes should only show user objects (IsSystemObject = 0)");
        }

        [Test]
        public void SystemMessageTypesChildFactoryShouldFilterInSystemObjects()
        {
            var factory = new SystemMessageTypesChildFactory();
            var isSystemFilter = factory.Filters.OfType<NodePropertyFilter>().SingleOrDefault(f => f.Property == "IsSystemObject");
            Assert.NotNull(isSystemFilter, "SystemMessageTypes factory should have an IsSystemObject filter");
            CollectionAssert.AreEqual(new object[] { 1 }, isSystemFilter.Values,
                "SystemMessageTypes should only show system objects (IsSystemObject = 1)");
        }

        // Same filter pattern for Contracts, Queues, Services

        [Test]
        public void ContractsChildFactoryShouldFilterOutSystemObjects()
        {
            var factory = new ContractsChildFactory();
            var isSystemFilter = factory.Filters.OfType<NodePropertyFilter>().SingleOrDefault(f => f.Property == "IsSystemObject");
            Assert.NotNull(isSystemFilter);
            CollectionAssert.AreEqual(new object[] { 0 }, isSystemFilter.Values);
        }

        [Test]
        public void QueuesChildFactoryShouldFilterOutSystemObjects()
        {
            var factory = new QueuesChildFactory();
            var isSystemFilter = factory.Filters.OfType<NodePropertyFilter>().SingleOrDefault(f => f.Property == "IsSystemObject");
            Assert.NotNull(isSystemFilter);
            CollectionAssert.AreEqual(new object[] { 0 }, isSystemFilter.Values);
        }

        [Test]
        public void ServicesChildFactoryShouldFilterOutSystemObjects()
        {
            var factory = new ServicesChildFactory();
            var isSystemFilter = factory.Filters.OfType<NodePropertyFilter>().SingleOrDefault(f => f.Property == "IsSystemObject");
            Assert.NotNull(isSystemFilter);
            CollectionAssert.AreEqual(new object[] { 0 }, isSystemFilter.Values);
        }

        // CreateChild node types

        [Test]
        public void MessageTypesChildFactoryCreateChildShouldReturnMessageTypeNode()
        {
            var factory = new MessageTypesChildFactory();
            var parent = new FolderNode { NodeValue = "MessageTypes", NodeTypeId = NodeTypes.MessageTypes };
            var child = factory.CreateChild(parent, new Mock<NamedSmoObject>().Object);
            Assert.AreEqual("MessageType", child.NodeType);
        }

        [Test]
        public void SystemMessageTypesChildFactoryCreateChildShouldReturnSystemMessageTypeNode()
        {
            var factory = new SystemMessageTypesChildFactory();
            var parent = new FolderNode { NodeValue = "SystemMessageTypes", NodeTypeId = NodeTypes.SystemMessageTypes };
            var child = factory.CreateChild(parent, new Mock<NamedSmoObject>().Object);
            Assert.AreEqual("SystemMessageType", child.NodeType);
        }

        // ServiceBroker folder

        [Test]
        public void ServiceBrokerChildFactoryAppliesOnlyToServiceBrokerParent()
        {
            var factory = new ServiceBrokerChildFactory();
            CollectionAssert.AreEquivalent(
                new[] { nameof(NodeTypes.ServiceBroker) },
                factory.ApplicableParents());
        }

        [Test]
        public void ServiceBrokerChildFactoryShouldPopulateExpectedSubfolders()
        {
            var context = new SmoQueryContext(
                new Server(new ServerConnection(new SqlConnection(fakeConnectionString))),
                serviceProvider);
            context.ValidFor = ValidForFlag.AllOnPrem;

            var parent = new TestFolderNode(context)
            {
                NodeValue = Microsoft.SqlTools.SqlCore.SR.SchemaHierarchy_ServiceBroker,
                NodeTypeId = NodeTypes.ServiceBroker,
            };

            var factory = new ServiceBrokerChildFactory();
            var children = factory.Expand(parent, refresh: false, name: null, includeSystemObjects: true, CancellationToken.None).ToList();
            var folders = children.OfType<FolderNode>().ToList();

            Assert.That(folders.Any(f => f.NodeTypeId == NodeTypes.MessageTypes), "MessageTypes folder should be present");
            Assert.That(folders.Any(f => f.NodeTypeId == NodeTypes.Contracts), "Contracts folder should be present");
            Assert.That(folders.Any(f => f.NodeTypeId == NodeTypes.Queues), "Queues folder should be present");
            Assert.That(folders.Any(f => f.NodeTypeId == NodeTypes.Services), "Services folder should be present");
            Assert.That(folders.Any(f => f.NodeTypeId == NodeTypes.RemoteServiceBindings), "RemoteServiceBindings folder should be present");
        }

        // Helpers

        private SmoQueryContext BuildContextWithParent(SmoObjectBase parent)
        {
            var context = new SmoQueryContext(
                new Server(new ServerConnection(new SqlConnection(fakeConnectionString))),
                serviceProvider);
            context.Parent = parent;
            return context;
        }

        private sealed class TestFolderNode : TreeNode
        {
            private readonly SmoQueryContext context;

            internal TestFolderNode(SmoQueryContext context)
            {
                this.context = context;
            }

            public override object GetContext() => context;
        }
    }
}
