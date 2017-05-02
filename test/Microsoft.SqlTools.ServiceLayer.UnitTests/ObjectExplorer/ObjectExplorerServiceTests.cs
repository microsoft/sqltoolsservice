//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{

    public class ObjectExplorerServiceTests : ObjectExplorerTestBase
    {
        private ObjectExplorerService service;
        private Mock<ConnectionService> connectionServiceMock;
        private Mock<IProtocolEndpoint> serviceHostMock;
        public ObjectExplorerServiceTests()
        {
            connectionServiceMock = new Mock<ConnectionService>();
            serviceHostMock = new Mock<IProtocolEndpoint>();
            service = CreateOEService(connectionServiceMock.Object);
            service.InitializeService(serviceHostMock.Object);
        }

        [Fact]
        public async Task CreateSessionRequestErrorsIfConnectionDetailsIsNull()
        {
            object errorResponse = null;
            var contextMock = RequestContextMocks.Create<CreateSessionResponse>(null)
                                                 .AddErrorHandling((errorMessage, errorCode) => errorResponse = errorMessage);

            await service.HandleCreateSessionRequest(null, contextMock.Object);
            VerifyErrorSent(contextMock);
            Assert.True(((string)errorResponse).Contains("ArgumentNullException"));
        }
        
        [Fact]
        public async Task CreateSessionRequestReturnsFalseOnConnectionFailure()
        {
            // Given the connection service fails to connect
            ConnectionDetails details = TestObjects.GetTestConnectionDetails();
            ConnectionCompleteParams completeParams = null;
            serviceHostMock.AddEventHandling(ConnectionCompleteNotification.Type, (et, p) => completeParams = p);

            string expectedExceptionText = "Error!!!";
            connectionServiceMock.Setup(c => c.Connect(It.IsAny<ConnectParams>()))
                .Throws(new Exception(expectedExceptionText));

            // when creating a new session
            // then expect the create session request to return false
            await RunAndVerify<CreateSessionResponse>(
                test: (requestContext) => service.HandleCreateSessionRequest(details, requestContext),
                verify: (actual =>
                {
                    Assert.False(actual.Success);
                    Assert.Null(actual.SessionId);
                    Assert.Null(actual.RootNode);
                }));

            // And expect error notification to be sent
            serviceHostMock.Verify(x => x.SendEvent(ConnectionCompleteNotification.Type, It.IsAny<ConnectionCompleteParams>()), Times.Once());
            Assert.NotNull(completeParams);
            Assert.True(completeParams.Messages.Contains(expectedExceptionText));
        }


        [Fact]
        public async Task CreateSessionRequestWithMasterConnectionReturnsServerSuccessAndNodeInfo()
        {
            // Given the connection service fails to connect
            ConnectionDetails details = new ConnectionDetails()
            {
                UserName = "user",
                Password = "password",
                DatabaseName = "master",
                ServerName = "serverName"
            };
            await CreateSessionRequestAndVerifyServerNodeHelper(details);
        }

        [Fact]
        public async Task CreateSessionRequestWithEmptyConnectionReturnsServerSuccessAndNodeInfo()
        {
            // Given the connection service fails to connect
            ConnectionDetails details = new ConnectionDetails()
            {
                UserName = "user",
                Password = "password",
                DatabaseName = "",
                ServerName = "serverName"
            };
            await CreateSessionRequestAndVerifyServerNodeHelper(details);
        }

        [Fact]
        public async Task CreateSessionRequestWithMsdbConnectionReturnsServerSuccessAndNodeInfo()
        {
            // Given the connection service fails to connect
            ConnectionDetails details = new ConnectionDetails()
            {
                UserName = "user",
                Password = "password",
                DatabaseName = "msdb",
                ServerName = "serverName"
            };
            await CreateSessionRequestAndVerifyServerNodeHelper(details);
        }

        [Fact]
        public async Task ExpandNodeGivenValidSessionShouldReturnTheNodeChildren()
        {
            await ExpandAndVerifyServerNodes();
        }

        [Fact]
        public async Task RefreshNodeGivenValidSessionShouldReturnTheNodeChildren()
        {
            await RefreshAndVerifyServerNodes();
        }

        [Fact]
        public async Task ExpandNodeGivenInvalidSessionShouldReturnEmptyList()
        {
            ExpandParams expandParams = new ExpandParams()
            {
                SessionId = "invalid session is",
                NodePath = "Any path"
            };

            // when expanding
            // then expect the nodes are server children 
            await RunAndVerify<ExpandResponse>(
                test: (requestContext) => service.HandleExpandRequest(expandParams, requestContext),
                verify: (actual =>
                {
                    Assert.Equal(actual.SessionId, expandParams.SessionId);
                    Assert.Null(actual.Nodes);
                }));
        }

        [Fact]
        public async Task RefreshNodeGivenInvalidSessionShouldReturnEmptyList()
        {
            RefreshParams expandParams = new RefreshParams()
            {
                SessionId = "invalid session is",
                NodePath = "Any path"
            };

            // when expanding
            // then expect the nodes are server children 
            await RunAndVerify<RefreshResponse>(
                test: (requestContext) => service.HandleRefreshRequest(expandParams, requestContext),
                verify: (actual =>
                {
                    Assert.Equal(actual.SessionId, expandParams.SessionId);
                    Assert.Null(actual.Nodes);
                }));
        }

        [Fact]
        public async Task CloseSessionGivenInvalidSessionShouldReturnEmptyList()
        {
            CloseSessionParams closeSessionParamsparams = new CloseSessionParams()
            {
                SessionId = "invalid session is",
            };

            // when expanding
            // then expect the nodes are server children 
            await RunAndVerify<CloseSessionResponse>(
                test: (requestContext) => service.HandleCloseSessionRequest(closeSessionParamsparams, requestContext),
                verify: (actual =>
                {
                    Assert.Equal(actual.SessionId, closeSessionParamsparams.SessionId);
                    Assert.False(actual.Success);
                }));
        }

        [Fact]
        public async Task CloseSessionGivenValidSessionShouldCloseTheSessionAndDisconnect()
        {
            var session = await CreateSession();
            CloseSessionParams closeSessionParamsparams = new CloseSessionParams()
            {
                SessionId = session.SessionId,
            };

            // when expanding
            // then expect the nodes are server children 
            await RunAndVerify<CloseSessionResponse>(
                test: (requestContext) => service.HandleCloseSessionRequest(closeSessionParamsparams, requestContext),
                verify: (actual =>
                {
                    Assert.Equal(actual.SessionId, closeSessionParamsparams.SessionId);
                    Assert.True(actual.Success);
                    Assert.False(service.SessionIds.Contains(session.SessionId));
                }));

            connectionServiceMock.Verify(c => c.Disconnect(It.IsAny<DisconnectParams>()));
        }

        private async Task<CreateSessionResponse> CreateSession()
        {
            ConnectionDetails details = new ConnectionDetails()
            {
                UserName = "user",
                Password = "password",
                DatabaseName = "msdb",
                ServerName = "serverName"
            };

            serviceHostMock.AddEventHandling(ConnectionCompleteNotification.Type, null);
            CreateSessionResponse result = default(CreateSessionResponse);
            var contextMock = RequestContextMocks.Create<CreateSessionResponse>(r => result = r).AddErrorHandling(null);
            connectionServiceMock.Setup(c => c.Connect(It.IsAny<ConnectParams>()))
                .Returns((ConnectParams connectParams) => Task.FromResult(GetCompleteParamsForConnection(connectParams.OwnerUri, details)));

            ConnectionInfo connectionInfo = new ConnectionInfo(null, null, null);
            string fakeConnectionString = "Data Source=server;Initial Catalog=database;Integrated Security=False;User Id=user";
            connectionInfo.AddConnection("Default", new SqlConnection(fakeConnectionString));
            connectionServiceMock.Setup((c => c.TryFindConnection(It.IsAny<string>(), out connectionInfo))).
                OutCallback((string t, out ConnectionInfo v) => v = connectionInfo)
                .Returns(true);

            connectionServiceMock.Setup(c => c.Disconnect(It.IsAny<DisconnectParams>())).Returns(true);
            await service.HandleCreateSessionRequest(details, contextMock.Object);

            return result;
        }

        private async Task ExpandAndVerifyServerNodes()
        {
            var session = await CreateSession();
            ExpandParams expandParams = new ExpandParams()
            {
                SessionId = session.SessionId,
                NodePath = session.RootNode.NodePath
            };

            // when expanding
            // then expect the nodes are server children 
            await RunAndVerify<ExpandResponse>(
                test: (requestContext) => service.HandleExpandRequest(expandParams, requestContext),
                verify: (actual =>
                {
                    Assert.Equal(actual.SessionId, session.SessionId);
                    Assert.NotNull(actual.SessionId);
                    VerifyServerNodeChildren(actual.Nodes);
                }));
        }

        private async Task RefreshAndVerifyServerNodes()
        {
            var session = await CreateSession();
            RefreshParams expandParams = new RefreshParams()
            {
                SessionId = session.SessionId,
                NodePath = session.RootNode.NodePath
            };

            // when expanding
            // then expect the nodes are server children 
            await RunAndVerify<RefreshResponse>(
                test: (requestContext) => service.HandleRefreshRequest(expandParams, requestContext),
                verify: (actual =>
                {
                    Assert.Equal(actual.SessionId, session.SessionId);
                    Assert.NotNull(actual.SessionId);
                    VerifyServerNodeChildren(actual.Nodes);
                }));
        }

        private async Task CreateSessionRequestAndVerifyServerNodeHelper(ConnectionDetails details)
        {
            serviceHostMock.AddEventHandling(ConnectionCompleteNotification.Type, null);
            
            connectionServiceMock.Setup(c => c.Connect(It.IsAny<ConnectParams>()))
                .Returns((ConnectParams connectParams) => Task.FromResult(GetCompleteParamsForConnection(connectParams.OwnerUri, details)));
            
            // when creating a new session
            // then expect the create session request to return false
            await RunAndVerify<CreateSessionResponse>(
                test: (requestContext) => service.HandleCreateSessionRequest(details, requestContext),
                verify: (actual =>
                {
                    Assert.True(actual.Success);
                    Assert.NotNull(actual.SessionId);
                    VerifyServerNode(actual.RootNode, details);
                }));

            // And expect no error notification to be sent
            serviceHostMock.Verify(x => x.SendEvent(ConnectionCompleteNotification.Type, 
                It.IsAny<ConnectionCompleteParams>()), Times.Never());
        }

        private void VerifyServerNode(NodeInfo serverNode, ConnectionDetails details)
        {
            Assert.NotNull(serverNode);
            Assert.Equal(NodeTypes.Server.ToString(), serverNode.NodeType);
            string[] pathParts = serverNode.NodePath.Split(TreeNode.PathPartSeperator);
            Assert.Equal(1, pathParts.Length);
            Assert.Equal(details.ServerName, pathParts[0]);
            Assert.True(serverNode.Label.Contains(details.ServerName));
            Assert.False(serverNode.IsLeaf);
        }

        private void VerifyServerNodeChildren(NodeInfo[] children)
        {
            Assert.NotNull(children);
            Assert.True(children.Count() == 3);
            Assert.True(children.All((x => x.NodeType == "Folder")));
        }

        private static ConnectionCompleteParams GetCompleteParamsForConnection(string uri, ConnectionDetails details)
        {
            return new ConnectionCompleteParams()
            {
                ConnectionId = Guid.NewGuid().ToString(),
                OwnerUri = uri,
                ConnectionSummary = new ConnectionSummary()
                {
                    ServerName = details.ServerName,
                    DatabaseName = details.DatabaseName,
                    UserName = details.UserName
                },
                ServerInfo = TestObjects.GetTestServerInfo()
            };
        }

        private async Task RunAndVerify<T>(Func<RequestContext<T>, Task> test, Action<T> verify)
        {
            T result = default(T);
            var contextMock = RequestContextMocks.Create<T>(r => result = r).AddErrorHandling(null);
            await test(contextMock.Object);
            VerifyResult(contextMock, verify, result);
        }
        
        private void VerifyResult<T>(Mock<RequestContext<T>> contextMock, Action<T> verify, T actual)
        {
            contextMock.Verify(c => c.SendResult(It.IsAny<T>()), Times.Once);
            contextMock.Verify(c => c.SendError(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            verify(actual);
        }

        private void VerifyErrorSent<T>(Mock<RequestContext<T>> contextMock)
        {
            contextMock.Verify(c => c.SendResult(It.IsAny<T>()), Times.Never);
            contextMock.Verify(c => c.SendError(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

    }
}
