//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    /// <summary>
    /// Verifies database metadata refreshes without modifying or querying the user's session.
    /// </summary>
    [TestFixture]
    public class DatabaseContextMetadataTests
    {
        private const string NewDatabase = "newDatabase";
        private Mock<DbConnection> defaultConnection;
        private Mock<ISqlConnectionFactory> factory;
        private Mock<IProtocolEndpoint> serviceHost;
        private Mock<ConnectionService> serviceMock;
        private ConnectionService service;
        private ConnectionInfo info;
        private ConnectionChangedParams notification;

        /// <summary>
        /// Creates a connected owner without opening a network connection.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            defaultConnection = new Mock<DbConnection>(MockBehavior.Strict);
            defaultConnection.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
            defaultConnection.SetupGet(connection => connection.State).Returns(ConnectionState.Open);
            defaultConnection.SetupGet(connection => connection.Database).Returns(NewDatabase);
            factory = new Mock<ISqlConnectionFactory>(MockBehavior.Strict);
            serviceHost = new Mock<IProtocolEndpoint>();
            notification = null;
            serviceHost.Setup(host => host.SendEvent(ConnectionChangedNotification.Type, It.IsAny<ConnectionChangedParams>()))
                .Callback<EventType<ConnectionChangedParams>, ConnectionChangedParams>((_, parameters) => notification = parameters)
                .Returns(Task.CompletedTask);
            serviceMock = new Mock<ConnectionService>(factory.Object) { CallBase = true };
            serviceMock.Setup(value => value.ReadServerInfo(It.IsAny<DbConnection>()))
                .Callback<DbConnection>(connection => Assert.That(connection.State, Is.EqualTo(ConnectionState.Open)))
                .Returns(new ServerInfo { DatabaseServiceTier = "Premium", DatabaseServiceObjective = "P2", ServerMajorVersion = 16 });
            service = serviceMock.Object;
            service.ServiceHost = serviceHost.Object;
            info = new ConnectionInfo(factory.Object, TestObjects.ScriptUri, TestObjects.GetTestConnectionDetails());
            info.AddConnection(ConnectionType.Default, defaultConnection.Object);
            service.OwnerToConnectionMap[TestObjects.ScriptUri] = info;
        }

        /// <summary>
        /// Metadata uses a disposable, non-enlisted connection to the updated database, not the query session.
        /// </summary>
        [Test]
        public void RefreshUsesSeparateConnection()
        {
            var metadataConnection = CreateMetadataConnection();
            string metadataConnectionString = null;
            factory.Setup(connectionFactory => connectionFactory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>(), null))
                .Callback<string, string, SqlRetryLogicBaseProvider>((connectionString, _, __) => metadataConnectionString = connectionString)
                .Returns(metadataConnection.Object);

            Assert.That(service.ChangeConnectionDatabaseContext(info.OwnerUri, NewDatabase), Is.True);

            var builder = new SqlConnectionStringBuilder(metadataConnectionString);
            Assert.That(builder.InitialCatalog, Is.EqualTo(NewDatabase));
            Assert.That(builder.Enlist, Is.False);
            Assert.That(info.CountConnections, Is.EqualTo(1));
            Assert.That(info.ConnectionDetails.DatabaseName, Is.EqualTo(NewDatabase));
            Assert.That(notification.Connection.DatabaseName, Is.EqualTo(NewDatabase));
            Assert.That(notification.ServerInfo.DatabaseServiceTier, Is.EqualTo("Premium"));
            Assert.That(notification.ServerInfo.DatabaseServiceObjective, Is.EqualTo("P2"));
            Assert.That(notification.ServerInfo.ServerMajorVersion, Is.EqualTo(16));
            metadataConnection.Verify(connection => connection.Open(), Times.Once());
            serviceMock.Verify(value => value.ReadServerInfo(metadataConnection.Object), Times.Once());
            serviceMock.Verify(value => value.ReadServerInfo(defaultConnection.Object), Times.Never());
            metadataConnection.Protected().Verify("Dispose", Times.Once(), true);
            defaultConnection.Protected().Verify<DbCommand>("CreateDbCommand", Times.Never());
        }

        /// <summary>
        /// Closed default connections retain the requested context without opening a metadata connection.
        /// </summary>
        [TestCase(ConnectionState.Closed)]
        [TestCase(ConnectionState.Broken)]
        public void InactiveDefaultConnectionSkipsRefresh(ConnectionState state)
        {
            defaultConnection.SetupGet(connection => connection.State).Returns(state);
            defaultConnection.SetupGet(connection => connection.Database).Returns("oldDatabase");

            Assert.That(service.ChangeConnectionDatabaseContext(info.OwnerUri, NewDatabase), Is.True);

            Assert.That(info.ConnectionDetails.DatabaseName, Is.EqualTo(NewDatabase));
            Assert.That(notification.Connection.DatabaseName, Is.EqualTo(NewDatabase));
            Assert.That(notification.ServerInfo, Is.Null);
            factory.Verify(connectionFactory => connectionFactory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>(), null), Times.Never());
        }

        /// <summary>
        /// Optional metadata failures do not undo a successful context change and always dispose the connection.
        /// </summary>
        [TestCase(true)]
        [TestCase(false)]
        public void RefreshFailurePreservesContext(bool failOnOpen)
        {
            var metadataConnection = CreateMetadataConnection();
            if (failOnOpen)
            {
                metadataConnection.Setup(connection => connection.Open()).Throws(new InvalidOperationException("Cannot connect"));
            }
            else
            {
                serviceMock.Setup(value => value.ReadServerInfo(metadataConnection.Object))
                    .Throws(new InvalidOperationException("Cannot read metadata"));
            }
            factory.Setup(connectionFactory => connectionFactory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>(), null))
                .Returns(metadataConnection.Object);

            Assert.That(service.ChangeConnectionDatabaseContext(info.OwnerUri, NewDatabase), Is.True);

            Assert.That(info.ConnectionDetails.DatabaseName, Is.EqualTo(NewDatabase));
            Assert.That(notification.Connection.DatabaseName, Is.EqualTo(NewDatabase));
            Assert.That(notification.ServerInfo, Is.Null);
            metadataConnection.Protected().Verify("Dispose", Times.Once(), true);
        }

        /// <summary>
        /// Callback-based authentication supplies a fresh token for the metadata connection's resource.
        /// </summary>
        [Test]
        public void RefreshUsesFreshAzureToken()
        {
            var metadataConnection = CreateMetadataConnection();
            info.ConnectionDetails.AuthenticationType = "AzureMFA";
            info.ConnectionDetails.AzureAccountToken = "old-token";
            info.AzureResourceUri = "https://database.windows.net/";
            string requestedResource = null;
            info.AzureTokenFetcher = resource =>
            {
                requestedResource = resource;
                return Task.FromResult(("fresh-token", DateTimeOffset.UtcNow.AddHours(1)));
            };
            factory.Setup(connectionFactory => connectionFactory.CreateSqlConnection(It.IsAny<string>(), "fresh-token", null))
                .Returns(metadataConnection.Object);

            Assert.That(service.ChangeConnectionDatabaseContext(info.OwnerUri, NewDatabase), Is.True);

            Assert.That(requestedResource, Is.EqualTo(info.AzureResourceUri));
            Assert.That(info.ConnectionDetails.AzureAccountToken, Is.EqualTo("old-token"));
            Assert.That(notification.ServerInfo.DatabaseServiceTier, Is.EqualTo("Premium"));
        }

        /// <summary>
        /// DAC sessions cannot open a second connection, so optional metadata is skipped.
        /// </summary>
        [Test]
        public void DedicatedAdminConnectionSkipsRefresh()
        {
            info.ConnectionDetails.ServerName = "admin:server";

            Assert.That(service.ChangeConnectionDatabaseContext(info.OwnerUri, NewDatabase), Is.True);

            Assert.That(notification.ServerInfo, Is.Null);
            factory.Verify(connectionFactory => connectionFactory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>(), null), Times.Never());
        }

        /// <summary>
        /// Server metadata preserves distinct non-null service properties and handles unsupported or absent columns.
        /// </summary>
        [TestCase("Premium", "P2", 9)]
        [TestCase(null, "P2", 9)]
        [TestCase("Premium", null, 9)]
        [TestCase(null, null, 9)]
        [TestCase(null, null, 7)]
        public void ServerVersionReadsDatabaseServiceProperties(string tier, string objective, int fieldCount)
        {
            using (var result = new DataTable())
            {
                for (int i = 0; i < fieldCount; i++)
                {
                    result.Columns.Add("Column" + i, typeof(object));
                }
                object[] row = { 5, "16.0.1000.0", "RTM", "SQL Azure", "machine", "server", 1,
                    (object)tier ?? DBNull.Value, (object)objective ?? DBNull.Value };
                Array.Resize(ref row, fieldCount);
                result.Rows.Add(row);
                using (var reader = result.CreateDataReader())
                {
                    var serverInfo = ReliableConnectionHelper.ReadServerInfo(reader);

                    Assert.That(serverInfo.DatabaseServiceTier, Is.EqualTo(tier));
                    Assert.That(serverInfo.DatabaseServiceObjective, Is.EqualTo(objective));
                    Assert.That(serverInfo.EngineEditionId, Is.EqualTo(5));
                    Assert.That(serverInfo.ServerMajorVersion, Is.EqualTo(16));
                }
            }
        }

        private static Mock<DbConnection> CreateMetadataConnection()
        {
            var connection = new Mock<DbConnection>();
            ConnectionState state = ConnectionState.Closed;
            connection.SetupGet(value => value.State).Returns(() => state);
            connection.Setup(value => value.Open()).Callback(() => state = ConnectionState.Open);
            return connection;
        }
    }
}
