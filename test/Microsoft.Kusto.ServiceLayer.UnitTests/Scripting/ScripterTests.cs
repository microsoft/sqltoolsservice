using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.Scripting;
using Microsoft.Kusto.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Scripting
{
    public class ScripterTests
    {
        [Test]
        public void SelectFromTableOrView_Returns_SelectQuery()
        {
            var scriptingObject = new ScriptingObject
            {
                Name = "My Table",
                Schema = "dbo",
                Type = "Table"
            };
            
            var scripter = new Scripter();
            var result = scripter.SelectFromTableOrView(scriptingObject);
            
            Assert.AreEqual("[@\"My Table\"]\n | take 10", result);
        }

        [Test]
        public void AlterFunction()
        {
            var expected = "AlterScript";
            var mockDataSource = new Mock<IDataSource>();
            mockDataSource.Setup(x => x.GenerateAlterFunctionScript(It.IsAny<string>())).Returns(expected);
            
            var scriptingObject = new ScriptingObject
            {
                Name = "Name(a:int, b: int)"
            };
            var scripter = new Scripter();
            var result = scripter.AlterFunction(mockDataSource.Object, scriptingObject);
            
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void ExecuteFunction()
        {
            var expected = "ExecuteScript";
            var mockDataSource = new Mock<IDataSource>();
            mockDataSource.Setup(x => x.GenerateExecuteFunctionScript(It.IsAny<string>())).Returns(expected);
            
            var scriptingObject = new ScriptingObject
            {
                Name = "Name(a:int, b: int)"
            };
            var scripter = new Scripter();
            var result = scripter.ExecuteFunction(mockDataSource.Object, scriptingObject);
            
            Assert.AreEqual(expected, result);
        }
    }
}