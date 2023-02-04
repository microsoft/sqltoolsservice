//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
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
            var dataSourceFactory = new Mock<IDataSourceFactory>();
            var connectedBindingQueue = new ConnectedBindingQueue(dataSourceFactory.Object);
            var connectionKey = connectedBindingQueue.AddConnectionContext(null, false);
            
            Assert.AreEqual(string.Empty, connectionKey);
        }

        [Test]
        public void AddConnectionContext_Returns_ConnectionKey()
        {
            var connectionDetails = new ConnectionDetails();
            var connectionFactory = new Mock<IDataSourceConnectionFactory>();
            var connectionInfo = new ConnectionInfo(connectionFactory.Object, "ownerUri", connectionDetails);
            
            var dataSourceFactory = new Mock<IDataSourceFactory>();
            var connectedBindingQueue = new ConnectedBindingQueue(dataSourceFactory.Object);
            var connectionKey = connectedBindingQueue.AddConnectionContext(connectionInfo, false, "featureName");
            
            Assert.AreEqual("NULL_NULL_NULL_NULL", connectionKey);
        }
        
        [Test]
        public void AddConnectionContext_Sets_BindingContext()
        {
            var connectionDetails = new ConnectionDetails
            {
                AccountToken = "AzureAccountToken" 
            };
            var connectionFactory = new Mock<IDataSourceConnectionFactory>();
            var connectionInfo = new ConnectionInfo(connectionFactory.Object, "ownerUri", connectionDetails);

            var dataSourceFactory = new Mock<IDataSourceFactory>();
            var dataSourceMock = new Mock<IDataSource>();
            dataSourceFactory
                .Setup(x => x.Create(It.IsAny<ConnectionDetails>(), It.IsAny<string>()))
                .Returns(dataSourceMock.Object);

            var connectedBindingQueue =
                new ConnectedBindingQueue(dataSourceFactory.Object);
            var connectionKey =
                connectedBindingQueue.AddConnectionContext(connectionInfo, false, "featureName");
            var bindingContext = connectedBindingQueue.GetOrCreateBindingContext(connectionKey);
            
            Assert.AreEqual(dataSourceMock.Object, bindingContext.DataSource);
            Assert.AreEqual(500, bindingContext.BindingTimeout);
            Assert.AreEqual(true, bindingContext.IsConnected);
        }

        [Test]
        public void RemoveBindingContext_Removes_Context()
        {
            var connectionDetails = new ConnectionDetails();
            var connectionFactory = new Mock<IDataSourceConnectionFactory>();
            var connectionInfo = new ConnectionInfo(connectionFactory.Object, "ownerUri", connectionDetails);
            
            var dataSourceFactory = new Mock<IDataSourceFactory>();
            var connectedBindingQueue = new ConnectedBindingQueue(dataSourceFactory.Object);
            var connectionKey = connectedBindingQueue.AddConnectionContext(connectionInfo, false, "featureName");
            
            connectedBindingQueue.RemoveBindingContext(connectionInfo);
            
            Assert.IsFalse(connectedBindingQueue.BindingContextMap.ContainsKey(connectionKey));
        }
    }
}