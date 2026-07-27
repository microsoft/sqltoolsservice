//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;
using static Microsoft.SqlTools.ServiceLayer.ObjectExplorer.ObjectExplorerService;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectExplorer
{
    /// <summary>
    /// Integration tests for Service Broker items in the Object Explorer.
    /// These tests require a running SQL Server instance.
    /// </summary>
    public class ServiceBrokerIntegrationTests
    {
        private ObjectExplorerService _service = TestServiceProvider.Instance.ObjectExplorerService;

        /// <summary>
        /// Verifies that expanding the Service Broker node shows the expected subfolders.
        /// </summary>
        [Test]
        public async Task ServiceBrokerNodeShouldContainExpectedSubfolders()
        {
            await RunTest(null, string.Empty, "ServiceBroker", async (testDbName, session) =>
            {
                var dbNode = new NodeInfo(session.Root);
                var dbChildren = (await _service.ExpandNode(session, dbNode.NodePath)).Nodes;

                var serviceBrokerNode = dbChildren.FirstOrDefault(n => n.Label == Microsoft.SqlTools.SqlCore.SR.SchemaHierarchy_ServiceBroker);
                Assert.NotNull(serviceBrokerNode, "Service Broker node should be present under database");

                var sbChildren = (await _service.ExpandNode(session, serviceBrokerNode.NodePath)).Nodes;

                Assert.That(sbChildren.Any(n => n.Label == Microsoft.SqlTools.SqlCore.SR.SchemaHierarchy_MessageTypes),
                    "Message Types folder should be present under Service Broker");
                Assert.That(sbChildren.Any(n => n.Label == Microsoft.SqlTools.SqlCore.SR.SchemaHierarchy_Contracts),
                    "Contracts folder should be present under Service Broker");
                Assert.That(sbChildren.Any(n => n.Label == Microsoft.SqlTools.SqlCore.SR.SchemaHierarchy_Queues),
                    "Queues folder should be present under Service Broker");
                Assert.That(sbChildren.Any(n => n.Label == Microsoft.SqlTools.SqlCore.SR.SchemaHierarchy_Services),
                    "Services folder should be present under Service Broker");
            });
        }

        /// <summary>
        /// Verifies that the Message Types node has a System Message Types subfolder
        /// and that user-defined message types appear at the top level.
        /// </summary>
        [Test]
        public async Task MessageTypesShouldHaveSystemMessageTypesSubfolder()
        {
            string query = "IF NOT EXISTS (SELECT 1 FROM sys.service_message_types WHERE name = 'OETestMessageType') CREATE MESSAGE TYPE [OETestMessageType] VALIDATION = NONE";

            await RunTest("#testDb#", query, "ServiceBroker", async (testDbName, session) =>
            {
                var dbNode = new NodeInfo(session.Root);
                var dbChildren = (await _service.ExpandNode(session, dbNode.NodePath)).Nodes;
                var sbNode = dbChildren.FirstOrDefault(n => n.Label == Microsoft.SqlTools.SqlCore.SR.SchemaHierarchy_ServiceBroker);
                Assert.That(sbNode, Is.Not.Null, "Service Broker node should be present");

                var sbChildren = (await _service.ExpandNode(session, sbNode.NodePath)).Nodes;
                var msgTypesNode = sbChildren.FirstOrDefault(n => n.Label == Microsoft.SqlTools.SqlCore.SR.SchemaHierarchy_MessageTypes);
                Assert.That(msgTypesNode, Is.Not.Null, "Message Types node should be present");

                var msgChildren = (await _service.ExpandNode(session, msgTypesNode.NodePath)).Nodes;
                var userMsgType = msgChildren.FirstOrDefault(n => n.Label == "OETestMessageType");
                Assert.That(userMsgType, Is.Not.Null, "User-defined message type should appear at the top level of Message Types");

                await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, testDbName,
                    "DROP MESSAGE TYPE [OETestMessageType]");
            });
        }

        /// <summary>
        /// Verifies that system message types appear inside the System Message Types subfolder.
        /// </summary>
        [Test]
        public async Task SystemMessageTypesShouldAppearInSystemSubfolder()
        {
            await RunTest(null, string.Empty, "ServiceBroker", async (testDbName, session) =>
            {
                var dbNode = new NodeInfo(session.Root);
                var dbChildren = (await _service.ExpandNode(session, dbNode.NodePath)).Nodes;
                var sbNode = dbChildren.FirstOrDefault(n => n.Label == Microsoft.SqlTools.SqlCore.SR.SchemaHierarchy_ServiceBroker);
                Assert.That(sbNode, Is.Not.Null, "Service Broker node should be present");

                var sbChildren = (await _service.ExpandNode(session, sbNode.NodePath)).Nodes;
                var msgTypesNode = sbChildren.FirstOrDefault(n => n.Label == Microsoft.SqlTools.SqlCore.SR.SchemaHierarchy_MessageTypes);
                Assert.That(msgTypesNode, Is.Not.Null, "Message Types node should be present");
            });
        }

        private async Task RunTest(string databaseName, string query, string testDbPrefix, Func<string, ObjectExplorerSession, Task> test)
        {
            SqlTestDb testDb = null;
            string uri = string.Empty;
            try
            {
                testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, query, testDbPrefix);
                string dbToConnect = databaseName == "#testDb#" || databaseName == null ? testDb.DatabaseName : databaseName;
                var session = await CreateSession(dbToConnect);
                uri = session.Uri;
                await test(testDb.DatabaseName, session);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed Service Broker OE test. uri:{uri} error:{ex.Message}", ex);
            }
            finally
            {
                if (!string.IsNullOrEmpty(uri))
                {
                    _service.CloseSession(uri);
                }
                if (testDb != null)
                {
                    await testDb.CleanupAsync();
                }
            }
        }

        private async Task<ObjectExplorerSession> CreateSession(string databaseName)
        {
            var connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem, databaseName);
            var details = connectParams.Connection;
            string uri = Guid.NewGuid().ToString();
            return await _service.DoCreateSession(details, uri);
        }
    }
}
