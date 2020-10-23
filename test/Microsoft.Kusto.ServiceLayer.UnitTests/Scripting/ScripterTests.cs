using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.Scripting;
using Microsoft.Kusto.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Scripting
{
    public class ScripterTests
    {
        [Test]
        public void SelectFromTableOrView_Returns_SelectQuery()
        {
            var mockDataSource = new Mock<IDataSource>();
            var urn = new Urn(@"Server[@Name = 'SERVER']/Database[@Name = 'quoted''db']/Table[@Name = 'quoted''Name' and @Schema = 'quoted''Schema']");
            var scripter = new Scripter();
            var result = scripter.SelectFromTableOrView(urn);
            
            Assert.AreEqual("[@\"quoted'Name\"]\n | take 10", result);
        }

        [Test]
        public void AlterFunction()
        {
            var expected = "AlterScript";
            
            var mockDataSource = new Mock<IDataSource>();
            mockDataSource.Setup(x => x.GenerateAlterFunctionScript(It.IsAny<string>())).Returns(expected);
            
            var dataSourceFactoryMock = new Mock<IDataSourceFactory>();
            dataSourceFactoryMock.Setup(x =>
                    x.Create(DataSourceType.Kusto, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RetryPolicy>()))
                .Returns(mockDataSource.Object);
            var reliableDataSource = new ReliableDataSourceConnection("", RetryPolicyFactory.NoRetryPolicy,
                RetryPolicyFactory.NoRetryPolicy, "", dataSourceFactoryMock.Object, "");

            var scriptingObject = new ScriptingObject
            {
                Name = "Name(a:int, b: int)"
            };
            var scripter = new Scripter();
            var result = scripter.AlterFunction(reliableDataSource, scriptingObject);
            
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void ExecuteFunction()
        {
            var expected = "ExecuteScript";
            var mockDataSource = new Mock<IDataSource>();
            mockDataSource.Setup(x => x.GenerateExecuteFunctionScript(It.IsAny<string>())).Returns(expected);
            
            var dataSourceFactoryMock = new Mock<IDataSourceFactory>();
            dataSourceFactoryMock.Setup(x =>
                    x.Create(DataSourceType.Kusto, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RetryPolicy>()))
                .Returns(mockDataSource.Object);
            var reliableDataSource = new ReliableDataSourceConnection("", RetryPolicyFactory.NoRetryPolicy,
                RetryPolicyFactory.NoRetryPolicy, "", dataSourceFactoryMock.Object, "");

            var scriptingObject = new ScriptingObject
            {
                Name = "Name(a:int, b: int)"
            };
            var scripter = new Scripter();
            var result = scripter.ExecuteFunction(reliableDataSource, scriptingObject);
            
            Assert.AreEqual(expected, result);
        }
    }
}