using System;
using System.Collections.Generic;
using Microsoft.Kusto.ServiceLayer.DataSource;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.DataSource
{
    public class DataSourceFactoryTests
    {
        [TestCase(typeof(ArgumentNullException), "", "AzureAccountToken")]
        [TestCase(typeof(ArgumentNullException), "ConnectionString", "")]
        [TestCase(typeof(ArgumentException), "ConnectionString", "AzureAccountToken")]
        public void Create_Throws_Exceptions_For_InvalidParams(Type exceptionType,
            string connectionString,
            string azureAccountToken)
        {
            var dataSourceFactory = new DataSourceFactory();
            Assert.Throws(exceptionType,
                () => dataSourceFactory.Create(DataSourceType.None, connectionString, azureAccountToken));
        }

        [Test]
        public void GetDefaultAutoComplete_Throws_ArgumentException_For_InvalidDataSourceType()
        {
            var dataSourceFactory = new DataSourceFactory();
            Assert.Throws<ArgumentException>(() =>
                dataSourceFactory.GetDefaultAutoComplete(DataSourceType.None, null, null));
        }

        [Test]
        public void GetDefaultSemanticMarkers_Throws_ArgumentException_For_InvalidDataSourceType()
        {
            var dataSourceFactory = new DataSourceFactory();
            Assert.Throws<ArgumentException>(() =>
                dataSourceFactory.GetDefaultSemanticMarkers(DataSourceType.None, null, null, null));
        }

        [Test]
        public void ConvertToServerinfoFormat_Throws_ArgumentException_For_InvalidDataSourceType()
        {
            var dataSourceFactory = new DataSourceFactory();
            Assert.Throws<ArgumentException>(() =>
                dataSourceFactory.ConvertToServerinfoFormat(DataSourceType.None, null));
        }

        [Test]
        public void ConvertToServerinfoFormat_Returns_ServerInfo_With_Options()
        {
            var diagnosticsInfo = new DiagnosticsInfo
            {
                Options = new Dictionary<string, object>
                {
                    {"Key", "Object"}
                }
            };
            
            var dataSourceFactory = new DataSourceFactory();
            var serverInfo = dataSourceFactory.ConvertToServerinfoFormat(DataSourceType.Kusto, diagnosticsInfo);

            Assert.IsNotNull(serverInfo.Options);
            Assert.AreEqual(diagnosticsInfo.Options["Key"], serverInfo.Options["Key"]);
        }
    }
}