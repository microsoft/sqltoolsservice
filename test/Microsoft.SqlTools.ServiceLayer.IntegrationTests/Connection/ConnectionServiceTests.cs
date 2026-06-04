//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;
using StreamJsonRpc;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Connection
{
    /// <summary>
    /// Tests for the ServiceHost Connection Service tests that require a live database connection
    /// </summary>
    public class ConnectionServiceTests
    {
        [Test]
        public void RunningMultipleQueriesCreatesOnlyOneConnection()
        {
            // Connect/disconnect twice to ensure reconnection can occur
            ConnectionService service = ConnectionService.Instance;
            service.OwnerToConnectionMap.Clear();
            for (int i = 0; i < 2; i++)
            {
                var result = LiveConnectionHelper.InitLiveConnectionInfo();
                ConnectionInfo connectionInfo = result.ConnectionInfo;
                string uri = connectionInfo.OwnerUri;

                // We should see one ConnectionInfo and one DbConnection
                Assert.AreEqual(1, connectionInfo.CountConnections);
                Assert.AreEqual(1, service.OwnerToConnectionMap.Count);

                // If we run a query
                var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
                Query query = new Query(Constants.StandardQuery, connectionInfo, new QueryExecutionSettings(), fileStreamFactory);
                query.Execute();
                query.ExecutionTask.Wait();

                // We should see 1 DbConnections
                Assert.AreEqual(1, connectionInfo.CountConnections);

                // If we run another query
                query = new Query(Constants.StandardQuery, connectionInfo, new QueryExecutionSettings(), fileStreamFactory);
                query.Execute();
                query.ExecutionTask.Wait();

                // We should see 1 DbConnections
                Assert.AreEqual(1, connectionInfo.CountConnections);

                // If we disconnect, we should remain in a consistent state to do it over again
                // e.g. loop and do it over again
                service.Disconnect(new DisconnectParams() { OwnerUri = connectionInfo.OwnerUri });

                // We should be left with an empty connection map
                Assert.AreEqual(0, service.OwnerToConnectionMap.Count);
            }
        }

        [Test]
        public void DatabaseChangesAffectAllConnections()
        {
            // If we make a connection to a live database 
            ConnectionService service = ConnectionService.Instance;
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            ConnectionInfo connectionInfo = result.ConnectionInfo;
            ConnectionDetails details = connectionInfo.ConnectionDetails;
            string initialDatabaseName = details.DatabaseName;
            string uri = connectionInfo.OwnerUri;
            string newDatabaseName = "tempdb";
            string changeDatabaseQuery = "use " + newDatabaseName;

            // Then run any query to create a query DbConnection
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Query query = new Query(Constants.StandardQuery, connectionInfo, new QueryExecutionSettings(), fileStreamFactory);
            query.Execute();
            query.ExecutionTask.Wait();

            // All open DbConnections (Query and Default) should have initialDatabaseName as their database
            foreach (DbConnection connection in connectionInfo.AllConnections)
            {
                if (connection != null && connection.State == ConnectionState.Open)
                {
                    Assert.AreEqual(connection.Database, initialDatabaseName);
                }
            }

            // If we run a query to change the database
            query = new Query(changeDatabaseQuery, connectionInfo, new QueryExecutionSettings(), fileStreamFactory);
            query.Execute();
            query.ExecutionTask.Wait();

            // All open DbConnections (Query and Default) should have newDatabaseName as their database
            foreach (DbConnection connection in connectionInfo.AllConnections)
            {
                if (connection != null && connection.State == ConnectionState.Open)
                {
                    Assert.AreEqual(connection.Database, newDatabaseName);
                }
            }
        }

        /// <summary>
        /// Test HandleGetConnectionStringRequest
        /// </summary>
        [Test]
        public async Task GetCurrentConnectionStringTest()
        {
            // If we make a connection to a live database 
            ConnectionService service = ConnectionService.Instance;
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            var resultPassword = result.ConnectionInfo.ConnectionDetails.Password;

            var requestParams = new GetConnectionStringParams()
            {
                OwnerUri = result.ConnectionInfo.OwnerUri,
                ConnectionDetails = null,
                IncludePassword = false,
                IncludeApplicationName = true
            };

            string connectionString = await service.HandleGetConnectionStringRequest(requestParams);
            Assert.True(connectionString.Contains("Password=" + ConnectionService.PasswordPlaceholder));

            // validate that the get command doesn't change any connection property and the following get commands work as expected
            requestParams.IncludePassword = true;

            connectionString = await service.HandleGetConnectionStringRequest(requestParams);
            Assert.True(connectionString.Contains("Password=" + resultPassword));
        }

        /// <summary>
        /// Test HandleGetConnectionStringRequest
        /// When IncludeApplicationName is set to false the connection string should not contain the application name
        /// </summary>
        [Test]
        public async Task GetCurrentConnectionStringTestwithoutApplicationName()
        {
            // If we make a connection to a live database 
            ConnectionService service = ConnectionService.Instance;
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            var resultApplicationName = result.ConnectionInfo.ConnectionDetails.ApplicationName;

            var requestParams = new GetConnectionStringParams()
            {
                OwnerUri = result.ConnectionInfo.OwnerUri,
                ConnectionDetails = null,
                IncludePassword = false,
                IncludeApplicationName = false
            };

            string connectionString = await service.HandleGetConnectionStringRequest(requestParams);
            Assert.False(connectionString.Contains("Application Name=" + resultApplicationName));
        }

        /// <summary>
        /// Test HandleGetConnectionStringRequest
        /// Using connection details to build connection string
        /// </summary>
        [Test]
        public async Task GetCurrentConnectionStringTestWithConnectionDetails()
        {
            // If we make a connection to a live database 
            ConnectionService service = ConnectionService.Instance;
            var requestParams = new GetConnectionStringParams();
            requestParams.OwnerUri = null;
            requestParams.ConnectionDetails = new ConnectionDetails() 
            {
                ServerName = "testServer", 
                DatabaseName = "testDatabase", 
                UserName = "sa", 
                Password = "[placeholder]", 
                ApplicationName = "sqlops-connection-string"
            };
            requestParams.IncludePassword = true;
            requestParams.IncludeApplicationName = true; 
            
            // get the expected connection string from the connection details being passed to ConnectionService
            string expectedConnectionString = ConnectionService.CreateConnectionStringBuilder(requestParams.ConnectionDetails).ToString();

            string connectionString = await service.HandleGetConnectionStringRequest(requestParams);
            Assert.True(connectionString.Contains(expectedConnectionString));
        }

        [Test]
        public async Task ParseConnectionStringRequest()
        {
            ConnectionService service = ConnectionService.Instance;

            // Validate successful parse
            ConnectionDetails result = await service.HandleParseConnectionStringRequest("Server=tcp:{servername},1433;Initial Catalog={databasename};Authentication=ActiveDirectoryInteractive;");

            Assert.That(result.ServerName, Is.EqualTo("tcp:{servername},1433"), "Valid connection string should return parsed ConnectionDetails object");

            // Validate error thrown on unsuccessful parse
            LocalRpcException ex = Assert.ThrowsAsync<LocalRpcException>(async () =>
                await service.HandleParseConnectionStringRequest("Server=tcp:{servername},1433;Initial Catalog={databasename};Authentication=NotRealAuthType;"));
            Assert.That(ex.Message, Is.EqualTo("Invalid value for key 'authentication'."));
        }

        [Test]
        public async Task BuildConnectionInfoRequest()
        {
            ConnectionService service = ConnectionService.Instance;

            // Validate successful parse
            ConnectionDetails result = await service.HandleBuildConnectionInfoRequest("Server=tcp:{servername},1433;Initial Catalog={databasename};Authentication=ActiveDirectoryInteractive;");

            Assert.That(result.ServerName, Is.EqualTo("tcp:{servername},1433"), "Valid connection string should return parsed ConnectionDetails object");

            // Validate null returned instad of error thrown on unsuccessful parse
            result = await service.HandleBuildConnectionInfoRequest("Server=tcp:{servername},1433;Initial Catalog={databasename};Authentication=NotRealAuthType;");

            Assert.That(result, Is.Null, "Invalid connection string response should be null");
        }
    }
}
