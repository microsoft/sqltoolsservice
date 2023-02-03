//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Data.SqlClient;
using NUnit.Framework;
using Moq;


namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Connection {

    [TestFixture]
    /// <summary>
    /// Tests for ConnectionDetails Class
    /// </summary>
    public class AuthenticationProviderTests {

        // Create Public Client Application
        public SqlAuthenticationParameters CreateMockSqlAuthenticationParameters() {
            private static ConnectionDetails details = new ConnectionDetails()
            {
                UserName = "user",
                Password = "password",
                DatabaseName = "msdb",
                ServerName = "serverName"
            };
            ConnectionInfo connectionInfo = new ConnectionInfo(null, null, details);
            var connectionString = ConnectionService.BuildConnectionString(connectionInfo.ConnectionDetails);
            SqlConnection sqlConn = new SqlConnection(connectionString);
            // Need to mock SqlAuthenticationParameters
            // var sqlAuthenticationMethod = SqlAuthenticationMethod.ActiveDirectoryInteractive;
            // var systemGuid = new System.Guid();
            // SqlAuthenticationParameters sqlAuthenticationParameters = new SqlAuthenticationParameters(sqlAuthenticationMethod, "servername", "databasename", "resource", "authority", "userId", "password", systemGuid, 30);
            var sqlAuthenticationParameters = new Mock<SqlAuthenticationParameters>{ };
        
        
        }
        
        public SqlAuthenticationToken TestAcquireTokenAsync() {
            
            throw new System.NotImplementedException();
        }
    }

}