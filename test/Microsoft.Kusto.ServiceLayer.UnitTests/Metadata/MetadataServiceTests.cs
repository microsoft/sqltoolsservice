using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.Metadata;
using Microsoft.Kusto.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Metadata
{
    public class MetadataServiceTests
    {
        [Test]
        public async Task HandleMetadataListRequest_Sets_MetadataListTask()
        {
            var serviceHostMock = new Mock<IProtocolEndpoint>();
            var connectionManagerMock = new Mock<IConnectionManager>();
            var connectionFactoryMock = new Mock<IDataSourceConnectionFactory>();
            var requestContextMock = new Mock<RequestContext<MetadataQueryResult>>();
            requestContextMock.Setup(x => x.SendResult(It.IsAny<MetadataQueryResult>())).Returns(Task.CompletedTask);
            
            var dataSourceMock = new Mock<IDataSource>();
            dataSourceMock.Setup(x => x.GetChildObjects(It.IsAny<DataSourceObjectMetadata>(), It.IsAny<bool>()))
                .Returns(new List<DataSourceObjectMetadata> {new DataSourceObjectMetadata {PrettyName = "TestName"}});
            
            var dataSourceFactoryMock = new Mock<IDataSourceFactory>();
            dataSourceFactoryMock.Setup(x => x.Create(It.IsAny<ConnectionDetails>(), It.IsAny<string>()))
                .Returns(dataSourceMock.Object);
            
            var reliableDataSource = new ReliableDataSourceConnection(new ConnectionDetails(), RetryPolicyFactory.NoRetryPolicy,
                RetryPolicyFactory.NoRetryPolicy, dataSourceFactoryMock.Object, "");

            var connectionDetails = new ConnectionDetails
            {
                ServerName = "ServerName",
                DatabaseName = "DatabaseName"
            };
            var connectionInfo = new ConnectionInfo(connectionFactoryMock.Object, "", connectionDetails);
            connectionInfo.AddConnection(ConnectionType.Default, reliableDataSource);
            
            connectionManagerMock.Setup(x => x.TryGetValue(It.IsAny<string>(), out connectionInfo));
            
            var metadataService = new MetadataService();
            metadataService.InitializeService(serviceHostMock.Object, connectionManagerMock.Object);

            await metadataService.HandleMetadataListRequest(new MetadataQueryParams(), requestContextMock.Object);

            requestContextMock.Verify(x => x.SendResult(It.Is<MetadataQueryResult>(result => result.Metadata.First().Name == "TestName")),
                Times.Once());
        }
    }
}