﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
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

                // We should see two DbConnections
                Assert.AreEqual(2, connectionInfo.CountConnections);

                // If we run another query
                query = new Query(Constants.StandardQuery, connectionInfo, new QueryExecutionSettings(), fileStreamFactory);
                query.Execute();
                query.ExecutionTask.Wait();

                // We should still have 2 DbConnections
                Assert.AreEqual(2, connectionInfo.CountConnections);

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
        public async Task GetCurrentConnectionStringTestwithConnectionDetails()
        {
            // If we make a connection to a live database 
            ConnectionService service = ConnectionService.Instance;
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            var resultConnectionDetails = result.ConnectionInfo.ConnectionDetails;
            var requestContext = new Mock<SqlTools.Hosting.Protocol.RequestContext<string>>();

            requestContext.Setup(x => x.SendResult(It.Is<string>((connectionString) => connectionString.Contains(resultConnectionDetails.ToString()))))
                            .Returns(Task.FromResult(new object()));
            var requestParams = new GetConnectionStringParams()
            {
                OwnerUri = null,
                ConnectionDetails = {ServerName = "testServer", DatabaseName = "testDatabase", UserName = "sa", Password = "password", ApplicationName = "TestApp"},
                IncludePassword = true,
                IncludeApplicationName = true
            };

            await service.HandleGetConnectionStringRequest(requestParams, requestContext.Object);
            requestContext.VerifyAll();
        }
    }
}
