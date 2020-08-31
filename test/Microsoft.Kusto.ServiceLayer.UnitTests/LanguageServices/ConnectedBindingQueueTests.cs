using System;
using System.Collections.Generic;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.LanguageServices
{
    public class ConnectedBindingQueueTests
    {
        private static IEnumerable<Tuple<ConnectionDetails, string>> ConnectionDetailsSource()
        {
            var results = new List<Tuple<ConnectionDetails, string>>();
            
            var details1 = new ConnectionDetails
            {
                ServerName = "ServerName",
                DatabaseName = "DatabaseName",
                UserName = "UserName",
                AuthenticationType = "AuthenticationType",
                DatabaseDisplayName = "DisplayName",
                GroupId = "GroupId"
            };
            
            results.Add(new Tuple<ConnectionDetails, string>(details1, "ServerName_DatabaseName_UserName_AuthenticationType_DisplayName_GroupId"));
            
            var details2 = new ConnectionDetails
            {
                ServerName = null,
                DatabaseName = null,
                UserName = null,
                AuthenticationType = null,
                DatabaseDisplayName = "",
                GroupId = ""
            };

            results.Add(new Tuple<ConnectionDetails, string>(details2, "NULL_NULL_NULL_NULL"));
            
            var details3 = new ConnectionDetails
            {
                ServerName = null,
                DatabaseName = null,
                UserName = null,
                AuthenticationType = null,
                DatabaseDisplayName = null,
                GroupId = null
            };
            
            results.Add(new Tuple<ConnectionDetails, string>(details3, "NULL_NULL_NULL_NULL"));
            
            return results;
        }
        
        [TestCaseSource(nameof(ConnectionDetailsSource))]
        public void GetConnectionContextKey_Returns_Key(Tuple<ConnectionDetails, string> tuple)
        {
            var contextKey = ConnectedBindingQueue.GetConnectionContextKey(tuple.Item1);
            Assert.AreEqual(tuple.Item2, contextKey);
        }

        [Test]
        public void AddConnectionContext_Returns_EmptyString_For_NullConnectionInfo()
        {
            var connectionOpenerMock = new Mock<ISqlConnectionOpener>();
            var dataSourceFactory = new Mock<IDataSourceFactory>();
            var connectedBindingQueue = new ConnectedBindingQueue(connectionOpenerMock.Object, dataSourceFactory.Object);
            var connectionKey = connectedBindingQueue.AddConnectionContext(null, false);
            
            Assert.AreEqual(string.Empty, connectionKey);
        }

        [Test]
        public void AddConnectionContext_Returns_ConnectionKey()
        {
            var connectionDetails = new ConnectionDetails();
            var connectionFactory = new Mock<IDataSourceConnectionFactory>();
            var connectionInfo = new ConnectionInfo(connectionFactory.Object, "ownerUri", connectionDetails);
            
            var connectionOpenerMock = new Mock<ISqlConnectionOpener>();
            var dataSourceFactory = new Mock<IDataSourceFactory>();
            var connectedBindingQueue = new ConnectedBindingQueue(connectionOpenerMock.Object, dataSourceFactory.Object);
            var connectionKey = connectedBindingQueue.AddConnectionContext(connectionInfo, false, "featureName");
            
            Assert.AreEqual("NULL_NULL_NULL_NULL", connectionKey);
        }
        
        [TestCase(true)]
        [TestCase(false)]
        public void AddConnectionContext_Sets_BindingContext(bool needsMetadata)
        {
            var connectionDetails = new ConnectionDetails();
            var connectionFactory = new Mock<IDataSourceConnectionFactory>();
            var connectionInfo = new ConnectionInfo(connectionFactory.Object, "ownerUri", connectionDetails);
            
            var connectionOpenerMock = new Mock<ISqlConnectionOpener>();
            var fakeServerConnection = new ServerConnection();
            connectionOpenerMock
                .Setup(x => x.OpenServerConnection(It.IsAny<ConnectionInfo>(), It.IsAny<string>()))
                .Returns(fakeServerConnection);
            
            var dataSourceFactory = new Mock<IDataSourceFactory>();
            var dataSourceMock = new Mock<IDataSource>();
            dataSourceFactory
                .Setup(x => x.Create(It.IsAny<DataSourceType>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(dataSourceMock.Object);
            
            var connectedBindingQueue = new ConnectedBindingQueue(connectionOpenerMock.Object, dataSourceFactory.Object);
            var connectionKey = connectedBindingQueue.AddConnectionContext(connectionInfo, needsMetadata, "featureName");
            var bindingContext = connectedBindingQueue.GetOrCreateBindingContext(connectionKey);
            
            Assert.AreEqual(fakeServerConnection, bindingContext.ServerConnection);
            Assert.AreEqual(dataSourceMock.Object, bindingContext.DataSource);
            Assert.AreEqual(500, bindingContext.BindingTimeout);
            Assert.AreEqual(true, bindingContext.IsConnected);
            Assert.AreEqual(CasingStyle.Uppercase, bindingContext.MetadataDisplayInfoProvider.BuiltInCasing);
            
            if (needsMetadata)
            {
                Assert.IsNotNull(bindingContext.SmoMetadataProvider);
                Assert.IsNotNull(bindingContext.Binder);
            }
            else
            {
                Assert.IsNull(bindingContext.SmoMetadataProvider);
                Assert.IsNull(bindingContext.Binder);
            }
        }

        [Test]
        public void RemoveBindingContext_Removes_Context()
        {
            var connectionDetails = new ConnectionDetails();
            var connectionFactory = new Mock<IDataSourceConnectionFactory>();
            var connectionInfo = new ConnectionInfo(connectionFactory.Object, "ownerUri", connectionDetails);
            
            var connectionOpenerMock = new Mock<ISqlConnectionOpener>();
            var dataSourceFactory = new Mock<IDataSourceFactory>();
            var connectedBindingQueue = new ConnectedBindingQueue(connectionOpenerMock.Object, dataSourceFactory.Object);
            var connectionKey = connectedBindingQueue.AddConnectionContext(connectionInfo, false, "featureName");
            
            
            connectedBindingQueue.RemoveBindingContext(connectionInfo);
            
            Assert.IsFalse(connectedBindingQueue.BindingContextMap.ContainsKey(connectionKey));
        }
    }
}