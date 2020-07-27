//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    [TestFixture]
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

        [Test]
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

        [Test]
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
            Assert.That(connectResult.ConnectionId, Is.Not.Empty);
        }

        [Test]
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
        [Test]
        public async Task CanConnectWithEmptyDatabaseName([Values(null, "")]string databaseName)
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

            Assert.That(connectionResult.ConnectionId, Is.Not.Empty, "check that a connection was created");
        }

        /// <summary>
        /// Verify that we can connect to the default database when no database name is
        /// provided as a parameter.
        /// </summary>
        [Test]
        public async Task ConnectToDefaultDatabaseRespondsWithActualDbName([Values("master", "nonMasterDb")]string expectedDbName)
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

            Assert.Multiple(() =>
            {
                Assert.That(connectionResult.ConnectionId, Is.Not.Empty, "ConnectionId");
                Assert.NotNull(connectionResult.ConnectionSummary, "ConnectionSummary");
                Assert.AreEqual(expectedDbName, connectionResult.ConnectionSummary.DatabaseName, "I expect connection to succeed and the Summary to include the correct DB name");
            });
        }

        /// <summary>
        /// Verify that when a connection is started for a URI with an already existing
        /// connection, we disconnect first before connecting.
        /// </summary>
        [Test]
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
            Assert.That(connectionResult.ConnectionId, Is.Not.Null.Or.Empty, "check that the connection was successful");

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
            Assert.That(connectionResult.ConnectionId, Is.Not.Null.Or.Empty, "check that the connection was successful");
            Assert.AreEqual(otherDbName, connectionResult.ConnectionSummary.DatabaseName);
        }

        /// <summary>
        /// Verify that when connecting with invalid credentials, an error is thrown.
        /// </summary>
        [Test]
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

            Assert.That(connectionResult.Messages, Is.Not.Null.Or.Empty, "check that an error was caught");
        }

        static readonly object[] invalidParameters =
        {
            new object[] { "SqlLogin", null, "my-server", "test", "sa", "123456" },
            new object[] { "SqlLogin", "file://my/sample/file.sql", null, "test", "sa", "123456" },
            new object[] {"SqlLogin", "file://my/sample/file.sql", "my-server", "test", null, "123456"},
            new object[] {"SqlLogin", "file://my/sample/file.sql", "my-server", "test", "sa", null},
            new object[] {"SqlLogin", "", "my-server", "test", "sa", "123456" },
            new object[] {"SqlLogin", "file://my/sample/file.sql", "", "test", "sa", "123456"},
            new object[] {"SqlLogin", "file://my/sample/file.sql", "my-server", "test", "", "123456"},
            new object[] {"SqlLogin", "file://my/sample/file.sql", "my-server", "test", "sa", ""},
            new object[] {"Integrated", null, "my-server", "test", "sa", "123456"},
            new object[] {"Integrated", "file://my/sample/file.sql", null, "test", "sa", "123456"},
            new object[] {"Integrated", "", "my-server", "test", "sa", "123456"},
            new object[] {"Integrated", "file://my/sample/file.sql", "", "test", "sa", "123456"}
    };
        /// <summary>
        /// Verify that when connecting with invalid parameters, an error is thrown.
        /// </summary>
        [Test, TestCaseSource(nameof(invalidParameters))]        
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

            Assert.That(connectionResult.Messages, Is.Not.Null.Or.Empty, "check that an error was caught");
        }

        static readonly object[] noUserNameOrPassword =
        {
            new object[] {null, null},
            new object[] {null, ""},
            new object[] {"", null},
            new object[] {"", ""},
            new object[] {"sa", null},
            new object[] {"sa", ""},
            new object[] {null, "12345678"},
            new object[] {"", "12345678"},
        };
        /// <summary>
        /// Verify that when using integrated authentication, the username and/or password can be empty.
        /// </summary>
        [Test, TestCaseSource(nameof(noUserNameOrPassword))]
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

            Assert.That(connectionResult.ConnectionId, Is.Not.Null.Or.Empty, "check that the connection was successful");
        }

        /// <summary>
        /// Verify that when connecting with a null parameters object, an error is thrown.
        /// </summary>
        [Test]
        public async Task ConnectingWithNullParametersObjectYieldsErrorMessage()
        {
            // Connect with null parameters
            var connectionResult = await
                TestObjects.GetTestConnectionService()
                .Connect(null);

            Assert.That(connectionResult.Messages, Is.Not.Null.Or.Empty, "check that an error was caught");
        }


        private static readonly object[] optionalParameters =
        {
            new object[] {"AuthenticationType", "Integrated", "Integrated Security" },
            new object[] {"AuthenticationType", "SqlLogin", ""},
            new object[] {"Encrypt", true, "Encrypt"},
            new object[] {"Encrypt", false, "Encrypt"},
            new object[] {"ColumnEncryptionSetting", "Enabled", "Column Encryption Setting=Enabled"},
            new object[] {"ColumnEncryptionSetting", "Disabled", "Column Encryption Setting=Disabled"},
            new object[] {"ColumnEncryptionSetting", "enabled", "Column Encryption Setting=Enabled"},
            new object[] {"ColumnEncryptionSetting", "disabled", "Column Encryption Setting=Disabled"},
            new object[] {"ColumnEncryptionSetting", "ENABLED", "Column Encryption Setting=Enabled"},
            new object[] {"ColumnEncryptionSetting", "DISABLED", "Column Encryption Setting=Disabled"},
            new object[] {"ColumnEncryptionSetting", "eNaBlEd", "Column Encryption Setting=Enabled"},
            new object[] {"ColumnEncryptionSetting", "DiSaBlEd", "Column Encryption Setting=Disabled"},
            new object[] {"TrustServerCertificate", true, "Trust Server Certificate"},
            new object[] {"TrustServerCertificate", false, "Trust Server Certificate"},
            new object[] {"PersistSecurityInfo", true, "Persist Security Info"},
            new object[] {"PersistSecurityInfo", false, "Persist Security Info"},
            new object[] {"ConnectTimeout", 15, "Connect Timeout"},
            new object[] {"ConnectRetryCount", 1, "Connect Retry Count"},
            new object[] {"ConnectRetryInterval", 10, "Connect Retry Interval"},
            new object[] {"ApplicationName", "vscode-mssql", "Application Name"},
            new object[] {"WorkstationId", "mycomputer", "Workstation ID"},
            new object[] {"ApplicationIntent", "ReadWrite", "Application Intent"},
            new object[] {"ApplicationIntent", "ReadOnly", "Application Intent"},
            new object[] {"CurrentLanguage", "test", "Current Language"},
            new object[] {"Pooling", false, "Pooling"},
            new object[] {"Pooling", true, "Pooling"},
            new object[] {"MaxPoolSize", 100, "Max Pool Size"},
            new object[] {"MinPoolSize", 0, "Min Pool Size"},
            new object[] {"LoadBalanceTimeout", 0, "Load Balance Timeout"},
            new object[] {"Replication", true, "Replication"},
            new object[] {"Replication", false, "Replication"},
            new object[] {"AttachDbFilename", "myfile", "AttachDbFilename"},
            new object[] {"FailoverPartner", "partner", "Failover Partner"},
            new object[] {"MultiSubnetFailover", true, "Multi Subnet Failover"},
            new object[] {"MultiSubnetFailover", false, "Multi Subnet Failover"},
            new object[] {"MultipleActiveResultSets", false, "Multiple Active Result Sets"},
            new object[] {"MultipleActiveResultSets", true, "Multiple Active Result Sets"},
            new object[] {"PacketSize", 8192, "Packet Size"},
            new object[] {"TypeSystemVersion", "Latest", "Type System Version"},
        };

        /// <summary>
        /// Verify that optional parameters can be built into a connection string for connecting.
        /// </summary>
        [Test, TestCaseSource(nameof(optionalParameters))]
        public void ConnectingWithOptionalParametersBuildsConnectionString(string propertyName, object propertyValue, string connectionStringMarker)
        {
            // Create a test connection details object and set the property to a specific value
            ConnectionDetails details = TestObjects.GetTestConnectionDetails();
            PropertyInfo info = details.GetType().GetProperty(propertyName);
            info.SetValue(details, propertyValue);

            // Test that a connection string can be created without exceptions
            string connectionString = ConnectionService.BuildConnectionString(details);

            Assert.That(connectionString, Contains.Substring(connectionStringMarker), "Verify that the parameter is in the connection string");
        }

        private static readonly object[] optionalEnclaveParameters =
        {
            new object[] {"EnclaveAttestationProtocol", "AAS", "Attestation Protocol=AAS"},
            new object[] {"EnclaveAttestationProtocol", "HGS", "Attestation Protocol=HGS"},
            new object[] {"EnclaveAttestationProtocol", "aas", "Attestation Protocol=AAS"},
            new object[] {"EnclaveAttestationProtocol", "hgs", "Attestation Protocol=HGS"},
            new object[] {"EnclaveAttestationProtocol", "AaS", "Attestation Protocol=AAS"},
            new object[] {"EnclaveAttestationProtocol", "hGs", "Attestation Protocol=HGS"},
            new object[] {"EnclaveAttestationUrl", "https://attestation.us.attest.azure.net/attest/SgxEnclave", "Enclave Attestation Url=https://attestation.us.attest.azure.net/attest/SgxEnclave" },
        };

        /// <summary>
        /// Verify that optional parameters which require ColumnEncryptionSetting to be enabled
        /// can be built into a connection string for connecting.
        /// </summary>
        [Test, TestCaseSource(nameof(optionalEnclaveParameters))]
        public void ConnectingWithOptionalEnclaveParametersBuildsConnectionString(string propertyName, object propertyValue, string connectionStringMarker)
        {
            // Create a test connection details object and set the property to a specific value
            ConnectionDetails details = TestObjects.GetTestConnectionDetails();
            details.ColumnEncryptionSetting = "Enabled";
            details.GetType()
                .GetProperty(propertyName)
                .SetValue(details, propertyValue);

            // Test that a connection string can be created without exceptions
            string connectionString = ConnectionService.BuildConnectionString(details);            
            Assert.That(connectionString, Contains.Substring(connectionStringMarker), "Verify that the parameter is in the connection string");
        }

        private static readonly object[] invalidOptions =
        {
            new object[] {"AuthenticationType", "NotAValidAuthType" },
            new object[] {"ColumnEncryptionSetting", "NotAValidColumnEncryptionSetting" },
            new object[] {"EnclaveAttestationProtocol", "NotAValidEnclaveAttestationProtocol" },
        };
        /// <summary>
        /// Build connection string with an invalid property type
        /// </summary>
        [Test, TestCaseSource(nameof(invalidOptions))]
        public void BuildConnectionStringWithInvalidOptions(string propertyName, object propertyValue)
        {
            ConnectionDetails details = TestObjects.GetTestConnectionDetails();
            PropertyInfo info = details.GetType().GetProperty(propertyName);
            info.SetValue(details, propertyValue);
            Assert.Throws<ArgumentException>(() => ConnectionService.BuildConnectionString(details));
        }

        private static readonly Tuple<string,object>[][] optionCombos =
        {
            new []
                {
                    Tuple.Create<string, object>("ColumnEncryptionSetting", null),
                    Tuple.Create<string, object>("EnclaveAttestationProtocol", "AAS"),
                    Tuple.Create<string, object>("EnclaveAttestationUrl", "https://attestation.us.attest.azure.net/attest/SgxEnclave")
                },
            new []
                {
                    Tuple.Create<string, object>("ColumnEncryptionSetting", "Disabled"),
                    Tuple.Create<string, object>("EnclaveAttestationProtocol", "AAS"),
                    Tuple.Create<string, object>("EnclaveAttestationUrl", "https://attestation.us.attest.azure.net/attest/SgxEnclave")
                },
            new []
                {
                    Tuple.Create<string, object>("ColumnEncryptionSetting", ""),
                    Tuple.Create<string, object>("EnclaveAttestationProtocol", "AAS"),
                    Tuple.Create<string, object>("EnclaveAttestationUrl", "https://attestation.us.attest.azure.net/attest/SgxEnclave")
                }
        };

        /// <summary>
        /// Build connection string with an invalid property combinations
        /// </summary>
        [Test]
        public void ConnStrWithInvalidOptions()
        {
            ConnectionDetails details = TestObjects.GetTestConnectionDetails();
            foreach (var options in optionCombos)
            {
                options.ToList().ForEach(tuple =>
                {
                    PropertyInfo info = details.GetType().GetProperty(tuple.Item1);
                    info.SetValue(details, tuple.Item2);
                });
                Assert.Throws<ArgumentException>(() => ConnectionService.BuildConnectionString(details));
            }
        }

        /// <summary>
        /// Verify that a connection changed event is fired when the database context changes.
        /// </summary>
        [Test]
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

            Assert.That(connectionResult.ConnectionId, Is.Not.Empty, "verify that a valid connection id was returned");

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
        [Test]
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

            Assert.That(connectionResult.ConnectionId, Is.Not.Null.Or.Empty, "check that the connection was successful");
        }

        /// <summary>
        /// Verify that we can disconnect from an active connection successfully
        /// </summary>
        [Test]
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

            Assert.That(connectionResult.ConnectionId, Is.Not.Null.Or.Empty, "check that the connection was successful");

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
        [Test]
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

            Assert.That(connectionResult.ConnectionId, Is.Not.Null.Or.Empty, "check that the connection was successful");

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
        [Test]
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
            Assert.That(connectionResult.ConnectionId, Is.Not.Null.Or.Empty, "check that the connection was successful");

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
        [Test]
        public async Task DisconnectValidatesParameters([Values("", null)] string disconnectUri)
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
            Assert.That(connectionResult.ConnectionId, Is.Not.Null.Or.Empty, "check that the connection was successful");

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
            Assert.That(connectionResult.ConnectionId, Is.Not.Null.Or.Empty, "check that the connection was successful");

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
        [Test]
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

            Assert.AreEqual(5, databaseNames.Length);
            Assert.AreEqual("master", databaseNames[0]);
            Assert.AreEqual("model", databaseNames[1]);
            Assert.AreEqual("msdb", databaseNames[2]);
            Assert.AreEqual("tempdb", databaseNames[3]);
            Assert.AreEqual("mydatabase", databaseNames[4]);
        }

        /// <summary>
        /// Verifies the the list databases operation lists database names for the server used by a connection.
        /// </summary>
        [Test]
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

            Assert.AreEqual(5, response.Databases.Length);
            VerifyDatabaseDetail(rows[0], response.Databases[4]);
            VerifyDatabaseDetail(rows[1], response.Databases[0]);
            VerifyDatabaseDetail(rows[2], response.Databases[1]);
            VerifyDatabaseDetail(rows[3], response.Databases[2]);
            VerifyDatabaseDetail(rows[4], response.Databases[3]);
        }

        private void VerifyDatabaseDetail(object[] expected, DatabaseInfo actual)
        {
            Assert.AreEqual(expected[0], actual.Options[ListDatabasesRequestDatabaseProperties.Name]);
            Assert.AreEqual(expected[1], actual.Options[ListDatabasesRequestDatabaseProperties.State]);
            Assert.AreEqual(expected[2], actual.Options[ListDatabasesRequestDatabaseProperties.SizeInMB]);
            Assert.AreEqual(expected[3], actual.Options[ListDatabasesRequestDatabaseProperties.LastBackup]);
        }


        /// <summary>
        /// Verify that the factory is returning DatabaseNamesHandler
        /// </summary>
        [Test]
        public void ListDatabaseRequestFactoryReturnsDatabaseNamesHandler()
        {
            var handler = ListDatabaseRequestHandlerFactory.getHandler(includeDetails: false, isSqlDB: true);
            Assert.That(handler, Is.InstanceOf<DatabaseNamesHandler>());
            handler = ListDatabaseRequestHandlerFactory.getHandler(includeDetails: false, isSqlDB: false);
            Assert.That(handler, Is.InstanceOf<DatabaseNamesHandler>());
        }

        /// <summary>
        /// Verify that the factory is returning SqlDBDatabaseDetailHandler
        /// </summary>
        [Test]
        public void ListDatabaseRequestFactoryReturnsSqlDBHandler()
        {
            var handler = ListDatabaseRequestHandlerFactory.getHandler(includeDetails: true, isSqlDB: true);
            Assert.That(handler, Is.InstanceOf<SqlDBDatabaseDetailHandler>());
        }

        /// <summary>
        /// Verify that the factory is returning SqlServerDatabaseDetailHandler
        /// </summary>
        [Test]
        public void ListDatabaseRequestFactoryReturnsSqlServerHandler()
        {
            var handler = ListDatabaseRequestHandlerFactory.getHandler(includeDetails: true, isSqlDB: false);
            Assert.That(handler, Is.InstanceOf<SqlServerDatabaseDetailHandler>());
        }

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Test]
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
        [Test]
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
        [Test]
        public async Task TestConnectRequestRegistersOwner()
        {
            // Given a request to connect to a database
            var service = TestObjects.GetTestConnectionService();
            var connectParams = TestObjects.GetTestConnectionParams();

            // connect to a database instance
            var connectionResult = await service.Connect(connectParams);

            // verify that a valid connection id was returned
            Assert.NotNull(connectionResult.ConnectionId);
            Assert.That(connectionResult.ConnectionId, Is.Not.EqualTo(string.Empty));
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
        [Test]
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
                    Assert.AreEqual(0, ex.Number);
                }
            });
        }

        /// <summary>
        /// Test that cancel connection with a null connection parameter
        /// </summary>
        [Test]
        public void TestCancelConnectionNullParam()
        {
            var service = TestObjects.GetTestConnectionService();
            Assert.False(service.CancelConnect(null));
        }

        /// <summary>
        /// Test that cancel connection with a null connection parameter
        /// </summary>
        [Test]
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
        [Test]
        public void TestConnectionCompleteNotificationIsCreated()
        {
            Assert.NotNull(ConnectionCompleteNotification.Type);
        }

        /// <summary>
        /// Test that the connection summary comparer creates a hash code correctly
        /// </summary>
        [Test]
        public void TestConnectionSummaryComparerHashCode([Values]bool objectNull, 
                                                          [Values(null, "server")]string serverName, 
                                                          [Values(null, "test")]string databaseName, 
                                                          [Values(null, "sa")]string userName)
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
                Assert.AreEqual(31, hashCode, "I expect it to be 31 for a null summary");
            }
            else
            {
                Assert.That(hashCode, Is.Not.EqualTo(31), "And not 31 otherwise");
            }
        }

        [Test]
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

            Assert.That(errorMessage, Is.Not.Null.Or.Empty, "Then I expect an error message");
        }

        [Test]
        public async Task ConnectingTwiceWithTheSameUriDoesNotCreateAnotherDbConnection()
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
            Assert.AreEqual(0, ownerToConnectionMap.Count);

            // If we connect to the service, there should be 1 connection
            await service.Connect(connectParamsSame1);
            Assert.AreEqual(1, ownerToConnectionMap.Count);

            // If we connect again with the same URI, there should still be 1 connection
            await service.Connect(connectParamsSame2);
            Assert.AreEqual(1, ownerToConnectionMap.Count);

            // If we connect with a different URI, there should be 2 connections
            await service.Connect(connectParamsDifferent);
            Assert.AreEqual(2, ownerToConnectionMap.Count);

            // If we disconnect with the unique URI, there should be 1 connection
            service.Disconnect(disconnectParamsDifferent);
            Assert.AreEqual(1, ownerToConnectionMap.Count);

            // If we disconnect with the duplicate URI, there should be 0 connections
            service.Disconnect(disconnectParamsSame);
            Assert.AreEqual(0, ownerToConnectionMap.Count);
        }

        [Test]
        public async Task DbConnectionDoesntLeakUponDisconnect()
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
            Assert.AreEqual(2, connectionInfo.CountConnections);
            Assert.AreEqual(1, service.OwnerToConnectionMap.Count);

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
            Assert.AreEqual(0, connectionInfo.CountConnections);
            Assert.AreEqual(0, service.OwnerToConnectionMap.Count);
        }

        [Test]
        public async Task ClosingQueryConnectionShouldLeaveDefaultConnectionOpen()
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
            Assert.AreEqual(2, connectionInfo.CountConnections);

            // If I Disconnect only the Query connection, there should be 1 connection in the map
            service.Disconnect(disconnectParamsQuery);
            Assert.AreEqual(1, connectionInfo.CountConnections);

            // If I reconnect, there should be 2 again
            await service.Connect(connectParamsQuery);
            Assert.AreEqual(2, connectionInfo.CountConnections);
        }

        [Test]
        public async Task GetOrOpenNullOwnerUri([Values(null, "")]string ownerUri)
        {
            // If: I have a connection service and I ask for a connection with an invalid ownerUri
            // Then: An exception should be thrown
            var service = TestObjects.GetTestConnectionService();
             Assert.ThrowsAsync<ArgumentException>(
                () => service.GetOrOpenConnection(ownerUri, ConnectionType.Default));
        }

        [Test]
        public async Task GetOrOpenNullConnectionType([Values(null, "")] string connType)
        {
            // If: I have a connection service and I ask for a connection with an invalid connectionType
            // Then: An exception should be thrown
            var service = TestObjects.GetTestConnectionService();
            Assert.ThrowsAsync<ArgumentException>(
                () => service.GetOrOpenConnection(TestObjects.ScriptUri, connType));
        }

        [Test]
        public async Task GetOrOpenNoConnection()
        {
            // If: I have a connection service and I ask for a connection for an unconnected uri
            // Then: An exception should be thrown
            var service = TestObjects.GetTestConnectionService();
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => service.GetOrOpenConnection(TestObjects.ScriptUri, ConnectionType.Query));
        }

        [Test]
        public async Task GetOrOpenNoDefaultConnection()
        {
            // Setup: Create a connection service with an empty connection info obj
            var service = TestObjects.GetTestConnectionService();
            var connInfo = new ConnectionInfo(null, null, null);
            service.OwnerToConnectionMap[TestObjects.ScriptUri] = connInfo;

            // If: I ask for a connection on a connection that doesn't have a default connection
            // Then: An exception should be thrown
            Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GetOrOpenConnection(TestObjects.ScriptUri, ConnectionType.Query));
        }

        [Test]
        public async Task GetOrOpenAdminDefaultConnection()
        {
            // Setup: Create a connection service with an empty connection info obj
            var service = TestObjects.GetTestConnectionService();
            var connInfo = new ConnectionInfo(null, null, null);
            service.OwnerToConnectionMap[TestObjects.ScriptUri] = connInfo;

            // If: I ask for a connection on a connection that doesn't have a default connection
            // Then: An exception should be thrown
            Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GetOrOpenConnection(TestObjects.ScriptUri, ConnectionType.Query));
        }

        [Test]
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
            Assert.AreEqual(1, connInfo.AllConnections.Count);

            // Verify that for the Query, no new connection is created
            DbConnection queryConn = await service.GetOrOpenConnection(connectionParameters.OwnerUri, ConnectionType.Query);
            connInfo = service.OwnerToConnectionMap[connectionParameters.OwnerUri];
            Assert.NotNull(defaultConn);
            Assert.AreEqual(1, connInfo.AllConnections.Count);

            // Verify that if the query connection was closed, it will be reopened on requesting the connection again
            Assert.AreEqual(ConnectionState.Open, queryConn.State);
            queryConn.Close();
            Assert.AreEqual(ConnectionState.Closed, queryConn.State);
            queryConn = await service.GetOrOpenConnection(connectionParameters.OwnerUri, ConnectionType.Query);
            Assert.AreEqual(ConnectionState.Open, queryConn.State);
        }

        [Test]
        public async Task ConnectionWithConnectionStringSucceeds()
        {
            // If I connect using a connection string instead of the normal parameters, the connection succeeds
            var connectionParameters = TestObjects.GetTestConnectionParams(true);
            var connectionResult = await TestObjects.GetTestConnectionService().Connect(connectionParameters);

            Assert.That(connectionResult.ConnectionId, Is.Not.Null.Or.Empty, "check that the connection was successful");
        }

        [Test]
        public async Task ConnectionWithBadConnectionStringFails()
        {
            // If I try to connect using an invalid connection string, the connection fails
            var connectionParameters = TestObjects.GetTestConnectionParams(true);
            connectionParameters.Connection.ConnectionString = "thisisnotavalidconnectionstring";
            var connectionResult = await TestObjects.GetTestConnectionService().Connect(connectionParameters);

            Assert.That(connectionResult.ErrorMessage, Is.Not.Empty);
        }

        [Test]
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
            Assert.That(connectionResult.ConnectionSummary.ServerName, Is.Not.EqualTo(serverName));
            Assert.That(connectionResult.ConnectionSummary.UserName, Is.Not.EqualTo(userName));
        }

        [Test]
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
            Assert.AreEqual(databaseName, connectionResult.ConnectionSummary.DatabaseName);
        }

        [Test]
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

            Assert.AreEqual(otherDbName, connection.Database);
        }

        [Test]
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

            Assert.AreEqual(otherDbName, dbName);
        }

        [Test]
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

            Assert.AreEqual(defaultDbName, dbName);
        }

        /// <summary>
        /// Test ParseConnectionString
        /// </summary>
        [Test]
        public void ParseConnectionStringTest()
        {
            // If we make a connection to a live database
            ConnectionService service = ConnectionService.Instance;

            var connectionString = "Server=tcp:{servername},1433;Initial Catalog={databasename};Persist Security Info=False;User ID={your_username};Password={your_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            var details = service.ParseConnectionString(connectionString);

            Assert.AreEqual("tcp:{servername},1433", details.ServerName);
            Assert.AreEqual("{databasename}", details.DatabaseName);
            Assert.AreEqual("{your_username}", details.UserName);
            Assert.AreEqual("{your_password}", details.Password);
            Assert.AreEqual(false, details.PersistSecurityInfo);
            Assert.AreEqual(false, details.MultipleActiveResultSets);
            Assert.AreEqual(true, details.Encrypt);
            Assert.AreEqual(false, details.TrustServerCertificate);
            Assert.AreEqual(30, details.ConnectTimeout);
        }

        [Test]
        public async Task ConnectingWithAzureAccountUsesToken()
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
