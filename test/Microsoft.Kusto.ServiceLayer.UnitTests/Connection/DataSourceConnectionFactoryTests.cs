using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.DataSource;
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
            var connection = connectionFactory.CreateDataSourceConnection("", "");
            
            Assert.IsNotNull(connection);
        }
    }
}