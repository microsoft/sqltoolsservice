//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection
{
    /// <summary>
    /// Tests for ReliableConnection code
    /// </summary>
    public class ReliableConnectionTests
    {
        [Fact]
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
            Assert.Equal(azureAccountToken, reliableConnection.GetUnderlyingConnection().AccessToken);
        }
    }
}