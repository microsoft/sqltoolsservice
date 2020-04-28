//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Moq.Protected;
using Xunit;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    /// <summary>
    /// Tests for the ServiceHost Connection Service tests
    /// </summary>
    public class ConnectionServiceTests
    {
        /// <summary>
        /// Creates a mock db command that returns a predefined result set
        /// </summary>
        public static DbCommand CreateTestCommand(TestResultSet[] data)
        {
            var commandMock = new Mock<DbCommand> { CallBase = true };
            var commandMockSetup = commandMock.Protected()
                .Setup<DbDataReader>("ExecuteDbDataReader", It.IsAny<CommandBehavior>());

            commandMockSetup.Returns(() => new TestDbDataReader(data, false));

            return commandMock.Object;
        }

        /// <summary>
        /// Creates a mock db connection that returns predefined data when queried for a result set
        /// </summary>
        public DbConnection CreateMockDbConnection(TestResultSet[] data)
        {
            var connectionMock = new Mock<DbConnection> { CallBase = true };
            connectionMock.Protected()
                .Setup<DbCommand>("CreateDbCommand")
                .Returns(CreateTestCommand(data));

            return connectionMock.Object;
        }

        [Fact]
        public void CanCancelConnectRequest()
        {
            const string testFile = "file:///my/test/file.sql";

            // Given a connection that times out and responds to cancellation
            var mockConnection = new Mock<DbConnection> { CallBase = true };
            CancellationToken token;
            bool ready = false;
            mockConnection.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Callback<CancellationToken>(t =>
                {
                    // Pass the token to the return handler and signal the main thread to cancel
                    token = t;
                    ready = true;
                })
                .Returns(() =>
                {
                    if (TestUtils.WaitFor(() => token.IsCancellationRequested))
                    {
                        throw new OperationCanceledException();
                    }
                    return Task.FromResult(true);
                });

            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockConnection.Object);


            var connectionService = new ConnectionService(mockFactory.Object);

            // Connect the connection asynchronously in a background thread
            var connectionDetails = TestObjects.GetTestConnectionDetails();
            var connectTask = Task.Run(async () => await connectionService
                .Connect(new ConnectParams
                {
                    OwnerUri = testFile,
                    Connection = connectionDetails
                }));

            // Wait for the connection to call OpenAsync()
            Assert.True(TestUtils.WaitFor(() => ready));

            // Send a cancellation request
            var cancelResult = connectionService
                .CancelConnect(new CancelConnectParams()
                {
                    OwnerUri = testFile
                });

            // Wait for the connection task to finish
            connectTask.Wait();

            // Verify that the connection was cancelled (no connection was created)
            Assert.Null(connectTask.Result.ConnectionId);

            // Verify that the cancel succeeded
            Assert.True(cancelResult);
        }

        [Fact]
        public async Task CanCancelConnectRequestByConnecting()
        {
            const string testFile = "file:///my/test/file.sql";

            // Given a connection that times out and responds to cancellation
            var mockConnection = new Mock<DbConnection> { CallBase = true };
            CancellationToken token;
            bool ready = false;
            mockConnection.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Callback<CancellationToken>(t =>
                {
                    // Pass the token to the return handler and signal the main thread to cancel
                    token = t;
                    ready = true;
                })
                .Returns(() =>
                {
                    if (TestUtils.WaitFor(() => token.IsCancellationRequested))
                    {
                        throw new OperationCanceledException();
                    }
                    return Task.FromResult(true);
                });

            // Given a second connection that succeeds
            var mockConnection2 = new Mock<DbConnection> { CallBase = true };
            mockConnection2.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.Run(() => { }));

            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.SetupSequence(factory => factory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockConnection.Object)
                .Returns(mockConnection2.Object);


            var connectionService = new ConnectionService(mockFactory.Object);

            // Connect the first connection asynchronously in a background thread
            var connectionDetails = TestObjects.GetTestConnectionDetails();
            var connectTask = Task.Run(async () => await connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = testFile,
                    Connection = connectionDetails
                }));

            // Wait for the connection to call OpenAsync()
            Assert.True(TestUtils.WaitFor(() => ready));

            // Send a cancellation by trying to connect again
            var connectResult = await connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = testFile,
                    Connection = connectionDetails
                });

            // Wait for the first connection task to finish
            connectTask.Wait();

            // Verify that the first connection was cancelled (no connection was created)
            Assert.Null(connectTask.Result.ConnectionId);

            // Verify that the second connection succeeded
            Assert.NotEmpty(connectResult.ConnectionId);
        }

        [Fact]
        public void CanCancelConnectRequestByDisconnecting()
        {
            const string testFile = "file:///my/test/file.sql";

            // Given a connection that times out and responds to cancellation
            var mockConnection = new Mock<DbConnection> { CallBase = true };
            CancellationToken token;
            bool ready = false;
            mockConnection.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Callback<CancellationToken>(t =>
                {
                    // Pass the token to the return handler and signal the main thread to cancel
                    token = t;
                    ready = true;
                })
                .Returns(() =>
                {
                    if (TestUtils.WaitFor(() => token.IsCancellationRequested))
                    {
                        throw new OperationCanceledException();
                    }
                    return Task.FromResult(true);
                });

            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockConnection.Object);


            var connectionService = new ConnectionService(mockFactory.Object);

            // Connect the first connection asynchronously in a background thread
            var connectionDetails = TestObjects.GetTestConnectionDetails();
            var connectTask = Task.Run(async () => await connectionService
                .Connect(new ConnectParams
                {
                    OwnerUri = testFile,
                    Connection = connectionDetails
                }));

            // Wait for the connection to call OpenAsync()
            Assert.True(TestUtils.WaitFor(() => ready));

            // Send a cancellation by trying to disconnect
            var disconnectResult = connectionService
                .Disconnect(new DisconnectParams
                {
                    OwnerUri = testFile
                });

            // Wait for the first connection task to finish
            connectTask.Wait();

            // Verify that the first connection was cancelled (no connection was created)
            Assert.Null(connectTask.Result.ConnectionId);

            // Verify that the disconnect failed (since it caused a cancellation)
            Assert.False(disconnectResult);
        }

        /// <summary>
        /// Verify that we can connect to the default database when no database name is
        /// provided as a parameter.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task CanConnectWithEmptyDatabaseName(string databaseName)
        {
            // Connect
            var connectionDetails = TestObjects.GetTestConnectionDetails();
            connectionDetails.DatabaseName = databaseName;
            var connectionResult = await
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
        [InlineData("master")]
        [InlineData("nonMasterDb")]
        public async Task ConnectToDefaultDatabaseRespondsWithActualDbName(string expectedDbName)
        {
            // Given connecting with empty database name will return the expected DB name
            var connectionMock = new Mock<DbConnection> { CallBase = true };
            connectionMock.Setup(c => c.Database).Returns(expectedDbName);

            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(connectionMock.Object);

            var connectionService = new ConnectionService(mockFactory.Object);

            // When I connect with an empty DB name
            var connectionDetails = TestObjects.GetTestConnectionDetails();
            connectionDetails.DatabaseName = string.Empty;

            var connectionResult = await
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
        public async Task ConnectingWhenConnectionExistCausesDisconnectThenConnect()
        {
            bool callbackInvoked = false;

            string ownerUri = "file://my/sample/file.sql";
            const string masterDbName = "master";
            const string otherDbName = "other";
            // Given a connection that returns the database name
            var dummySqlConnection = new TestSqlConnection(null);

            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string connString, string azureAccountToken) =>
            {
                dummySqlConnection.ConnectionString = connString;
                SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(connString);

                // Database name is respected. Follow heuristic where empty DB name really means Master
                var dbName = string.IsNullOrEmpty(scsb.InitialCatalog) ? masterDbName : scsb.InitialCatalog;
                dummySqlConnection.SetDatabase(dbName);
                return dummySqlConnection;
            });

            var connectionService = new ConnectionService(mockFactory.Object);

            // register disconnect callback
            connectionService.RegisterOnDisconnectTask(
                (result, uri) =>
                {
                    callbackInvoked = true;
                    Assert.True(uri.Equals(ownerUri));
                    return Task.FromResult(true);
                }
            );

            // When I connect to default
            var connectionResult = await
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = TestObjects.GetTestConnectionDetails()
                });

            // Then I expect to be connected to master
            Assert.NotEmpty(connectionResult.ConnectionId);

            // And when I then connect to another DB
            var updatedConnectionDetails = TestObjects.GetTestConnectionDetails();
            updatedConnectionDetails.DatabaseName = otherDbName;
            connectionResult = await
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = updatedConnectionDetails
                });

            // Then I expect to be disconnected from master, and connected to the new DB
            // verify that the event was fired (we disconnected first before connecting)
            Assert.True(callbackInvoked);

            // verify that we connected again
            Assert.NotEmpty(connectionResult.ConnectionId);
            Assert.Equal(otherDbName, connectionResult.ConnectionSummary.DatabaseName);
        }

        /// <summary>
        /// Verify that when connecting with invalid credentials, an error is thrown.
        /// </summary>
        [Fact]
        public async Task ConnectingWithInvalidCredentialsYieldsErrorMessage()
        {
            var testConnectionDetails = TestObjects.GetTestConnectionDetails();
            var invalidConnectionDetails = new ConnectionDetails
            {
                ServerName = testConnectionDetails.ServerName,
                DatabaseName = testConnectionDetails.DatabaseName,
                UserName = "invalidUsername",
                Password = Guid.NewGuid().ToString()
            };
            // triggers exception when opening mock connection

            // Connect to test db with invalid credentials
            var connectionResult = await
                TestObjects.GetTestConnectionService()
                .Connect(new ConnectParams
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
        public async Task ConnectingWithInvalidParametersYieldsErrorMessage(string authType, string ownerUri, string server, string database, string userName, string password)
        {
            // Connect with invalid parameters
            var connectionResult = await
                TestObjects.GetTestConnectionService()
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = new ConnectionDetails()
                    {
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
        public async Task ConnectingWithNoUsernameOrPasswordWorksForIntegratedAuth(string userName, string password)
        {
            // Connect
            var connectionResult = await
                TestObjects.GetTestConnectionService()
                .Connect(new ConnectParams()
                {
                    OwnerUri = "file:///my/test/file.sql",
                    Connection = new ConnectionDetails()
                    {
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
        public async Task ConnectingWithNullParametersObjectYieldsErrorMessage()
        {
            // Connect with null parameters
            var connectionResult = await
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
        [InlineData("ColumnEncryptionSetting", "Enabled", "Column Encryption Setting=Enabled")]
        [InlineData("ColumnEncryptionSetting", "Disabled", "Column Encryption Setting=Disabled")]
        [InlineData("ColumnEncryptionSetting", "enabled", "Column Encryption Setting=Enabled")]
        [InlineData("ColumnEncryptionSetting", "disabled", "Column Encryption Setting=Disabled")]
        [InlineData("ColumnEncryptionSetting", "ENABLED", "Column Encryption Setting=Enabled")]
        [InlineData("ColumnEncryptionSetting", "DISABLED", "Column Encryption Setting=Disabled")]
        [InlineData("ColumnEncryptionSetting", "eNaBlEd", "Column Encryption Setting=Enabled")]
        [InlineData("ColumnEncryptionSetting", "DiSaBlEd", "Column Encryption Setting=Disabled")]
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
        /// Verify that optional parameters which require ColumnEncryptionSetting to be enabled
        /// can be built into a connection string for connecting.
        /// </summary>
        [Theory]
        [InlineData("EnclaveAttestationProtocol", "AAS", "Attestation Protocol=AAS")]
        [InlineData("EnclaveAttestationProtocol", "HGS", "Attestation Protocol=HGS")]
        [InlineData("EnclaveAttestationProtocol", "aas", "Attestation Protocol=AAS")]
        [InlineData("EnclaveAttestationProtocol", "hgs", "Attestation Protocol=HGS")]
        [InlineData("EnclaveAttestationProtocol", "AaS", "Attestation Protocol=AAS")]
        [InlineData("EnclaveAttestationProtocol", "hGs", "Attestation Protocol=HGS")]
        [InlineData("EnclaveAttestationUrl", "https://attestation.us.attest.azure.net/attest/SgxEnclave", "Enclave Attestation Url=https://attestation.us.attest.azure.net/attest/SgxEnclave")]
        public void ConnectingWithOptionalEnclaveParametersBuildsConnectionString(string propertyName, object propertyValue, string connectionStringMarker)
        {
            // Create a test connection details object and set the property to a specific value
            ConnectionDetails details = TestObjects.GetTestConnectionDetails();
            details.GetType()
                .GetProperty("ColumnEncryptionSetting")
                .SetValue(details, "Enabled");
            details.GetType()
                .GetProperty(propertyName)
                .SetValue(details, propertyValue);

            // Test that a connection string can be created without exceptions
            string connectionString = ConnectionService.BuildConnectionString(details);
            Assert.NotNull(connectionString);
            Assert.NotEmpty(connectionString);

            // Verify that the parameter is in the connection string
            Assert.True(connectionString.Contains(connectionStringMarker));
        }

        /// <summary>
        /// Build connection string with an invalid property type
        /// </summary>
        [Theory]
        [InlineData("AuthenticationType", "NotAValidAuthType")]
        [InlineData("ColumnEncryptionSetting", "NotAValidColumnEncryptionSetting")]
        [InlineData("EnclaveAttestationProtocol", "NotAValidEnclaveAttestationProtocol")]
        public void BuildConnectionStringWithInvalidOptions(string propertyName, object propertyValue)
        {
            ConnectionDetails details = TestObjects.GetTestConnectionDetails();
            PropertyInfo info = details.GetType().GetProperty(propertyName);
            info.SetValue(details, propertyValue);
            Assert.Throws<ArgumentException>(() => ConnectionService.BuildConnectionString(details));
        }

        /// <summary>
        /// Parameters used for test: BuildConnectionStringWithInvalidOptionCombinations
        /// </summary>
        public static readonly object[][] ConnectionStringWithInvalidOptionCombinations =
        {
            new object[]
            {
                typeof(ArgumentException),
                new []
                {
                    Tuple.Create<string, object>("ColumnEncryptionSetting", null),
                    Tuple.Create<string, object>("EnclaveAttestationProtocol", "AAS"),
                    Tuple.Create<string, object>("EnclaveAttestationUrl", "https://attestation.us.attest.azure.net/attest/SgxEnclave")
                }
            },
            new object[]
            {
                typeof(ArgumentException),
                new []
                {
                    Tuple.Create<string, object>("ColumnEncryptionSetting", "Disabled"),
                    Tuple.Create<string, object>("EnclaveAttestationProtocol", "AAS"),
                    Tuple.Create<string, object>("EnclaveAttestationUrl", "https://attestation.us.attest.azure.net/attest/SgxEnclave")
                }
            },
            new object[]
            {
                typeof(ArgumentException),
                new []
                {
                    Tuple.Create<string, object>("ColumnEncryptionSetting", ""),
                    Tuple.Create<string, object>("EnclaveAttestationProtocol", "AAS"),
                    Tuple.Create<string, object>("EnclaveAttestationUrl", "https://attestation.us.attest.azure.net/attest/SgxEnclave")
                }
            }
        };

        /// <summary>
        /// Build connection string with an invalid property combinations
        /// </summary>
        [Theory]
        [MemberData(nameof(ConnectionStringWithInvalidOptionCombinations))]
        public void BuildConnectionStringWithInvalidOptionCombinations(Type exceptionType, Tuple<string, object>[] propertyNameValuePairs)
        {
            ConnectionDetails details = TestObjects.GetTestConnectionDetails();
            propertyNameValuePairs.ToList().ForEach(tuple =>
            {
                PropertyInfo info = details.GetType().GetProperty(tuple.Item1);
                info.SetValue(details, tuple.Item2);
            });
            Assert.Throws(exceptionType, () => ConnectionService.BuildConnectionString(details));
        }

        /// <summary>
        /// Verify that a connection changed event is fired when the database context changes.
        /// </summary>
        [Fact]
        public async Task ConnectionChangedEventIsFiredWhenDatabaseContextChanges()
        {
            var serviceHostMock = new Mock<IProtocolEndpoint>();

            var connectionService = TestObjects.GetTestConnectionService();
            connectionService.ServiceHost = serviceHostMock.Object;

            // Set up an initial connection
            const string ownerUri = "file://my/sample/file.sql";
            var connectionResult = await
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

            // Tell the connection manager that the database change occurred
            connectionService.ChangeConnectionDatabaseContext(ownerUri, "myOtherDb");

            // Verify that the connection changed event was fired
            serviceHostMock.Verify(x => x.SendEvent(ConnectionChangedNotification.Type, It.IsAny<ConnectionChangedParams>()), Times.Once());
        }

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Fact]
        public async Task ConnectToDatabaseTest()
        {
            // connect to a database instance
            string ownerUri = "file://my/sample/file.sql";
            var connectionResult = await
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
        /// Verify that we can disconnect from an active connection successfully
        /// </summary>
        [Fact]
        public async Task DisconnectFromDatabaseTest()
        {
            // first connect
            string ownerUri = "file://my/sample/file.sql";
            var connectionService = TestObjects.GetTestConnectionService();
            var connectionResult = await
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
        public async Task DisconnectFiresCallbackEvent()
        {
            bool callbackInvoked = false;

            // first connect
            string ownerUri = "file://my/sample/file.sql";
            var connectionService = TestObjects.GetTestConnectionService();
            var connectionResult = await
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
                (result, uri) =>
                {
                    callbackInvoked = true;
                    Assert.True(uri.Equals(ownerUri));
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
        public async Task DisconnectRemovesOwnerMapping()
        {
            // first connect
            string ownerUri = "file://my/sample/file.sql";
            var connectionService = TestObjects.GetTestConnectionService();
            var connectionResult = await
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
        [InlineData(null)]
        [InlineData("")]

        public async Task DisconnectValidatesParameters(string disconnectUri)
        {
            // first connect
            string ownerUri = "file://my/sample/file.sql";
            var connectionService = TestObjects.GetTestConnectionService();
            var connectionResult = await
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

        private async Task<ListDatabasesResponse> RunListDatabasesRequestHandler(TestResultSet testdata, bool? includeDetails)
        {
            // Setup mock connection factory to inject query results
            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(CreateMockDbConnection(new[] { testdata }));
            var connectionService = new ConnectionService(mockFactory.Object);

            // connect to a database instance
            string ownerUri = "file://my/sample/file.sql";
            var connectionResult = await
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = TestObjects.GetTestConnectionDetails()
                });

            // verify that a valid connection id was returned
            Assert.NotEmpty(connectionResult.ConnectionId);

            // list databases for the connection
            ListDatabasesParams parameters = new ListDatabasesParams
            {
                OwnerUri = ownerUri,
                IncludeDetails = includeDetails
            };
            return connectionService.ListDatabases(parameters);
        }

        /// <summary>
        /// Verifies the the list databases operation lists database names for the server used by a connection.
        /// </summary>
        [Fact]
        public async Task ListDatabasesOnServerForCurrentConnectionReturnsDatabaseNames()
        {
            // Result set for the query of database names
            TestDbColumn[] cols = { new TestDbColumn("name") };
            object[][] rows =
            {
                new object[] {"mydatabase"}, // this should be sorted to the end in the response
                new object[] {"master"},
                new object[] {"model"},
                new object[] {"msdb"},
                new object[] {"tempdb"}
            };
            TestResultSet data = new TestResultSet(cols, rows);
            var response = await RunListDatabasesRequestHandler(testdata: data, includeDetails: null);

            string[] databaseNames = response.DatabaseNames;

            Assert.Equal(databaseNames.Length, 5);
            Assert.Equal(databaseNames[0], "master");
            Assert.Equal(databaseNames[1], "model");
            Assert.Equal(databaseNames[2], "msdb");
            Assert.Equal(databaseNames[3], "tempdb");
            Assert.Equal(databaseNames[4], "mydatabase");
        }

        /// <summary>
        /// Verifies the the list databases operation lists database names for the server used by a connection.
        /// </summary>
        [Fact]
        public async Task ListDatabasesOnServerForCurrentConnectionReturnsDatabaseDetails()
        {
            // Result set for the query of database names
            TestDbColumn[] cols = {
                new TestDbColumn("name"),
                new TestDbColumn("state"),
                new TestDbColumn("size"),
                new TestDbColumn("last_backup")
             };
            object[][] rows =
            {
                new object[] {"mydatabase", "Online", "10", "2010-01-01 11:11:11"}, // this should be sorted to the end in the response
                new object[] {"master", "Online", "11", "2010-01-01 11:11:12"},
                new object[] {"model", "Offline", "12", "2010-01-01 11:11:13"},
                new object[] {"msdb", "Online", "13", "2010-01-01 11:11:14"},
                new object[] {"tempdb", "Online", "14", "2010-01-01 11:11:15"}
            };
            TestResultSet data = new TestResultSet(cols, rows);
            var response = await RunListDatabasesRequestHandler(testdata: data, includeDetails: true);

            Assert.Equal(response.Databases.Length, 5);
            VerifyDatabaseDetail(rows[0], response.Databases[4]);
            VerifyDatabaseDetail(rows[1], response.Databases[0]);
            VerifyDatabaseDetail(rows[2], response.Databases[1]);
            VerifyDatabaseDetail(rows[3], response.Databases[2]);
            VerifyDatabaseDetail(rows[4], response.Databases[3]);
        }

        private void VerifyDatabaseDetail(object[] expected, DatabaseInfo actual)
        {
            Assert.Equal(expected[0], actual.Options[ListDatabasesRequestDatabaseProperties.Name]);
            Assert.Equal(expected[1], actual.Options[ListDatabasesRequestDatabaseProperties.State]);
            Assert.Equal(expected[2], actual.Options[ListDatabasesRequestDatabaseProperties.SizeInMB]);
            Assert.Equal(expected[3], actual.Options[ListDatabasesRequestDatabaseProperties.LastBackup]);
        }


        /// <summary>
        /// Verify that the factory is returnning DatabaseNamesHandler
        /// </summary>
        [Fact]
        public void ListDatabaseRequestFactoryReturnsDatabaseNamesHandler()
        {
            var handler = ListDatabaseRequestHandlerFactory.getHandler(includeDetails: false, isSqlDB: true);
            Assert.IsType(typeof(DatabaseNamesHandler), handler);
            handler = ListDatabaseRequestHandlerFactory.getHandler(includeDetails: false, isSqlDB: false);
            Assert.IsType(typeof(DatabaseNamesHandler), handler);
        }

        /// <summary>
        /// Verify that the factory is returnning SqlDBDatabaseDetailHandler
        /// </summary>
        [Fact]
        public void ListDatabaseRequestFactoryReturnsSqlDBHandler()
        {
            var handler = ListDatabaseRequestHandlerFactory.getHandler(includeDetails: true, isSqlDB: true);
            Assert.IsType(typeof(SqlDBDatabaseDetailHandler), handler);
        }

        /// <summary>
        /// Verify that the factory is returnning SqlServerDatabaseDetailHandler
        /// </summary>
        [Fact]
        public void ListDatabaseRequestFactoryReturnsSqlServerHandler()
        {
            var handler = ListDatabaseRequestHandlerFactory.getHandler(includeDetails: true, isSqlDB: false);
            Assert.IsType(typeof(SqlServerDatabaseDetailHandler), handler);
        }

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Fact]
        public async Task OnConnectionCallbackHandlerTest()
        {
            bool callbackInvoked = false;

            // setup connection service with callback
            var connectionService = TestObjects.GetTestConnectionService();
            connectionService.RegisterOnConnectionTask(
                (sqlConnection) =>
                {
                    callbackInvoked = true;
                    return Task.FromResult(true);
                }
            );

            // connect to a database instance
            await connectionService.Connect(TestObjects.GetTestConnectionParams());

            // verify that a valid connection id was returned
            Assert.True(callbackInvoked);
        }

        /// <summary>
        /// Test ConnectionSummaryComparer
        /// </summary>
        [Fact]
        public void TestConnectionSummaryComparer()
        {
            var summary1 = new ConnectionSummary()
            {
                ServerName = "localhost",
                DatabaseName = "master",
                UserName = "user"
            };

            var summary2 = new ConnectionSummary()
            {
                ServerName = "localhost",
                DatabaseName = "master",
                UserName = "user"
            };

            var comparer = new ConnectionSummaryComparer();
            Assert.True(comparer.Equals(summary1, summary2));

            summary2.DatabaseName = "tempdb";
            Assert.False(comparer.Equals(summary1, summary2));
            Assert.False(comparer.Equals(null, summary2));

            Assert.False(summary1.GetHashCode() == summary2.GetHashCode());
        }

        /// <summary>
        /// Verify when a connection is created that the URI -> Connection mapping is created in the connection service.
        /// </summary>
        [Fact]
        public async Task TestConnectRequestRegistersOwner()
        {
            // Given a request to connect to a database
            var service = TestObjects.GetTestConnectionService();
            var connectParams = TestObjects.GetTestConnectionParams();

            // connect to a database instance
            var connectionResult = await service.Connect(connectParams);

            // verify that a valid connection id was returned
            Assert.NotNull(connectionResult.ConnectionId);
            Assert.NotEqual(string.Empty, connectionResult.ConnectionId);
            Assert.NotNull(new Guid(connectionResult.ConnectionId));

            // verify that the (URI -> connection) mapping was created
            ConnectionInfo info;
            Assert.True(service.TryFindConnection(connectParams.OwnerUri, out info));
        }

        /// <summary>
        /// Verify that Linux/OSX SqlExceptions thrown do not contain an error code.
        /// This is a bug in .NET core (see https://github.com/dotnet/corefx/issues/12472).
        /// If this test ever fails, it means that this bug has been fixed. When this is
        /// the case, look at RetryPolicyUtils.cs in IsRetryableNetworkConnectivityError(),
        /// and remove the code block specific to Linux/OSX.
        /// </summary>
        [Fact]
        public void TestThatLinuxAndOsxSqlExceptionHasNoErrorCode()
        {
            RunIfWrapper.RunIfLinuxOrOSX(() =>
            {
                try
                {
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
                    {
                        DataSource = "bad-server-name",
                        UserID = "sa",
                        Password = "bad password"
                    };

                    SqlConnection connection = new SqlConnection(builder.ConnectionString);
                    connection.Open(); // This should fail
                }
                catch (SqlException ex)
                {
                    // Error code should be 0 due to bug
                    Assert.Equal(ex.Number, 0);
                }
            });
        }

        /// <summary>
        /// Test that cancel connection with a null connection parameter
        /// </summary>
        [Fact]
        public void TestCancelConnectionNullParam()
        {
            var service = TestObjects.GetTestConnectionService();
            Assert.False(service.CancelConnect(null));
        }

        /// <summary>
        /// Test that cancel connection with a null connection parameter
        /// </summary>
        [Fact]
        public void TestListDatabasesInvalidParams()
        {
            var service = TestObjects.GetTestConnectionService();
            var listParams = new ListDatabasesParams();
            Assert.Throws<ArgumentException>(() => service.ListDatabases(listParams));
            listParams.OwnerUri = "file://notmyfile.sql";
            Assert.Throws<Exception>(() => service.ListDatabases(listParams));
        }

        /// <summary>
        /// Test that the connection complete notification type can be created.
        /// </summary>
        [Fact]
        public void TestConnectionCompleteNotificationIsCreated()
        {
            Assert.NotNull(ConnectionCompleteNotification.Type);
        }

        /// <summary>
        /// Test that the connection summary comparer creates a hash code correctly
        /// </summary>
        [Theory]
        [InlineData(true, null, null, null)]
        [InlineData(false, null, null, null)]
        [InlineData(false, null, null, "sa")]
        [InlineData(false, null, "test", null)]
        [InlineData(false, null, "test", "sa")]
        [InlineData(false, "server", null, null)]
        [InlineData(false, "server", null, "sa")]
        [InlineData(false, "server", "test", null)]
        [InlineData(false, "server", "test", "sa")]
        public void TestConnectionSummaryComparerHashCode(bool objectNull, string serverName, string databaseName, string userName)
        {
            // Given a connection summary and comparer object
            ConnectionSummary summary = null;
            if (!objectNull)
            {
                summary = new ConnectionSummary()
                {
                    ServerName = serverName,
                    DatabaseName = databaseName,
                    UserName = userName
                };
            }
            ConnectionSummaryComparer comparer = new ConnectionSummaryComparer();

            // If I compute a hash code
            int hashCode = comparer.GetHashCode(summary);
            if (summary == null || (serverName == null && databaseName == null && userName == null))
            {
                // Then I expect it to be 31 for a null summary
                Assert.Equal(31, hashCode);
            }
            else
            {
                // And not 31 otherwise
                Assert.NotEqual(31, hashCode);
            }
        }

        [Fact]
        public void ConnectParamsAreInvalidIfConnectionIsNull()
        {
            // Given connection parameters where the connection property is null
            ConnectParams parameters = new ConnectParams
            {
                OwnerUri = "my/sql/file.sql",
                Connection = null
            };

            string errorMessage;

            // If I check if the parameters are valid
            Assert.False(parameters.IsValid(out errorMessage));

            // Then I expect an error message
            Assert.NotNull(errorMessage);
            Assert.NotEmpty(errorMessage);
        }

        [Fact]
        public async void ConnectingTwiceWithTheSameUriDoesNotCreateAnotherDbConnection()
        {
            // Setup the connect and disconnect params
            var connectParamsSame1 = new ConnectParams()
            {
                OwnerUri = "connectParamsSame",
                Connection = TestObjects.GetTestConnectionDetails()
            };
            var connectParamsSame2 = new ConnectParams()
            {
                OwnerUri = "connectParamsSame",
                Connection = TestObjects.GetTestConnectionDetails()
            };
            var disconnectParamsSame = new DisconnectParams()
            {
                OwnerUri = connectParamsSame1.OwnerUri
            };
            var connectParamsDifferent = new ConnectParams()
            {
                OwnerUri = "connectParamsDifferent",
                Connection = TestObjects.GetTestConnectionDetails()
            };
            var disconnectParamsDifferent = new DisconnectParams()
            {
                OwnerUri = connectParamsDifferent.OwnerUri
            };

            // Given a request to connect to a database, there should be no initial connections in the map
            var service = TestObjects.GetTestConnectionService();
            Dictionary<string, ConnectionInfo> ownerToConnectionMap = service.OwnerToConnectionMap;
            Assert.Equal(0, ownerToConnectionMap.Count);

            // If we connect to the service, there should be 1 connection
            await service.Connect(connectParamsSame1);
            Assert.Equal(1, ownerToConnectionMap.Count);

            // If we connect again with the same URI, there should still be 1 connection
            await service.Connect(connectParamsSame2);
            Assert.Equal(1, ownerToConnectionMap.Count);

            // If we connect with a different URI, there should be 2 connections
            await service.Connect(connectParamsDifferent);
            Assert.Equal(2, ownerToConnectionMap.Count);

            // If we disconnect with the unique URI, there should be 1 connection
            service.Disconnect(disconnectParamsDifferent);
            Assert.Equal(1, ownerToConnectionMap.Count);

            // If we disconnect with the duplicate URI, there should be 0 connections
            service.Disconnect(disconnectParamsSame);
            Assert.Equal(0, ownerToConnectionMap.Count);
        }

        [Fact]
        public async void DbConnectionDoesntLeakUponDisconnect()
        {
            // If we connect with a single URI and 2 connection types
            var connectParamsDefault = new ConnectParams()
            {
                OwnerUri = "connectParams",
                Connection = TestObjects.GetTestConnectionDetails(),
                Type = ConnectionType.Default
            };
            var connectParamsQuery = new ConnectParams()
            {
                OwnerUri = "connectParams",
                Connection = TestObjects.GetTestConnectionDetails(),
                Type = ConnectionType.Query
            };
            var disconnectParams = new DisconnectParams()
            {
                OwnerUri = connectParamsDefault.OwnerUri
            };
            var service = TestObjects.GetTestConnectionService();
            await service.Connect(connectParamsDefault);
            await service.Connect(connectParamsQuery);

            // We should have one ConnectionInfo and 2 DbConnections
            ConnectionInfo connectionInfo = service.OwnerToConnectionMap[connectParamsDefault.OwnerUri];
            Assert.Equal(2, connectionInfo.CountConnections);
            Assert.Equal(1, service.OwnerToConnectionMap.Count);

            // If we record when the Default connecton calls Close()
            bool defaultDisconnectCalled = false;
            var mockDefaultConnection = new Mock<DbConnection> { CallBase = true };
            mockDefaultConnection.Setup(x => x.Close())
                .Callback(() =>
                {
                    defaultDisconnectCalled = true;
                });
            connectionInfo.ConnectionTypeToConnectionMap[ConnectionType.Default] = mockDefaultConnection.Object;

            // And when the Query connecton calls Close()
            bool queryDisconnectCalled = false;
            var mockQueryConnection = new Mock<DbConnection> { CallBase = true };
            mockQueryConnection.Setup(x => x.Close())
                .Callback(() =>
                {
                    queryDisconnectCalled = true;
                });
            connectionInfo.ConnectionTypeToConnectionMap[ConnectionType.Query] = mockQueryConnection.Object;

            // If we disconnect all open connections with the same URI as used above
            service.Disconnect(disconnectParams);

            // Close() should have gotten called for both DbConnections
            Assert.True(defaultDisconnectCalled);
            Assert.True(queryDisconnectCalled);

            // And the maps that hold connection data should be empty
            Assert.Equal(0, connectionInfo.CountConnections);
            Assert.Equal(0, service.OwnerToConnectionMap.Count);
        }

        [Fact]
        public async void ClosingQueryConnectionShouldLeaveDefaultConnectionOpen()
        {
            // Setup the connect and disconnect params
            var connectParamsDefault = new ConnectParams()
            {
                OwnerUri = "connectParamsSame",
                Connection = TestObjects.GetTestConnectionDetails(),
                Type = ConnectionType.Default
            };
            var connectParamsQuery = new ConnectParams()
            {
                OwnerUri = connectParamsDefault.OwnerUri,
                Connection = TestObjects.GetTestConnectionDetails(),
                Type = ConnectionType.Query
            };
            var disconnectParamsQuery = new DisconnectParams()
            {
                OwnerUri = connectParamsDefault.OwnerUri,
                Type = connectParamsQuery.Type
            };

            // If I connect a Default and a Query connection
            var service = TestObjects.GetTestConnectionService();
            await service.Connect(connectParamsDefault);
            await service.Connect(connectParamsQuery);
            ConnectionInfo connectionInfo = service.OwnerToConnectionMap[connectParamsDefault.OwnerUri];

            // There should be 2 connections in the map
            Assert.Equal(2, connectionInfo.CountConnections);

            // If I Disconnect only the Query connection, there should be 1 connection in the map
            service.Disconnect(disconnectParamsQuery);
            Assert.Equal(1, connectionInfo.CountConnections);

            // If I reconnect, there should be 2 again
            await service.Connect(connectParamsQuery);
            Assert.Equal(2, connectionInfo.CountConnections);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetOrOpenNullOwnerUri(string ownerUri)
        {
            // If: I have a connection service and I ask for a connection with an invalid ownerUri
            // Then: An exception should be thrown
            var service = TestObjects.GetTestConnectionService();
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.GetOrOpenConnection(ownerUri, ConnectionType.Default));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetOrOpenNullConnectionType(string connType)
        {
            // If: I have a connection service and I ask for a connection with an invalid connectionType
            // Then: An exception should be thrown
            var service = TestObjects.GetTestConnectionService();
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.GetOrOpenConnection(TestObjects.ScriptUri, connType));
        }

        [Fact]
        public async Task GetOrOpenNoConnection()
        {
            // If: I have a connection service and I ask for a connection for an unconnected uri
            // Then: An exception should be thrown
            var service = TestObjects.GetTestConnectionService();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => service.GetOrOpenConnection(TestObjects.ScriptUri, ConnectionType.Query));
        }

        [Fact]
        public async Task GetOrOpenNoDefaultConnection()
        {
            // Setup: Create a connection service with an empty connection info obj
            var service = TestObjects.GetTestConnectionService();
            var connInfo = new ConnectionInfo(null, null, null);
            service.OwnerToConnectionMap[TestObjects.ScriptUri] = connInfo;

            // If: I ask for a connection on a connection that doesn't have a default connection
            // Then: An exception should be thrown
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GetOrOpenConnection(TestObjects.ScriptUri, ConnectionType.Query));
        }

        [Fact]
        public async Task GetOrOpenAdminDefaultConnection()
        {
            // Setup: Create a connection service with an empty connection info obj
            var service = TestObjects.GetTestConnectionService();
            var connInfo = new ConnectionInfo(null, null, null);
            service.OwnerToConnectionMap[TestObjects.ScriptUri] = connInfo;

            // If: I ask for a connection on a connection that doesn't have a default connection
            // Then: An exception should be thrown
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GetOrOpenConnection(TestObjects.ScriptUri, ConnectionType.Query));
        }

        [Fact]
        public async Task ConnectionWithAdminConnectionEnsuresOnlyOneConnectionCreated()
        {
            // If I try to connect using a connection string, it overrides the server name and username for the connection
            ConnectParams connectionParameters = TestObjects.GetTestConnectionParams();
            string serverName = "ADMIN:overriddenServerName";
            string userName = "overriddenUserName";
            connectionParameters.Connection.ServerName = serverName;
            connectionParameters.Connection.UserName = userName;

            // Connect
            ConnectionService service = TestObjects.GetTestConnectionService();
            var connectionResult = await service.Connect(connectionParameters);

            // Verify you can get the connection for default
            DbConnection defaultConn = await service.GetOrOpenConnection(connectionParameters.OwnerUri, ConnectionType.Default);
            ConnectionInfo connInfo = service.OwnerToConnectionMap[connectionParameters.OwnerUri];
            Assert.NotNull(defaultConn);
            Assert.Equal(connInfo.AllConnections.Count, 1);

            // Verify that for the Query, no new connection is created
            DbConnection queryConn = await service.GetOrOpenConnection(connectionParameters.OwnerUri, ConnectionType.Query);
            connInfo = service.OwnerToConnectionMap[connectionParameters.OwnerUri];
            Assert.NotNull(defaultConn);
            Assert.Equal(connInfo.AllConnections.Count, 1);

            // Verify that if the query connection was closed, it will be reopened on requesting the connection again
            Assert.Equal(ConnectionState.Open, queryConn.State);
            queryConn.Close();
            Assert.Equal(ConnectionState.Closed, queryConn.State);
            queryConn = await service.GetOrOpenConnection(connectionParameters.OwnerUri, ConnectionType.Query);
            Assert.Equal(ConnectionState.Open, queryConn.State);
        }

        [Fact]
        public async Task ConnectionWithConnectionStringSucceeds()
        {
            // If I connect using a connection string instead of the normal parameters, the connection succeeds
            var connectionParameters = TestObjects.GetTestConnectionParams(true);
            var connectionResult = await TestObjects.GetTestConnectionService().Connect(connectionParameters);

            Assert.NotEmpty(connectionResult.ConnectionId);
        }

        [Fact]
        public async Task ConnectionWithBadConnectionStringFails()
        {
            // If I try to connect using an invalid connection string, the connection fails
            var connectionParameters = TestObjects.GetTestConnectionParams(true);
            connectionParameters.Connection.ConnectionString = "thisisnotavalidconnectionstring";
            var connectionResult = await TestObjects.GetTestConnectionService().Connect(connectionParameters);

            Assert.NotEmpty(connectionResult.ErrorMessage);
        }

        [Fact]
        public async Task ConnectionWithConnectionStringOverridesServerInfo()
        {
            // If I try to connect using a connection string, it overrides the server name and username for the connection
            var connectionParameters = TestObjects.GetTestConnectionParams();
            var serverName = "overriddenServerName";
            var userName = "overriddenUserName";
            connectionParameters.Connection.ServerName = serverName;
            connectionParameters.Connection.UserName = userName;
            var connectionString = TestObjects.GetTestConnectionParams(true).Connection.ConnectionString;
            connectionParameters.Connection.ConnectionString = connectionString;

            // Connect and verify that the connectionParameters object's server name and username have been overridden
            var connectionResult = await TestObjects.GetTestConnectionService().Connect(connectionParameters);
            Assert.NotEqual(serverName, connectionResult.ConnectionSummary.ServerName);
            Assert.NotEqual(userName, connectionResult.ConnectionSummary.UserName);
        }

        [Fact]
        public async Task OtherParametersOverrideConnectionString()
        {
            // If I try to connect using a connection string, and set parameters other than the server name, username, or password,
            // they override the values in the connection string.
            var connectionParameters = TestObjects.GetTestConnectionParams();
            var databaseName = "overriddenDatabaseName";
            connectionParameters.Connection.DatabaseName = databaseName;
            var connectionString = TestObjects.GetTestConnectionParams(true).Connection.ConnectionString;
            connectionParameters.Connection.ConnectionString = connectionString;

            // Connect and verify that the connection string's database name has been overridden
            var connectionResult = await TestObjects.GetTestConnectionService().Connect(connectionParameters);
            Assert.Equal(databaseName, connectionResult.ConnectionSummary.DatabaseName);
        }

        [Fact]
        public async Task CanChangeDatabase()
        {
            string ownerUri = "file://my/sample/file.sql";
            const string masterDbName = "master";
            const string otherDbName = "other";
            // Given a connection that returns the database name
            var connection = new TestSqlConnection(null);

            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string connString, string azureAccountToken) =>
            {
                connection.ConnectionString = connString;
                SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(connString);

                // Database name is respected. Follow heuristic where empty DB name really means Master
                var dbName = string.IsNullOrEmpty(scsb.InitialCatalog) ? masterDbName : scsb.InitialCatalog;
                connection.SetDatabase(dbName);
                return connection;
            });

            var connectionService = new ConnectionService(mockFactory.Object);

            // When I connect to default
            var connectionResult = await
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = TestObjects.GetTestConnectionDetails()
                });

            connection.SetState(ConnectionState.Open);

            connectionService.ChangeConnectionDatabaseContext(ownerUri, otherDbName);

            Assert.Equal(otherDbName, connection.Database);
        }

        [Fact]
        public async Task CanChangeDatabaseAzure()
        {

            string ownerUri = "file://my/sample/file.sql";
            const string masterDbName = "master";
            const string otherDbName = "other";
            string dbName = masterDbName;
            // Given a connection that returns the database name
            var mockConnection = new Mock<DbConnection>();
            mockConnection.Setup(conn => conn.ChangeDatabase(It.IsAny<string>()))
            .Throws(new Exception());
            mockConnection.SetupGet(conn => conn.Database).Returns(dbName);
            mockConnection.SetupGet(conn => conn.State).Returns(ConnectionState.Open);
            mockConnection.Setup(conn => conn.Close());
            mockConnection.Setup(conn => conn.Open());

            var connection = mockConnection.Object;

            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string connString, string azureAccountToken) =>
            {
                connection.ConnectionString = connString;
                SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(connString);

                // Database name is respected. Follow heuristic where empty DB name really means Master
                dbName = string.IsNullOrEmpty(scsb.InitialCatalog) ? masterDbName : scsb.InitialCatalog;
                return connection;
            });

            var connectionService = new ConnectionService(mockFactory.Object);

            // When I connect to default
            var connectionResult = await
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = TestObjects.GetTestConnectionDetails()
                });

            ConnectionInfo testInfo;
            connectionService.TryFindConnection(ownerUri, out testInfo);

            Assert.NotNull(testInfo);

            testInfo.IsCloud = true;

            connectionService.ChangeConnectionDatabaseContext(ownerUri, otherDbName, true);

            Assert.Equal(otherDbName, dbName);
        }

        [Fact]
        public async Task ReturnsFalseIfNotForced()
        {
            string ownerUri = "file://my/sample/file.sql";
            const string defaultDbName = "databaseName";
            const string otherDbName = "other";
            string dbName = defaultDbName;
            // Given a connection that returns the database name
            var mockConnection = new Mock<DbConnection>();
            mockConnection.Setup(conn => conn.ChangeDatabase(It.IsAny<string>()))
            .Throws(new Exception());
            mockConnection.SetupGet(conn => conn.Database).Returns(dbName);
            mockConnection.SetupGet(conn => conn.State).Returns(ConnectionState.Open);
            mockConnection.Setup(conn => conn.Close());
            mockConnection.Setup(conn => conn.Open());

            var connection = mockConnection.Object;

            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string connString, string azureAccountToken) =>
            {
                connection.ConnectionString = connString;
                SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(connString);

                // Database name is respected. Follow heuristic where empty DB name really means Master
                dbName = string.IsNullOrEmpty(scsb.InitialCatalog) ? defaultDbName : scsb.InitialCatalog;
                return connection;
            });

            var connectionService = new ConnectionService(mockFactory.Object);

            // When I connect to default
            var connectionResult = await
                connectionService
                .Connect(new ConnectParams()
                {
                    OwnerUri = ownerUri,
                    Connection = TestObjects.GetTestConnectionDetails()
                });

            ConnectionInfo testInfo;
            connectionService.TryFindConnection(ownerUri, out testInfo);

            Assert.NotNull(testInfo);

            testInfo.IsCloud = true;

            Assert.False(connectionService.ChangeConnectionDatabaseContext(ownerUri, otherDbName));

            Assert.Equal(defaultDbName, dbName);
        }

        /// <summary>
        /// Test ParseConnectionString
        /// </summary>
        [Fact]
        public void ParseConnectionStringTest()
        {
            // If we make a connection to a live database
            ConnectionService service = ConnectionService.Instance;

            var connectionString = "Server=tcp:{servername},1433;Initial Catalog={databasename};Persist Security Info=False;User ID={your_username};Password={your_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            var details = service.ParseConnectionString(connectionString);

            Assert.Equal("tcp:{servername},1433", details.ServerName);
            Assert.Equal("{databasename}", details.DatabaseName);
            Assert.Equal("{your_username}", details.UserName);
            Assert.Equal("{your_password}", details.Password);
            Assert.Equal(false, details.PersistSecurityInfo);
            Assert.Equal(false, details.MultipleActiveResultSets);
            Assert.Equal(true, details.Encrypt);
            Assert.Equal(false, details.TrustServerCertificate);
            Assert.Equal(30, details.ConnectTimeout);
        }

        [Fact]
        public async void ConnectingWithAzureAccountUsesToken()
        {
            // Set up mock connection factory
            var mockFactory = new Mock<ISqlConnectionFactory>();
            mockFactory.Setup(factory => factory.CreateSqlConnection(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new TestSqlConnection(null));
            var connectionService = new ConnectionService(mockFactory.Object);

            var details = TestObjects.GetTestConnectionDetails();
            var azureAccountToken = "testAzureAccountToken";
            details.AzureAccountToken = azureAccountToken;
            details.UserName = "";
            details.Password = "";
            details.AuthenticationType = "AzureMFA";

            // If I open a connection using connection details that include an account token
            await connectionService.Connect(new ConnectParams
            {
                OwnerUri = "testURI",
                Connection = details
            });

            // Then the connection factory got called with details including an account token
            mockFactory.Verify(factory => factory.CreateSqlConnection(It.IsAny<string>(), It.Is<string>(accountToken => accountToken == azureAccountToken)), Times.Once());
        }
    }
}
