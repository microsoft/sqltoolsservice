using System;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.ObjectExplorer
{
    public class ObjectExplorerServiceTests
    {
        [Test]
        public async Task HandleCreateSessionRequest_ThrowsException_SendsError()
        {
            var mockConnectedBindingQueue = new Mock<IConnectedBindingQueue>();
            var mockConnectionService = new Mock<IConnectionService>();
            var mockServiceProvider = new Mock<IMultiServiceProvider>();
            mockServiceProvider
                .Setup(x => x.GetService<IConnectionService>())
                .Returns(mockConnectionService.Object);
            
            var mockServiceHost = new Mock<IProtocolEndpoint>();

            Exception exceptionSent = null;
            var mockRequestContext = new Mock<RequestContext<CreateSessionResponse>>();
            mockRequestContext
                .Setup(x => x.SendError(It.IsAny<Exception>()))
                .Callback<Exception>(x => exceptionSent = x)
                .Returns(Task.CompletedTask);
            
            var objectExplorerService = new ObjectExplorerService(mockConnectedBindingQueue.Object);
            objectExplorerService.SetServiceProvider(mockServiceProvider.Object);
            objectExplorerService.InitializeService(mockServiceHost.Object);

            await objectExplorerService.HandleCreateSessionRequest(null, mockRequestContext.Object);
                
            Assert.AreEqual(typeof(ArgumentNullException), exceptionSent.GetType());
            Assert.AreEqual("Value cannot be null. (Parameter 'connectionDetails')", exceptionSent.Message);
        }

        [Test]
        public async Task HandleCreateSessionRequest_ValidParams_CreateSession()
        {
            var mockConnectedBindingQueue = new Mock<IConnectedBindingQueue>();
            
            // this has to be set for ObjectExplorerSession
            var connectionCompleteParams = new ConnectionCompleteParams
            {
                OwnerUri = "FakeOwnerUri",
                ConnectionId = Guid.Empty.ToString(),
                ServerInfo = new ServerInfo(),
                ConnectionSummary = new ConnectionSummary
                {
                    ServerName = "FakeServerName",
                }
            };
            
            var mockConnectionService = new Mock<IConnectionService>();
            mockConnectionService
                .Setup(x => x.Connect(It.IsAny<ConnectParams>()))
                .Returns(Task.FromResult(connectionCompleteParams));

            var expectedConnectionDetails = new ConnectionDetails
            {
                DatabaseName = "FakeDatabaseName"
            };

            var mockDatasource = new Mock<IDataSource>();
            mockDatasource.Setup(x => x.ClusterName).Returns("FakeClusterName");
            
            var mockDataSourceFactory = new Mock<IDataSourceFactory>();
            mockDataSourceFactory
                .Setup(x => x.Create(It.IsAny<ConnectionDetails>(), It.IsAny<string>()))
                .Returns(mockDatasource.Object);
            
            var mockConnectionFactory = new Mock<IDataSourceConnectionFactory>();
            var connectionInfo = new ConnectionInfo(mockConnectionFactory.Object, null, expectedConnectionDetails);
            var connection = new ReliableDataSourceConnection(expectedConnectionDetails, RetryPolicyFactory.NoRetryPolicy,
                RetryPolicyFactory.NoRetryPolicy, mockDataSourceFactory.Object, "");
            connectionInfo.AddConnection(ConnectionType.ObjectExplorer, connection);
            
            var mockConnectionManager = new Mock<IConnectionManager>();
            mockConnectionManager
                .Setup(x => x.TryGetValue(It.IsAny<string>(), out connectionInfo))
                .Returns(true);
            
            var mockServiceProvider = new Mock<IMultiServiceProvider>();
            mockServiceProvider
                .Setup(x => x.GetService<IConnectionService>())
                .Returns(mockConnectionService.Object);
            mockServiceProvider
                .Setup(x => x.GetService<IConnectionManager>())
                .Returns(mockConnectionManager.Object);

            EventType<SessionCreatedParameters> serviceHostEventTypeSent = null;
            SessionCreatedParameters serviceHostParamsSent = null;
            var mockServiceHost = new Mock<IProtocolEndpoint>();
            mockServiceHost.Setup(x => x.SendEvent(It.IsAny<EventType<SessionCreatedParameters>>(), It.IsAny<SessionCreatedParameters>()))
                .Callback<EventType<SessionCreatedParameters>, SessionCreatedParameters>((eventType, param) =>
                {
                    serviceHostEventTypeSent = eventType;
                    serviceHostParamsSent = param;
                })
                .Returns(Task.CompletedTask);

            CreateSessionResponse responseSent = null;
            var mockRequestContext = new Mock<RequestContext<CreateSessionResponse>>();
            mockRequestContext
                .Setup(x => x.SendResult(It.IsAny<CreateSessionResponse>()))
                .Callback<CreateSessionResponse>(x => responseSent = x)
                .Returns(Task.CompletedTask);
            
            var objectExplorerService = new ObjectExplorerService(mockConnectedBindingQueue.Object);
            objectExplorerService.SetServiceProvider(mockServiceProvider.Object);
            objectExplorerService.InitializeService(mockServiceHost.Object);

            var connectionDetails = new ConnectionDetails
            {
                ServerName = "FakeServerName",
                DatabaseName = "FakeDatabaseName",
                UserName = "FakeUserName",
                AuthenticationType = "AzureMFA"
            };  
            
            await objectExplorerService.HandleCreateSessionRequest(connectionDetails, mockRequestContext.Object);
                
            Assert.AreEqual("FakeServerName_FakeDatabaseName_FakeUserName_AzureMFA", responseSent.SessionId);
            Assert.AreEqual(CreateSessionCompleteNotification.Type.MethodName, serviceHostEventTypeSent.MethodName);
            Assert.AreEqual(true, serviceHostParamsSent.Success);
            Assert.AreEqual("FakeServerName_FakeDatabaseName_FakeUserName_AzureMFA", serviceHostParamsSent.SessionId);
            Assert.AreEqual(null,serviceHostParamsSent.ErrorMessage);
            
            Assert.IsNotNull(serviceHostParamsSent.RootNode);
            Assert.AreEqual(null, serviceHostParamsSent.RootNode.ErrorMessage);
            Assert.AreEqual(false, serviceHostParamsSent.RootNode.IsLeaf);
            Assert.AreEqual("FakeServerName (Kusto Cluster )", serviceHostParamsSent.RootNode.Label);
            Assert.AreEqual("FakeServerName", serviceHostParamsSent.RootNode.NodePath);
            Assert.AreEqual(null, serviceHostParamsSent.RootNode.NodeStatus);
            Assert.AreEqual(null, serviceHostParamsSent.RootNode.NodeSubType);
            Assert.AreEqual("Server", serviceHostParamsSent.RootNode.NodeType);
            
            Assert.IsNotNull(serviceHostParamsSent.RootNode.Metadata);
            Assert.AreEqual("FakeClusterName", serviceHostParamsSent.RootNode.Metadata.Name);
            Assert.AreEqual("FakeClusterName", serviceHostParamsSent.RootNode.Metadata.Urn);
            Assert.AreEqual(DataSourceMetadataType.Cluster, serviceHostParamsSent.RootNode.Metadata.MetadataType);
            Assert.AreEqual("FakeClusterName", serviceHostParamsSent.RootNode.Metadata.PrettyName);
            Assert.AreEqual("Cluster", serviceHostParamsSent.RootNode.Metadata.MetadataTypeName);
        }
        
        [Test]
        public async Task HandleExpandRequest_InvalidUri_ReturnsFalse()
        {
            var mockConnectedBindingQueue = new Mock<IConnectedBindingQueue>();
            var mockConnectionService = new Mock<IConnectionService>();
            var mockServiceProvider = new Mock<IMultiServiceProvider>();
            mockServiceProvider
                .Setup(x => x.GetService<IConnectionService>())
                .Returns(mockConnectionService.Object);

            EventType<ExpandResponse> serviceHostEventTypeSent = null;
            ExpandResponse serviceHostExpandResponseSent = null;
            var mockServiceHost = new Mock<IProtocolEndpoint>();
            mockServiceHost
                .Setup(x => x.SendEvent(It.IsAny<EventType<ExpandResponse>>(), It.IsAny<ExpandResponse>()))
                .Callback<EventType<ExpandResponse>, ExpandResponse>((eventType, expandResponse) =>
                {
                    serviceHostEventTypeSent = eventType;
                    serviceHostExpandResponseSent = expandResponse;
                })
                .Returns(Task.CompletedTask);
            
            var objectExplorerService = new ObjectExplorerService(mockConnectedBindingQueue.Object);
            objectExplorerService.SetServiceProvider(mockServiceProvider.Object);
            objectExplorerService.InitializeService(mockServiceHost.Object);

            var expandParams = new ExpandParams
            {
                SessionId = "FakeSessionId",
                NodePath = "FakeNodePath"
            };

            bool? contextResultSent = null;
            var mockRequestContext = new Mock<RequestContext<bool>>();
            mockRequestContext
                .Setup(x => x.SendResult(It.IsAny<bool>()))
                .Callback<bool>(x => contextResultSent = x)
                .Returns(Task.CompletedTask);

            await objectExplorerService.HandleExpandRequest(expandParams, mockRequestContext.Object);
            
            Assert.AreEqual( ExpandCompleteNotification.Type.MethodName, serviceHostEventTypeSent.MethodName);
            Assert.AreEqual( expandParams.NodePath, serviceHostExpandResponseSent.NodePath);
            Assert.AreEqual( expandParams.SessionId, serviceHostExpandResponseSent.SessionId);
            Assert.AreEqual($"Couldn't find session for session: {expandParams.SessionId}", serviceHostExpandResponseSent.ErrorMessage);
            Assert.IsFalse(contextResultSent);
        }
    }
}