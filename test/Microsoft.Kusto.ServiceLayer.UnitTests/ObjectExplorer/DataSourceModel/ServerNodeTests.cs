using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.SqlTools.Extensibility;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.ObjectExplorer.DataSourceModel
{ 
    public class ServerNodeTests
    {
        [Test]
        public void ServerNode_ThrowsException_NullConnectionParam()
        {
            ConnectionCompleteParams param = null;
            var serviceProviderMock = new Mock<IMultiServiceProvider>();

            Assert.Throws<ArgumentNullException>(() => new ServerNode(param, serviceProviderMock.Object, null, new DatabaseMetadata()));
        }
        
        [Test]
        public void ServerNode_ThrowsException_NullConnectionSummary()
        {
            var param = new ConnectionCompleteParams
            {
                ConnectionSummary = null
            };

            var serviceProviderMock = new Mock<IMultiServiceProvider>();

            Assert.Throws<ArgumentNullException>(() => new ServerNode(param, serviceProviderMock.Object, null, new DatabaseMetadata()));
        }
        
        [Test]
        public void ServerNode_ThrowsException_NullServiceProvider()
        {
            var param = new ConnectionCompleteParams
            {
                ConnectionSummary = new ConnectionSummary()
            };

            Assert.Throws<ArgumentNullException>(() => new ServerNode(param, null, null, new DatabaseMetadata()));
        }

        [TestCase(CommonConstants.MasterDatabaseName, "User1", "Server Name (SQL Server 2020 - User1)")]
        [TestCase(CommonConstants.MasterDatabaseName, "", "Server Name (SQL Server 2020)")]
        [TestCase(CommonConstants.MsdbDatabaseName, "User2", "Server Name (SQL Server 2020 - User2)")]
        [TestCase(CommonConstants.MsdbDatabaseName, "", "Server Name (SQL Server 2020)")]
        [TestCase(CommonConstants.ModelDatabaseName, "User3", "Server Name (SQL Server 2020 - User3)")]
        [TestCase(CommonConstants.ModelDatabaseName, "", "Server Name (SQL Server 2020)")]
        [TestCase(CommonConstants.TempDbDatabaseName, "User4", "Server Name (SQL Server 2020 - User4)")]
        [TestCase(CommonConstants.TempDbDatabaseName, "", "Server Name (SQL Server 2020)")]
        [TestCase("Database1", "User5", "Server Name (SQL Server 2020 - User5, Database1)")]
        [TestCase("Database1", "", "Server Name (SQL Server 2020 - Database1)")]
        public void GetConnectionLabel_Sets_Label_For_Params(string databaseName, string userName, string expected)
        {
            var connectionParams = new ConnectionCompleteParams
            {
                ConnectionSummary = new ConnectionSummary
                {
                    DatabaseName = databaseName,
                    UserName = userName,
                    ServerName = "Server Name"
                },
                ServerInfo = new ServerInfo
                {
                    ServerVersion = "2020"
                }
            };
            
            var serviceProviderMock = new Mock<IMultiServiceProvider>();
            var dataSourceMock = new Mock<IDataSource>();
            var metadata = new DataSourceObjectMetadata();

            var serverNode = new ServerNode(connectionParams, serviceProviderMock.Object, dataSourceMock.Object,
                metadata);

            var nodeInfo = serverNode.ToNodeInfo();
            Assert.AreEqual(expected, nodeInfo.Label);
        }

        [Test]
        public void Refresh_Calls_DataSourceRefresh()
        {
            var connectionParams = new ConnectionCompleteParams
            {
                ConnectionSummary = new ConnectionSummary
                {
                    UserName = "UserName",
                    DatabaseName = "DatabaseName",
                    ServerName = "ServerName"
                },
                ServerInfo = new ServerInfo
                {
                    ServerVersion = "Version"
                }
            };
            
            var serviceProviderMock = new Mock<IMultiServiceProvider>();
            var dataSourceMock = new Mock<IDataSource>();
            var metadata = new DataSourceObjectMetadata();

            var serverNode = new ServerNode(connectionParams, serviceProviderMock.Object, dataSourceMock.Object,
                metadata);

            serverNode.Refresh(CancellationToken.None);

            dataSourceMock.Verify(x => x.Refresh(It.IsAny<DataSourceObjectMetadata>()), Times.Once());
        }

        [Test]
        public void Refresh_Returns_Children()
        {
            var childMetadata = new DataSourceObjectMetadata
            {
                MetadataTypeName = DataSourceMetadataType.Database.ToString(),
                MetadataType = DataSourceMetadataType.Database,
                Name = "Database1"
            };
            
            var connectionParams = new ConnectionCompleteParams
            {
                ConnectionSummary = new ConnectionSummary
                {
                    UserName = "UserName",
                    DatabaseName = "DatabaseName",
                    ServerName = "ServerName"
                },
                ServerInfo = new ServerInfo
                {
                    ServerVersion = "Version"
                }
            };
            
            var serviceProviderMock = new Mock<IMultiServiceProvider>();
            var dataSourceMock = new Mock<IDataSource>();
            dataSourceMock.Setup(x => x.GetChildObjects(It.IsAny<DataSourceObjectMetadata>(), It.IsAny<bool>()))
                .Returns(new List<DataSourceObjectMetadata> {childMetadata});

            var parentMetadata = new DataSourceObjectMetadata();
            var serverNode = new ServerNode(connectionParams, serviceProviderMock.Object, dataSourceMock.Object,
                parentMetadata);

            var children = serverNode.Refresh(CancellationToken.None);
            Assert.AreEqual(1, children.Count);
            var child = children.First();
            
            Assert.AreEqual(childMetadata.MetadataTypeName, child.NodeType);
            Assert.AreEqual(NodeTypes.Database, child.NodeTypeId);
            Assert.AreEqual(childMetadata.Name, child.NodeValue);
        }
    }
}