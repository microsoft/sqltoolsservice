//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Test.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Connection
{
    /// <summary>
    /// Tests for the ServiceHost Connection Service tests
    /// </summary>
    public class ConnectionServiceTests
    {
        #region "Connection tests"

        /// <summary>
        /// Verify that the SQL parser correctly detects errors in text
        /// </summary>
        [Fact]
        public void ConnectToDatabaseTest()
        {
            // connect to a database instance 
            var connectionResult = 
                TestObjects.GetTestConnectionService()
                .Connect(TestObjects.GetTestConnectionDetails());

            // verify that a valid connection id was returned
            Assert.True(connectionResult.ConnectionId > 0);
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
            var connectionResult = connectionService.Connect(TestObjects.GetTestConnectionDetails());

            // verify that a valid connection id was returned
            Assert.True(callbackInvoked);
        }

        #endregion
    }
}
