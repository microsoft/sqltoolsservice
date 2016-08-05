//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.Test.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Connection
{
    /// <summary>
    /// Tests for the ServiceHost Connection Service tests
    /// </summary>
    public class ConnectionServiceTests
    {
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
        [InlineDataAttribute(null, "my-server", "test", "sa", "123456")]
        [InlineDataAttribute("file://my/sample/file.sql", null, "test", "sa", "123456")]
        [InlineDataAttribute("file://my/sample/file.sql", "my-server", null, "sa", "123456")]
        [InlineDataAttribute("file://my/sample/file.sql", "my-server", "test", null, "123456")]
        [InlineDataAttribute("file://my/sample/file.sql", "my-server", "test", "sa", null)]
        [InlineDataAttribute("", "my-server", "test", "sa", "123456")]
        [InlineDataAttribute("file://my/sample/file.sql", "", "test", "sa", "123456")]
        [InlineDataAttribute("file://my/sample/file.sql", "my-server", "", "sa", "123456")]
        [InlineDataAttribute("file://my/sample/file.sql", "my-server", "test", "", "123456")]
        [InlineDataAttribute("file://my/sample/file.sql", "my-server", "test", "sa", "")]
        public void ConnectingWithInvalidParametersYieldsErrorMessage(string ownerUri, string server, string database, string userName, string password)
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
                        Password = password
                    }
                });
            
            // check that an error was caught
            Assert.NotNull(connectionResult.Messages);
            Assert.NotEqual(String.Empty, connectionResult.Messages);
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

            //var endpoint = new Mock<IProtocolEndpoint>();
            //Func<ConnectParams, RequestContext<ConnectResponse>, Task> connectRequestHandler = null;
            //endpoint.Setup(e => e.SetRequestHandler(ConnectionRequest.Type, It.IsAny<Func<ConnectParams, RequestContext<ConnectResponse>, Task>>()))
            //    .Callback<Func<ConnectParams, RequestContext<ConnectResponse>, Task>>(handler => connectRequestHandler = handler);

            // when I initialize the service
            //service.InitializeService(endpoint.Object);

            // then I expect the handler to be captured
            //Assert.NotNull(connectRequestHandler);

            // when I call the service
            //var requestContext = new Mock<RequestContext<ConnectResponse>>();

            //connectRequestHandler(connectParams, requestContext.Object);
            // then I should get a live connection

            // and then I should have 
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
