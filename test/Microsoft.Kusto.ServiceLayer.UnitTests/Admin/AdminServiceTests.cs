using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.Admin;
using Microsoft.Kusto.ServiceLayer.Admin.Contracts;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Admin
{
    public class AdminServiceTests
    {
        [TestCase(null)]
        [TestCase("")]
        public async Task HandleGetDatabaseInfoRequest_NoDatabase_Returns_Null(string databaseName)
        {
            var mockServiceHost = new Mock<IProtocolEndpoint>();
            var mockDataSourceFactory = new Mock<IDataSourceConnectionFactory>();
            var connectionInfo = new ConnectionInfo(mockDataSourceFactory.Object, null, new ConnectionDetails { DatabaseName = databaseName });
            var mockConnectionService = new Mock<IConnectionService>();
            mockConnectionService
                .Setup(x => x.TryFindConnection(It.IsAny<string>(), out connectionInfo))
                .Returns(true);
            
            var mockRequestContext = new Mock<RequestContext<GetDatabaseInfoResponse>>();
            var actualResponse = new GetDatabaseInfoResponse();
            mockRequestContext.Setup(x => x.SendResult(It.IsAny<GetDatabaseInfoResponse>()))
                .Callback<GetDatabaseInfoResponse>(actual => actualResponse = actual)
                .Returns(Task.CompletedTask);
            
            var adminService = new AdminService();
            adminService.InitializeService(mockServiceHost.Object, mockConnectionService.Object);
            await adminService.HandleGetDatabaseInfoRequest(new GetDatabaseInfoParams(), mockRequestContext.Object);

            Assert.AreEqual(null, actualResponse.DatabaseInfo);
        }
        
        [Test]
        public async Task HandleGetDatabaseInfoRequest_Returns_DatabaseName()
        {
            var expectedDatabaseInfo = new DatabaseInfo()
            {
                Options = new Dictionary<string, object>
                {
                    { "FakeDatabaseName", "FakeSizeInMB" }
                }
            };
            
            var mockDatasource = new Mock<IDataSource>();
            mockDatasource
                .Setup(x => x.GetDatabaseInfo(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(expectedDatabaseInfo);

            var mockDataSourceFactory = new Mock<IDataSourceFactory>();
            mockDataSourceFactory
                .Setup(x => x.Create(It.IsAny<ConnectionDetails>(), It.IsAny<string>()))
                .Returns(mockDatasource.Object);

            var expectedConnectionDetails = new ConnectionDetails
            {
                DatabaseName = "FakeDatabaseName"
            };
            
            var mockConnectionFactory = new Mock<IDataSourceConnectionFactory>();
            var connectionInfo = new ConnectionInfo(mockConnectionFactory.Object, null, expectedConnectionDetails);
            var connection = new ReliableDataSourceConnection(expectedConnectionDetails, RetryPolicyFactory.NoRetryPolicy,
                RetryPolicyFactory.NoRetryPolicy, mockDataSourceFactory.Object, "");
            connectionInfo.AddConnection(ConnectionType.Default, connection);
            
            var mockConnectionService = new Mock<IConnectionService>();
            mockConnectionService
                .Setup(x => x.TryFindConnection(It.IsAny<string>(), out connectionInfo))
                .Returns(true);
            
            var mockRequestContext = new Mock<RequestContext<GetDatabaseInfoResponse>>();
            var actualResponse = new GetDatabaseInfoResponse();
            mockRequestContext.Setup(x => x.SendResult(It.IsAny<GetDatabaseInfoResponse>()))
                .Callback<GetDatabaseInfoResponse>(actual => actualResponse = actual)
                .Returns(Task.CompletedTask);
            
            var mockServiceHost = new Mock<IProtocolEndpoint>();
            var adminService = new AdminService();
            adminService.InitializeService(mockServiceHost.Object, mockConnectionService.Object);
            await adminService.HandleGetDatabaseInfoRequest(new GetDatabaseInfoParams(), mockRequestContext.Object);

            Assert.AreEqual("FakeDatabaseName", actualResponse.DatabaseInfo.Options.First().Key);
            Assert.AreEqual("FakeSizeInMB", actualResponse.DatabaseInfo.Options.First().Value);
        }
        
        [Test]
        public async Task HandleGetDatabaseInfoRequest_NoConnection_Returns_Null()
        {
            var mockServiceHost = new Mock<IProtocolEndpoint>();
            var mockConnectionService = new Mock<IConnectionService>();
            var mockRequestContext = new Mock<RequestContext<GetDatabaseInfoResponse>>();
            var actualResponse = new GetDatabaseInfoResponse();
            mockRequestContext.Setup(x => x.SendResult(It.IsAny<GetDatabaseInfoResponse>()))
                .Callback<GetDatabaseInfoResponse>(actual => actualResponse = actual)
                .Returns(Task.CompletedTask);

            var adminService = new AdminService();
            adminService.InitializeService(mockServiceHost.Object, mockConnectionService.Object);
            await adminService.HandleGetDatabaseInfoRequest(new GetDatabaseInfoParams(), mockRequestContext.Object);

            Assert.AreEqual(null, actualResponse.DatabaseInfo);
        }

        [Test]
        public async Task HandleDatabaseInfoRequest_ThrowsException_Returns_Error()
        {
            var mockServiceHost = new Mock<IProtocolEndpoint>();
            var mockConnectionService = new Mock<IConnectionService>();
            ConnectionInfo connectionInfo;
            var expectedException = new Exception("Fake Error Message");
            var actualException = new Exception();
            mockConnectionService
                .Setup(x => x.TryFindConnection(It.IsAny<string>(), out connectionInfo))
                .Throws(expectedException);

            var mockRequestContext = new Mock<RequestContext<GetDatabaseInfoResponse>>();

            mockRequestContext
                .Setup(x => x.SendError(It.IsAny<Exception>()))
                .Callback<Exception>(ex => actualException = ex)
                .Returns(Task.CompletedTask);

            var adminService = new AdminService();
            adminService.InitializeService(mockServiceHost.Object, mockConnectionService.Object);
            await adminService.HandleGetDatabaseInfoRequest(new GetDatabaseInfoParams(), mockRequestContext.Object);

            Assert.AreEqual(expectedException.GetType(), actualException.GetType());
            Assert.AreEqual(expectedException.Message, actualException.Message);
        }
    }
}