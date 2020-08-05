//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    [TestFixture]
    /// <summary>
    /// Tests for ReliableConnection code
    /// </summary>
    public class ReliableConnectionTests
    {
        [Test]
        public void ReliableSqlConnectionUsesAzureToken()
        {
            ConnectionDetails details = TestObjects.GetTestConnectionDetails();
            details.UserName = "";
            details.Password = "";
            string connectionString = ConnectionService.BuildConnectionString(details);
            string azureAccountToken = "testAzureAccountToken";
            RetryPolicy retryPolicy = RetryPolicyFactory.CreateDefaultConnectionRetryPolicy();

            // If I create a ReliableSqlConnection using an azure account token
            var reliableConnection = new ReliableSqlConnection(connectionString, retryPolicy, retryPolicy, azureAccountToken);

            // Then the connection's azureAccountToken gets set
            Assert.AreEqual(azureAccountToken, reliableConnection.GetUnderlyingConnection().AccessToken);
        }
    }
}