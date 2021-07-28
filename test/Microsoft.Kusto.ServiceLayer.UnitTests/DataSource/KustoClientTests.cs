using System;
using Microsoft.Kusto.ServiceLayer.DataSource.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource.Kusto;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.DataSource
{
    public class KustoClientTests
    {
        [TestCase("dstsAuth")]	
        [TestCase("AzureMFA")]	
        public void Constructor_Throws_ArgumentException_For_MissingToken(string authType)	
        {	
            var connectionDetails = new DataSourceConnectionDetails	
            {	
                UserToken = "",
                AuthenticationType = authType
            };	

            Assert.Throws<ArgumentException>(() => new KustoClient(connectionDetails, "ownerUri"));	
        }
        
        [Test]
        [Ignore("This should be moved to an integration test since Kusto Client calls Query")]
        public void Constructor_Sets_ClusterName_With_DefaultDatabaseName()
        {
            string clusterName = "https://fake.url.com";
            var connectionDetails = new DataSourceConnectionDetails
            {
                UserToken = "UserToken",
                ServerName = clusterName,
                DatabaseName = "",
                AuthenticationType = "AzureMFA"
            };

            var client = new KustoClient(connectionDetails, "ownerUri");

            Assert.AreEqual(clusterName, client.ClusterName);
            Assert.AreEqual("NetDefaultDB", client.DatabaseName);
        }

        [TestCase("dstsAuth")]
        [TestCase("AzureMFA")]
        [TestCase("NoAuth")]
        [TestCase("SqlLogin")]
        public void Constructor_Creates_Client_With_Valid_AuthenticationType(string authenticationType)
        {
            string clusterName = "https://fake.url.com";
            var connectionDetails = new DataSourceConnectionDetails
            {
                UserToken = "UserToken",
                ServerName = clusterName,
                DatabaseName = "FakeDatabaseName",
                AuthenticationType = authenticationType,
                UserName = authenticationType == "SqlLogin" ? "username": null,
                Password = authenticationType == "SqlLogin" ? "password": null
            };

            var client = new KustoClient(connectionDetails, "ownerUri");

            Assert.AreEqual(clusterName, client.ClusterName);
            Assert.AreEqual("FakeDatabaseName", client.DatabaseName);
        }
    }
}