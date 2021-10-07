using System;
using System.Linq;
using System.Threading;
using Microsoft.Kusto.ServiceLayer.DataSource.Intellisense;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.DataSource.Monitor;
using Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses;
using Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses.Models;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.DataSource
{
    public class MonitorDataSourceTests
    {
        [Test]
        public void GetDatabaseInfo_Returns_ClusterName_And_WorkspaceName()
        {
            var expectedWorkspaceResponse = new WorkspaceResponse
            {
                TableGroups = Array.Empty<TableGroupsModel>(),
                Workspaces = new[]
                {
                    new WorkspacesModel
                    {
                        Name = "FakeWorkspaceName",
                        Id = "FakeWorkspaceId",
                        TableGroups = Array.Empty<string>(),
                        Tables = Array.Empty<string>()
                    }
                }
            };
            
            var mockMonitorClient = new Mock<IMonitorClient>();
            mockMonitorClient.Setup(x => x.WorkspaceId).Returns("FakeWorkspaceId");
            mockMonitorClient.Setup(x => x.LoadMetadata(It.IsAny<bool>())).Returns(expectedWorkspaceResponse);
            
            var mockIntellisenseClient = new Mock<IIntellisenseClient>();

            var datasource = new MonitorDataSource(mockMonitorClient.Object, mockIntellisenseClient.Object);

            var databaseInfo = datasource.GetDatabaseInfo(null, null);
            
            Assert.IsNotNull(databaseInfo);

            Assert.AreEqual("FakeWorkspaceId", databaseInfo.Options["id"]);
            Assert.AreEqual("FakeWorkspaceName", databaseInfo.Options["name"]);
        }

        [Test]
        public void GetDatabases_Returns_ClusterName()
        {
            var expectedWorkspaceResponse = new WorkspaceResponse
            {
                TableGroups = Array.Empty<TableGroupsModel>(),
                Workspaces = new[]
                {
                    new WorkspacesModel
                    {
                        Name = "FakeWorkspaceName",
                        Id = "FakeWorkspaceId",
                        TableGroups = Array.Empty<string>(),
                        Tables = Array.Empty<string>()
                    }
                }
            };
            
            var mockMonitorClient = new Mock<IMonitorClient>();
            mockMonitorClient.Setup(x => x.WorkspaceId).Returns("FakeWorkspaceId");
            mockMonitorClient.Setup(x => x.LoadMetadata(It.IsAny<bool>())).Returns(expectedWorkspaceResponse);
            
            var mockIntellisenseClient = new Mock<IIntellisenseClient>();

            var datasource = new MonitorDataSource(mockMonitorClient.Object, mockIntellisenseClient.Object);

            var response = datasource.GetDatabases(null, false);
            
            Assert.IsNotNull(response);
            Assert.AreEqual(null, response.Databases);
            Assert.IsNotNull(response.DatabaseNames);
            Assert.AreEqual("FakeWorkspaceId", response.DatabaseNames.First());
        }

        [Test]
        public void NotImplementedMethods_ThrowNotImplementedException()
        {
            var expectedWorkspaceResponse = new WorkspaceResponse
            {
                TableGroups = Array.Empty<TableGroupsModel>(),
                Workspaces = new[]
                {
                    new WorkspacesModel
                    {
                        Name = "FakeWorkspaceName",
                        Id = "FakeWorkspaceId",
                        TableGroups = Array.Empty<string>(),
                        Tables = Array.Empty<string>()
                    }
                }
            };
            
            var mockMonitorClient = new Mock<IMonitorClient>();
            mockMonitorClient.Setup(x => x.WorkspaceId).Returns("FakeWorkspaceId");
            mockMonitorClient.Setup(x => x.LoadMetadata(It.IsAny<bool>())).Returns(expectedWorkspaceResponse);
            
            var mockIntellisenseClient = new Mock<IIntellisenseClient>();

            var datasource = new MonitorDataSource(mockMonitorClient.Object, mockIntellisenseClient.Object);

            Assert.Throws<NotImplementedException>(() => datasource.GenerateAlterFunctionScript(It.IsAny<string>()));
            Assert.Throws<NotImplementedException>(() => datasource.GenerateExecuteFunctionScript(It.IsAny<string>()));
            Assert.Throws<NotImplementedException>(() => datasource.ExecuteControlCommandAsync<DataSourceObjectMetadata>(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
        }
    }
}
