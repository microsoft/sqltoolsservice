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
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Moq;
using NUnit.Framework;

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
            var requestContext = new Mock<SqlTools.Hosting.Protocol.RequestContext<string>>();

            requestContext.Setup(x => x.SendResult(It.Is<string>((connectionString) => connectionString.Contains("Password=" + ConnectionService.PasswordPlaceholder))))
                .Returns(Task.FromResult(new object()));

            var requestParams = new GetConnectionStringParams()
            {
                OwnerUri = result.ConnectionInfo.OwnerUri,
                ConnectionDetails = null,
                IncludePassword = false,
                IncludeApplicationName = true
            };

            await service.HandleGetConnectionStringRequest(requestParams, requestContext.Object);
            requestContext.VerifyAll();

            // validate that the get command doesn't change any connection property and the following get commands work as expected
            requestParams.IncludePassword = true;

            requestContext.Setup(x => x.SendResult(It.Is<string>((connectionString) => connectionString.Contains("Password=" + resultPassword))))
                .Returns(Task.FromResult(new object()));

            await service.HandleGetConnectionStringRequest(requestParams, requestContext.Object);
            requestContext.VerifyAll();
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
            var requestContext = new Mock<SqlTools.Hosting.Protocol.RequestContext<string>>();

            requestContext.Setup(x => x.SendResult(It.Is<string>((connectionString) => !connectionString.Contains("Application Name=" + resultApplicationName))))
                            .Returns(Task.FromResult(new object()));
            var requestParams = new GetConnectionStringParams()
            {
                OwnerUri = result.ConnectionInfo.OwnerUri,
                ConnectionDetails = null,
                IncludePassword = false,
                IncludeApplicationName = false
            };

            await service.HandleGetConnectionStringRequest(requestParams, requestContext.Object);
            requestContext.VerifyAll();
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

            var requestContext = new Mock<SqlTools.Hosting.Protocol.RequestContext<string>>();
            requestContext.Setup(x => x.SendResult(It.Is<string>((connectionString) => connectionString.Contains(expectedConnectionString))))
                            .Returns(Task.FromResult(new object()));                      

            await service.HandleGetConnectionStringRequest(requestParams, requestContext.Object);
            requestContext.VerifyAll();
        }

        [Test]
        public async Task ParseConnectionStringRequest()
        {
            ConnectionService service = ConnectionService.Instance;

            ConnectionDetails result = null;
            bool resultSent = false;
            string error = null;
            bool errorSent = false;
            var requestContext = RequestContextMocks.Create<ConnectionDetails>(r => { result = r; resultSent = true; }).AddErrorHandling((msg, code, data) => { error = msg; errorSent = true; });

            // Validate successful parse
            await service.HandleParseConnectionStringRequest("Server=tcp:{servername},1433;Initial Catalog={databasename};Authentication=ActiveDirectoryInteractive;", requestContext.Object);

            Assert.That(result.ServerName, Is.EqualTo("tcp:{servername},1433"), "Valid connection string should return parsed ConnectionDetails object");
            Assert.That(errorSent, Is.False, "Valid connection string should not return an error");
            Assert.That(error, Is.Null, "Valid connection string should not throw an error");

            // Validate error thrown on unsuccessful parse
            result = null;
            resultSent = false;
            error = null;
            errorSent = false;

            await service.HandleParseConnectionStringRequest("Server=tcp:{servername},1433;Initial Catalog={databasename};Authentication=NotRealAuthType;", requestContext.Object);

            Assert.That(result, Is.Null, "Invalid connection string should not return ConnectionDetails");
            Assert.That(resultSent, Is.False, "Invalid connection string should not return anything");
            Assert.That(error, Is.EqualTo("Invalid value for key 'authentication'."), "Invalid connection string should return error message indicating the issue");
        }

        [Test]
        public async Task BuildConnectionInfoRequest()
        {
            ConnectionService service = ConnectionService.Instance;

            ConnectionDetails result = null;
            bool resultSent = false;
            string error = null;
            bool errorSent = false;
            var requestContext = RequestContextMocks.Create<ConnectionDetails>(r => { result = r; resultSent = true; }).AddErrorHandling((msg, code, data) => { error = msg; errorSent = true; });

            // Validate successful parse
            await service.HandleBuildConnectionInfoRequest("Server=tcp:{servername},1433;Initial Catalog={databasename};Authentication=ActiveDirectoryInteractive;", requestContext.Object);

            Assert.That(result.ServerName, Is.EqualTo("tcp:{servername},1433"), "Valid connection string should return parsed ConnectionDetails object");
            Assert.That(errorSent, Is.False, "Valid connection string should not return an error");
            Assert.That(error, Is.Null, "Valid connection string should not throw an error");

            // Validate null returned instad of error thrown on unsuccessful parse
            result = null;
            resultSent = false;
            error = null;
            errorSent = false;

            await service.HandleBuildConnectionInfoRequest("Server=tcp:{servername},1433;Initial Catalog={databasename};Authentication=NotRealAuthType;", requestContext.Object);

            Assert.That(result, Is.Null, "Invalid connection string response should be null");
            Assert.That(resultSent, Is.True, "Invalid connection string should return null as result");
            Assert.That(errorSent, Is.False, "Invalid connection string should not throw an error (instead, null result should have been sent");
        }
    }
}
