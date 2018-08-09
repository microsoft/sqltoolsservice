//
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
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Connection
{
    /// <summary>
    /// Tests for the ServiceHost Connection Service tests that require a live database connection
    /// </summary>
    public class ConnectionServiceTests
    {
        [Fact]
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
                Assert.Equal(1, connectionInfo.CountConnections);
                Assert.Equal(1, service.OwnerToConnectionMap.Count);

                // If we run a query
                var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
                Query query = new Query(Constants.StandardQuery, connectionInfo, new QueryExecutionSettings(), fileStreamFactory);
                query.Execute();
                query.ExecutionTask.Wait();

                // We should see two DbConnections
                Assert.Equal(2, connectionInfo.CountConnections);

                // If we run another query
                query = new Query(Constants.StandardQuery, connectionInfo, new QueryExecutionSettings(), fileStreamFactory);
                query.Execute();
                query.ExecutionTask.Wait();

                // We should still have 2 DbConnections
                Assert.Equal(2, connectionInfo.CountConnections);

                // If we disconnect, we should remain in a consistent state to do it over again
                // e.g. loop and do it over again
                service.Disconnect(new DisconnectParams() { OwnerUri = connectionInfo.OwnerUri });

                // We should be left with an empty connection map
                Assert.Equal(0, service.OwnerToConnectionMap.Count);
            }
        }

        [Fact]
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
                    Assert.Equal(connection.Database, initialDatabaseName);
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
                    Assert.Equal(connection.Database, newDatabaseName);
                }
            }
        }

        /// <summary>
        /// Test HandleGetConnectionStringRequest
        /// </summary>
        [Fact]
        public async void GetCurrentConnectionStringTest()
        {
            // If we make a connection to a live database 
            ConnectionService service = ConnectionService.Instance;
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            var requestContext = new Mock<SqlTools.Hosting.Protocol.RequestContext<string>>();

            requestContext.Setup(x => x.SendResult(It.Is<string>((connectionString) => connectionString.Contains("Password=" + ConnectionService.PasswordPlaceholder))))
                .Returns(Task.FromResult(new object()));

            var requestParams = new GetConnectionStringParams()
            {
                OwnerUri = result.ConnectionInfo.OwnerUri,
                IncludePassword = false
            };

            await service.HandleGetConnectionStringRequest(requestParams, requestContext.Object);
            requestContext.VerifyAll();
        }
        
        /// <summary>
        /// Test ParseConnectionString
        /// </summary>
        [Fact]
        public void ParseConnectionStringTest()
        {
            // If we make a connection to a live database 
            ConnectionService service = ConnectionService.Instance;
            
            var connectionString = "Server=tcp:{servername},1433;Initial Catalog={databasename};Persist Security Info=False;User ID={your_username};Password={your_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            var details = service.ParseConnectionString(connectionString);

            Assert.Equal("tcp:{servername},1433", details.ServerName);
            Assert.Equal("{databasename}", details.DatabaseName);
            Assert.Equal("{your_username}", details.UserName);
            Assert.Equal("{your_password}", details.Password);
            Assert.Equal(false, details.PersistSecurityInfo);
            Assert.Equal(false, details.MultipleActiveResultSets);
            Assert.Equal(true, details.Encrypt);
            Assert.Equal(false, details.TrustServerCertificate);
            Assert.Equal(30, details.ConnectTimeout);
        }
    }
}
