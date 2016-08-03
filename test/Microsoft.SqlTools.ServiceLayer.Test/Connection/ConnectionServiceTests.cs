//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
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

        //[Fact]
        //public void TestConnectRequestRegistersOwner()
        //{
        //    // Given a request to connect to a database
        //    var service = new ConnectionService(new TestSqlConnectionFactory());
        //    ConnectionDetails connectionDetails = TestObjects.GetTestConnectionDetails();
        //    var connectParams = new ConnectParams()
        //    {
        //        OwnerUri = "file://path/to/my.sql",
        //        Connection = connectionDetails
        //    };

        //    var endpoint = new Mock<IProtocolEndpoint>();
        //    Func<ConnectParams, RequestContext<ConnectResponse>, Task> connectRequestHandler = null;
        //    endpoint.Setup(e => e.SetRequestHandler(ConnectionRequest.Type, It.IsAny<Func<ConnectParams, RequestContext<ConnectResponse>, Task>>()))
        //        .Callback<Func<ConnectParams, RequestContext<ConnectResponse>, Task>>(handler => connectRequestHandler = handler);

        //    // when I initialize the service
        //    service.InitializeService(endpoint.Object);

        //    // then I expect the handler to be captured
        //    Assert.NotNull(connectRequestHandler);

        //    // when I call the service
        //    var requestContext = new Mock<RequestContext<ConnectResponse>>();

        //    connectRequestHandler(connectParams, requestContext);
        //    // then I should get a live connection

        //    // and then I should have 
        //    // connect to a database instance 
        //    var connectionResult =
        //        TestObjects.GetTestConnectionService()
        //        .Connect(TestObjects.GetTestConnectionDetails());

        //    // verify that a valid connection id was returned
        //    Assert.True(connectionResult.ConnectionId > 0);
        //}
    }
}
