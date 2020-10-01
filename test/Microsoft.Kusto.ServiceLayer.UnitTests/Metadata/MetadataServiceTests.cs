using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.Metadata;
using Microsoft.Kusto.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.Hosting.Protocol;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Metadata
{
    public class MetadataServiceTests
    {
        [Test]
        public void HandleMetadataListRequest_Sets_MetadataListTask()
        {
            var serviceHostMock = new Mock<IProtocolEndpoint>();
            var connectionManagerMock = new Mock<IConnectionManager>();
            var connectionFactoryMock = new Mock<IDataSourceConnectionFactory>();

            var connectionInfo = new ConnectionInfo(connectionFactoryMock.Object, "", new ConnectionDetails());
            connectionManagerMock.Setup(x => x.TryFindConnection(It.IsAny<string>(), out connectionInfo));
            
            var metadataService = new MetadataService();
            metadataService.InitializeService(serviceHostMock.Object, connectionManagerMock.Object);
            
            Assert.IsNull(metadataService.MetadataListTask);
            
            var task = metadataService.HandleMetadataListRequest(new MetadataQueryParams(),
                new RequestContext<MetadataQueryResult>());
            task.Wait();
            
            Assert.IsNotNull(metadataService.MetadataListTask);
        }
    }
}