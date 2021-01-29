using System;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.Contracts;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.DataSource
{
    public class KustoClientTests
    {
        [Test]
        public void Constructor_Throws_ArgumentException_For_MissingToken()
        {
            var connectionDetails = new DataSourceConnectionDetails
            {
                UserToken = ""
            };
            
            Assert.Throws<ArgumentException>(() => new KustoClient(connectionDetails, "ownerUri"));
        }

        [Test]
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
        public void Constructor_Creates_Client_With_Valid_AuthenticationType(string authenticationType)
        {
            string clusterName = "https://fake.url.com";
            var connectionDetails = new DataSourceConnectionDetails
            {
                UserToken = "UserToken",
                ServerName = clusterName,
                DatabaseName = "FakeDatabaseName",
                AuthenticationType = authenticationType
            };

            var client = new KustoClient(connectionDetails, "ownerUri");

            Assert.AreEqual(clusterName, client.ClusterName);
            Assert.AreEqual("FakeDatabaseName", client.DatabaseName);
        }
    }
}