//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.Test.Utility;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Connection
{
    /// <summary>
    /// Tests for the ServiceHost Connection Service tests
    /// </summary>
    public class ConnectionServiceTests
    {
        /// <summary>
        /// Creates a mock db command that returns a predefined result set
        /// </summary>
        public static DbCommand CreateTestCommand(Dictionary<string, string>[][] data)
        {
            var commandMock = new Mock<DbCommand> { CallBase = true };
            var commandMockSetup = commandMock.Protected()
                .Setup<DbDataReader>("ExecuteDbDataReader", It.IsAny<CommandBehavior>());

            commandMockSetup.Returns(new TestDbDataReader(data));

            return commandMock.Object;
        }

        /// <summary>
        /// Creates a mock db connection that returns predefined data when queried for a result set
        /// </summary>
        public DbConnection CreateMockDbConnection(Dictionary<string, string>[][] data)
        {
            var connectionMock = new Mock<DbConnection> { CallBase = true };
            connectionMock.Protected()
                .Setup<DbCommand>("CreateDbCommand")
                .Returns(CreateTestCommand(data));

            return connectionMock.Object;
        }

        /// <summary>
        /// Verify that we can connect to the default database when no database name is
        /// provided as a parameter.
        /// </summary>
        [Theory]
        [InlineDataAttribute(null)]
        [InlineDataAttribute("")]
        public void CanConnectWithEmptyDatabaseName(string databaseName)
        {
            // Connect
            var connectionDetails = TestObjects.GetTestConnectionDetails();
            connectionDetails.DatabaseName = databaseName;
            var connectionResult =
                TestObjects.GetTestConnectionService()
                .Connect(new ConnectParams()
                {
                    OwnerUri = "file:///my/test/file.sql",
                    Connection = connectionDetails
                });
            
            // check that a connection was created
            Assert.NotEmpty(connectionResult.ConnectionId);
        }

        /// <summary>
        /// Verify that we can connect to the default database when no database name is
        /// provided as a parameter.
        /// </summary>
        [Theory]
        [InlineDataAttribute("master")]
        [InlineDataAttribute("nonMasterDb")]
        public void ConnectToDefaultDatabaseRespondsWithActualDbName(string expectedDbName)
        {
            // Given connecting with empty database name will return the expected DB name
            var connectionMock = new Mock<DbConnection> { CallBase = true };
            connectionMock.Setup(c => c.Database).Returns(expectedDbName);

            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>()))
                .Returns(connectionMock.Object);

            var connectionService = new ConnectionService(mockFactory.Object);

            // When I connect with an empty DB name
            var connectionDetails = TestObjects.GetTestConnectionDetails();
            connectionDetails.DatabaseName = string.Empty;

            var connectionResult =
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = "file:///my/test/file.sql",
                    Connection = connectionDetails
                });

            // Then I expect connection to succeed and the Summary to include the correct DB name
            Assert.NotEmpty(connectionResult.ConnectionId);
            Assert.NotNull(connectionResult.ConnectionSummary);
            Assert.Equal(expectedDbName, connectionResult.ConnectionSummary.DatabaseName);
        }

        /// <summary>
        /// Verify that when a connection is started for a URI with an already existing
        /// connection, we disconnect first before connecting.
        /// </summary>
        [Fact]
        public void ConnectingWhenConnectionExistCausesDisconnectThenConnect()
        {
            bool callbackInvoked = false;

            // first connect
            string ownerUri = "file://my/sample/file.sql";
            var connectionService = TestObjects.GetTestConnectionService();
            var connectionResult =
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = TestObjects.GetTestConnectionDetails()
                });

            // verify that we are connected
            Assert.NotEmpty(connectionResult.ConnectionId);

            // register disconnect callback
            connectionService.RegisterOnDisconnectTask(
                (result) => { 
                    callbackInvoked = true;
                    return Task.FromResult(true);
                }
            );

            // send annother connect request
            connectionResult =
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = TestObjects.GetTestConnectionDetails()
                });

            // verify that the event was fired (we disconnected first before connecting)
            Assert.True(callbackInvoked);

            // verify that we connected again
            Assert.NotEmpty(connectionResult.ConnectionId);
        }

        /// <summary>
        /// Verify that when connecting with invalid credentials, an error is thrown.
        /// </summary>
        [Fact]
        public void ConnectingWithInvalidCredentialsYieldsErrorMessage()
        {
            var testConnectionDetails = TestObjects.GetTestConnectionDetails();
            var invalidConnectionDetails = new ConnectionDetails();
            invalidConnectionDetails.ServerName = testConnectionDetails.ServerName;
            invalidConnectionDetails.DatabaseName = testConnectionDetails.DatabaseName;
            invalidConnectionDetails.UserName = "invalidUsername"; // triggers exception when opening mock connection
            invalidConnectionDetails.Password = "invalidPassword";

            // Connect to test db with invalid credentials
            var connectionResult =
                TestObjects.GetTestConnectionService()
                .Connect(new ConnectParams()
                {
                    OwnerUri = "file://my/sample/file.sql",
                    Connection = invalidConnectionDetails
                });

            // check that an error was caught
            Assert.NotNull(connectionResult.Messages);
            Assert.NotEqual(String.Empty, connectionResult.Messages);
        }

        /// <summary>
        /// Verify that when connecting with invalid parameters, an error is thrown.
        /// </summary>
        [Theory]
        [InlineData("SqlLogin", null, "my-server", "test", "sa", "123456")]
        [InlineData("SqlLogin", "file://my/sample/file.sql", null, "test", "sa", "123456")]
        [InlineData("SqlLogin", "file://my/sample/file.sql", "my-server", "test", null, "123456")]
        [InlineData("SqlLogin", "file://my/sample/file.sql", "my-server", "test", "sa", null)]
        [InlineData("SqlLogin", "", "my-server", "test", "sa", "123456")]
        [InlineData("SqlLogin", "file://my/sample/file.sql", "", "test", "sa", "123456")]
        [InlineData("SqlLogin", "file://my/sample/file.sql", "my-server", "test", "", "123456")]
        [InlineData("SqlLogin", "file://my/sample/file.sql", "my-server", "test", "sa", "")]
        [InlineData("Integrated", null, "my-server", "test", "sa", "123456")]
        [InlineData("Integrated", "file://my/sample/file.sql", null, "test", "sa", "123456")]
        [InlineData("Integrated", "", "my-server", "test", "sa", "123456")]
        [InlineData("Integrated", "file://my/sample/file.sql", "", "test", "sa", "123456")]
        public void ConnectingWithInvalidParametersYieldsErrorMessage(string authType, string ownerUri, string server, string database, string userName, string password)
        {
            // Connect with invalid parameters
            var connectionResult =
                TestObjects.GetTestConnectionService()
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = new ConnectionDetails() {
                        ServerName = server,
                        DatabaseName = database,
                        UserName = userName,
                        Password = password,
                        AuthenticationType = authType
                    }
                });
            
            // check that an error was caught
            Assert.NotNull(connectionResult.Messages);
            Assert.NotEqual(String.Empty, connectionResult.Messages);
        }

        /// <summary>
        /// Verify that when using integrated authentication, the username and/or password can be empty.
        /// </summary>
        [Theory]
        [InlineData(null, null)]
        [InlineData(null, "")]
        [InlineData("", null)]
        [InlineData("", "")]
        [InlineData("sa", null)]
        [InlineData("sa", "")]
        [InlineData(null, "12345678")]
        [InlineData("", "12345678")]
        public void ConnectingWithNoUsernameOrPasswordWorksForIntegratedAuth(string userName, string password)
        {
            // Connect
            var connectionResult =
                TestObjects.GetTestConnectionService()
                .Connect(new ConnectParams()
                {
                    OwnerUri = "file:///my/test/file.sql",
                    Connection = new ConnectionDetails() {
                        ServerName = "my-server",
                        DatabaseName = "test",
                        UserName = userName,
                        Password = password,
                        AuthenticationType = "Integrated"
                    }
                });
            
            // check that the connection was successful
            Assert.NotEmpty(connectionResult.ConnectionId);
        }

        /// <summary>
        /// Verify that when connecting with a null parameters object, an error is thrown.
        /// </summary>
        [Fact]
        public void ConnectingWithNullParametersObjectYieldsErrorMessage()
        {
            // Connect with null parameters
            var connectionResult =
                TestObjects.GetTestConnectionService()
                .Connect(null);
            
            // check that an error was caught
            Assert.NotNull(connectionResult.Messages);
            Assert.NotEqual(String.Empty, connectionResult.Messages);
        }

        /// <summary>
        /// Verify that optional parameters can be built into a connection string for connecting.
        /// </summary>
        [Theory]
        [InlineData("AuthenticationType", "Integrated", "Integrated Security")]
        [InlineData("AuthenticationType", "SqlLogin", "")]
        [InlineData("Encrypt", true, "Encrypt")]
        [InlineData("Encrypt", false, "Encrypt")]
        [InlineData("TrustServerCertificate", true, "TrustServerCertificate")]
        [InlineData("TrustServerCertificate", false, "TrustServerCertificate")]
        [InlineData("PersistSecurityInfo", true, "Persist Security Info")]
        [InlineData("PersistSecurityInfo", false, "Persist Security Info")]
        [InlineData("ConnectTimeout", 15, "Connect Timeout")]
        [InlineData("ConnectRetryCount", 1, "ConnectRetryCount")]
        [InlineData("ConnectRetryInterval", 10, "ConnectRetryInterval")]
        [InlineData("ApplicationName", "vscode-mssql", "Application Name")]
        [InlineData("WorkstationId", "mycomputer", "Workstation ID")]
        [InlineData("ApplicationIntent", "ReadWrite", "ApplicationIntent")]
        [InlineData("ApplicationIntent", "ReadOnly", "ApplicationIntent")]
        [InlineData("CurrentLanguage", "test", "Current Language")]
        [InlineData("Pooling", false, "Pooling")]
        [InlineData("Pooling", true, "Pooling")]
        [InlineData("MaxPoolSize", 100, "Max Pool Size")]
        [InlineData("MinPoolSize", 0, "Min Pool Size")]
        [InlineData("LoadBalanceTimeout", 0, "Load Balance Timeout")]
        [InlineData("Replication", true, "Replication")]
        [InlineData("Replication", false, "Replication")]
        [InlineData("AttachDbFilename", "myfile", "AttachDbFilename")]
        [InlineData("FailoverPartner", "partner", "Failover Partner")]
        [InlineData("MultiSubnetFailover", true, "MultiSubnetFailover")]
        [InlineData("MultiSubnetFailover", false, "MultiSubnetFailover")]
        [InlineData("MultipleActiveResultSets", false, "MultipleActiveResultSets")]
        [InlineData("MultipleActiveResultSets", true, "MultipleActiveResultSets")]
        [InlineData("PacketSize", 8192, "Packet Size")]
        [InlineData("TypeSystemVersion", "Latest", "Type System Version")]
        public void ConnectingWithOptionalParametersBuildsConnectionString(string propertyName, object propertyValue, string connectionStringMarker)
        {
            // Create a test connection details object and set the property to a specific value
            ConnectionDetails details = TestObjects.GetTestConnectionDetails();
            PropertyInfo info = details.GetType().GetProperty(propertyName);
            info.SetValue(details, propertyValue);

            // Test that a connection string can be created without exceptions
            string connectionString = ConnectionService.BuildConnectionString(details);
            Assert.NotNull(connectionString);
            Assert.NotEmpty(connectionString);

            // Verify that the parameter is in the connection string
            Assert.True(connectionString.Contains(connectionStringMarker));
        }

        /// <summary>
        /// Verify that a connection changed event is fired when the database context changes.
        /// </summary>
        [Fact]
        public void ConnectionChangedEventIsFiredWhenDatabaseContextChanges()
        {
            var serviceHostMock = new Mock<IProtocolEndpoint>();

            var connectionService = TestObjects.GetTestConnectionService();
            connectionService.ServiceHost = serviceHostMock.Object;

            // Set up an initial connection
            string ownerUri = "file://my/sample/file.sql";
            var connectionResult =
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = TestObjects.GetTestConnectionDetails()
                });

            // verify that a valid connection id was returned
            Assert.NotEmpty(connectionResult.ConnectionId);

            ConnectionInfo info;
            Assert.True(connectionService.TryFindConnection(ownerUri, out info));

            // Tell the connection manager that the database change ocurred
            connectionService.ChangeConnectionDatabaseContext(ownerUri, "myOtherDb");

            // Verify that the connection changed event was fired
            serviceHostMock.Verify(x => x.SendEvent<ConnectionChangedParams>(ConnectionChangedNotification.Type, It.IsAny<ConnectionChangedParams>()), Times.Once());
        }

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Fact]
        public void ConnectToDatabaseTest()
        {
            // connect to a database instance 
            string ownerUri = "file://my/sample/file.sql";
            var connectionResult =
                TestObjects.GetTestConnectionService()
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = TestObjects.GetTestConnectionDetails()
                });

            // verify that a valid connection id was returned
            Assert.NotEmpty(connectionResult.ConnectionId);
        }

        /// <summary>
        /// Verify that we can disconnect from an active connection succesfully
        /// </summary>
        [Fact]
        public void DisconnectFromDatabaseTest()
        {
            // first connect
            string ownerUri = "file://my/sample/file.sql";
            var connectionService = TestObjects.GetTestConnectionService();
            var connectionResult =
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = TestObjects.GetTestConnectionDetails()
                });

            // verify that we are connected
            Assert.NotEmpty(connectionResult.ConnectionId);

            // send disconnect request
            var disconnectResult =
                connectionService
                .Disconnect(new DisconnectParams()
                {
                    OwnerUri = ownerUri
                });
            Assert.True(disconnectResult);
        }

        /// <summary>
        /// Test that when a disconnect is performed, the callback event is fired
        /// </summary>
        [Fact]
        public void DisconnectFiresCallbackEvent()
        {
            bool callbackInvoked = false;

            // first connect
            string ownerUri = "file://my/sample/file.sql";
            var connectionService = TestObjects.GetTestConnectionService();
            var connectionResult =
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = TestObjects.GetTestConnectionDetails()
                });

            // verify that we are connected
            Assert.NotEmpty(connectionResult.ConnectionId);

            // register disconnect callback
            connectionService.RegisterOnDisconnectTask(
                (result) => { 
                    callbackInvoked = true;
                    return Task.FromResult(true);
                }
            );

            // send disconnect request
            var disconnectResult =
                connectionService
                .Disconnect(new DisconnectParams()
                {
                    OwnerUri = ownerUri
                });
            Assert.True(disconnectResult);

            // verify that the event was fired
            Assert.True(callbackInvoked);
        }

        /// <summary>
        /// Test that disconnecting an active connection removes the Owner URI -> ConnectionInfo mapping
        /// </summary>
        [Fact]
        public void DisconnectRemovesOwnerMapping()
        {
            // first connect
            string ownerUri = "file://my/sample/file.sql";
            var connectionService = TestObjects.GetTestConnectionService();
            var connectionResult =
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = TestObjects.GetTestConnectionDetails()
                });

            // verify that we are connected
            Assert.NotEmpty(connectionResult.ConnectionId);

            // check that the owner mapping exists
            ConnectionInfo info;
            Assert.True(connectionService.TryFindConnection(ownerUri, out info));

            // send disconnect request
            var disconnectResult =
                connectionService
                .Disconnect(new DisconnectParams()
                {
                    OwnerUri = ownerUri
                });
            Assert.True(disconnectResult);

            // check that the owner mapping no longer exists
            Assert.False(connectionService.TryFindConnection(ownerUri, out info));
        }

        /// <summary>
        /// Test that disconnecting validates parameters and doesn't succeed when they are invalid
        /// </summary>
        [Theory]
        [InlineDataAttribute(null)]
        [InlineDataAttribute("")]

        public void DisconnectValidatesParameters(string disconnectUri)
        {
            // first connect
            string ownerUri = "file://my/sample/file.sql";
            var connectionService = TestObjects.GetTestConnectionService();
            var connectionResult =
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = TestObjects.GetTestConnectionDetails()
                });

            // verify that we are connected
            Assert.NotEmpty(connectionResult.ConnectionId);

            // send disconnect request
            var disconnectResult =
                connectionService
                .Disconnect(new DisconnectParams()
                {
                    OwnerUri = disconnectUri
                });

            // verify that disconnect failed
            Assert.False(disconnectResult);
        }

        /// <summary>
        /// Verifies the the list databases operation lists database names for the server used by a connection.
        /// </summary>
        [Fact]
        public void ListDatabasesOnServerForCurrentConnectionReturnsDatabaseNames()
        {
            // Result set for the query of database names
            Dictionary<string, string>[] data =
            {
                new Dictionary<string, string> { {"name", "master" } },
                new Dictionary<string, string> { {"name", "model" } },
                new Dictionary<string, string> { {"name", "msdb" } },
                new Dictionary<string, string> { {"name", "tempdb" } },
                new Dictionary<string, string> { {"name", "mydatabase" } },
            };

            // Setup mock connection factory to inject query results
            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>()))
                .Returns(CreateMockDbConnection(new[] {data}));
            var connectionService = new ConnectionService(mockFactory.Object);

            // connect to a database instance 
            string ownerUri = "file://my/sample/file.sql";
            var connectionResult =
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = TestObjects.GetTestConnectionDetails()
                });

            // verify that a valid connection id was returned
            Assert.NotEmpty(connectionResult.ConnectionId);

            // list databases for the connection
            ListDatabasesParams parameters = new ListDatabasesParams();
            parameters.OwnerUri = ownerUri;
            var listDatabasesResult = connectionService.ListDatabases(parameters);
            string[] databaseNames = listDatabasesResult.DatabaseNames;

            Assert.Equal(databaseNames.Length, 5);
            Assert.Equal(databaseNames[0], "master");
            Assert.Equal(databaseNames[1], "model");
            Assert.Equal(databaseNames[2], "msdb");
            Assert.Equal(databaseNames[3], "tempdb");
            Assert.Equal(databaseNames[4], "mydatabase");
        }

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Fact]
        public void OnConnectionCallbackHandlerTest()
        {
            bool callbackInvoked = false;

            // setup connection service with callback
            var connectionService = TestObjects.GetTestConnectionService();
            connectionService.RegisterOnConnectionTask(
                (sqlConnection) => { 
                    callbackInvoked = true;
                    return Task.FromResult(true); 
                }
            );
            
            // connect to a database instance 
            var connectionResult = connectionService.Connect(TestObjects.GetTestConnectionParams());

            // verify that a valid connection id was returned
            Assert.True(callbackInvoked);
        }

        /// <summary>
        /// Verify when a connection is created that the URI -> Connection mapping is created in the connection service.
        /// </summary>
        [Fact]
        public void TestConnectRequestRegistersOwner()
        {
            // Given a request to connect to a database
            var service = TestObjects.GetTestConnectionService();
            var connectParams = TestObjects.GetTestConnectionParams();

            // connect to a database instance 
            var connectionResult = service.Connect(connectParams);

            // verify that a valid connection id was returned
            Assert.NotNull(connectionResult.ConnectionId);
            Assert.NotEqual(String.Empty, connectionResult.ConnectionId);
            Assert.NotNull(new Guid(connectionResult.ConnectionId));
            
            // verify that the (URI -> connection) mapping was created
            ConnectionInfo info;
            Assert.True(service.TryFindConnection(connectParams.OwnerUri, out info));
        }
    }
}
