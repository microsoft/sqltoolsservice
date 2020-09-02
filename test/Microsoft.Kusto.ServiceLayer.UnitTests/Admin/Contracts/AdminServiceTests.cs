using Microsoft.Kusto.ServiceLayer.Admin;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Admin.Contracts
{
    public class AdminServiceTests
    {
        [TestCase(null)]
        [TestCase("")]
        public void GetDatabaseInfo_Returns_Null_For_Invalid_DatabaseName(string databaseName)
        {
            var dataSourceConnectionFactory = new Mock<IDataSourceConnectionFactory>();
            var connectionDetails = new ConnectionDetails
            {
                DatabaseName = databaseName 
            };
            var connectionInfo = new ConnectionInfo(dataSourceConnectionFactory.Object, "", connectionDetails); 
            
            var adminService = new AdminService();
            var databaseInfo = adminService.GetDatabaseInfo(connectionInfo);
            Assert.IsNull(databaseInfo);
        }
    }
}