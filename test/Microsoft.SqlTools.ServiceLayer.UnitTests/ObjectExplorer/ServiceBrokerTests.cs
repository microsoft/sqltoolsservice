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
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.SqlCore.ObjectExplorer;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.Nodes;
using Microsoft.SqlTools.SqlCore.ObjectExplorer.SmoModel;
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

        // ServiceBroker folder

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
