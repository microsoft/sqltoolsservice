using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel;
using Microsoft.SqlTools.Extensibility;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.ObjectExplorer
{
    public class ObjectExplorerSessionTests
    {
        [Test]
        public void CreateSession_Returns_Session()
        {
            var connectionParams = new ConnectionCompleteParams
            {
                OwnerUri = "FakeOwnerUri",
                ConnectionSummary = new ConnectionSummary(),
                ServerInfo = new ServerInfo()
            };
            var mockProvider = new Mock<IMultiServiceProvider>();
            var mockDatasource = new Mock<IDataSource>();
            mockDatasource.Setup(x => x.ClusterName).Returns("FakeDatabaseName");

            var session = ObjectExplorerSession.CreateSession(connectionParams, mockProvider.Object, mockDatasource.Object);
            
            Assert.IsNull(session.ConnectionInfo);
            Assert.IsNull(session.ErrorMessage);
            Assert.AreEqual(typeof(ServerNode), session.Root.GetType());
            Assert.AreEqual("FakeOwnerUri", session.Uri);
        }
    }
}