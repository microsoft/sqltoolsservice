//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;
using Moq;
using Moq.Protected;
using Xunit;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.UnitTests.RequestContextMocking;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.ObjectExplorer
{

    public class ObjectExplorerServiceTests : ObjectExplorerTestBase
    {
        private ObjectExplorerService service;
        private Mock<ConnectionService> connectionServiceMock;
        private Mock<IProtocolEndpoint> serviceHostMock;

        string fakeConnectionString =
            "Data Source=server;Initial Catalog=database;Integrated Security=False;User Id=user";

        private static ConnectionDetails details = new ConnectionDetails()
        {
            UserName = "user",
            Password = "password",
            DatabaseName = "msdb",
            ServerName = "serverName"
        };

        ConnectionInfo connectionInfo = new ConnectionInfo(null, null, details);

        ConnectedBindingQueue connectedBindingQueue;
        Mock<SqlConnectionOpener> mockConnectionOpener;

        public ObjectExplorerServiceTests()
        {
            connectionServiceMock = new Mock<ConnectionService>();
            serviceHostMock = new Mock<IProtocolEndpoint>();
            service = CreateOEService(connectionServiceMock.Object);
            connectionServiceMock.Setup(x =>
                x.RegisterConnectedQueue(It.IsAny<string>(), It.IsAny<IConnectedBindingQueue>()));
            service.InitializeService(serviceHostMock.Object);
            ConnectedBindingContext connectedBindingContext = new ConnectedBindingContext();
            connectedBindingContext.ServerConnection = new ServerConnection(new SqlConnection(fakeConnectionString));
            connectedBindingQueue = new ConnectedBindingQueue(false);
            connectedBindingQueue.BindingContextMap.Add(
                $"{details.ServerName}_{details.DatabaseName}_{details.UserName}_NULL", connectedBindingContext);
            connectedBindingQueue.BindingContextTasks.Add(connectedBindingContext, Task.Run(() => null));
            mockConnectionOpener = new Mock<SqlConnectionOpener>();
            connectedBindingQueue.SetConnectionOpener(mockConnectionOpener.Object);
            service.ConnectedBindingQueue = connectedBindingQueue;
        }

        [Fact]
        public async Task CreateSessionRequestErrorsIfConnectionDetailsIsNull()
        {
            object errorResponse = null;
            var contextMock = RequestContextMocks.Create<CreateSessionResponse>(null)
                .AddErrorHandling((errorMessage, errorCode) => errorResponse = errorMessage);

            await service.HandleCreateSessionRequest(null, contextMock.Object);
            VerifyErrorSent(contextMock);
            Assert.True(((string) errorResponse).Contains("ArgumentNullException"));
        }
        
        [Fact]
        public void FindNodeCanExpandParentNodes()
        {
            var mockTreeNode = new Mock<TreeNode>();
            object[] populateChildrenArguments =
                {ItExpr.Is<bool>(x => x == false), ItExpr.IsNull<string>(), new CancellationToken()};
            mockTreeNode.Protected().Setup("PopulateChildren", populateChildrenArguments);
            mockTreeNode.Object.IsAlwaysLeaf = false;

            // If I try to find a child node of the mock tree node with the expand parameter set to true
            ObjectExplorerUtils.FindNode(mockTreeNode.Object, node => false, node => false, true);

            // Then PopulateChildren gets called to expand the tree node
            mockTreeNode.Protected().Verify("PopulateChildren", Times.Once(), populateChildrenArguments);
        }
    }
}
