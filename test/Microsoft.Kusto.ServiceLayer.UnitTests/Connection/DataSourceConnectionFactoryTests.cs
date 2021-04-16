using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.SqlTools.Hosting.Contracts.Connection;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Connection
{
    public class DataSourceConnectionFactoryTests
    {
        [Test]
        public void CreateDataSourceConnection_Returns_Connection()
        {
            var dataSourceFactoryMock = new Mock<IDataSourceFactory>(); 
            var connectionFactory = new DataSourceConnectionFactory(dataSourceFactoryMock.Object);
            var connection = connectionFactory.CreateDataSourceConnection(new ConnectionDetails(), "");
            
            Assert.IsNotNull(connection);
        }
    }
}