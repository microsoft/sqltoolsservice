//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlServer.Management.Smo;

using NUnit.Framework;
using Microsoft.SqlTools.ServiceLayer.Test.Common;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectManagement
{
    /// <summary>
    /// Tests for the Login management component
    /// </summary>
    public class ServerHandlerTests
    {
        /// <summary>
        /// Test GetServerProperties for Sql Server
        /// </summary>
        [Test]
        public async Task GetServerProperties()
        {
            var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", serverType: TestServerType.OnPrem);
            using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo))
            {
                var server = new Server(new ServerConnection(sqlConn));
                var serverHandler = new ServerHandler(ConnectionService.Instance);

                var requestParams = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", true, SqlObjectType.Server, "", "");
                var result = await serverHandler.InitializeObjectView(requestParams);
                Assert.That(result.ViewInfo.ObjectInfo, Is.Not.Null, $"Expected result should not be empty");
                Assert.That(result.ViewInfo.ObjectInfo.Name, Is.EqualTo(server.Name), $"Server name should not be empty");
                Assert.That(((ServerInfo)result.ViewInfo.ObjectInfo).Language, Is.Not.Null, $"Server language should not be null");
                Assert.That(((ServerInfo)result.ViewInfo.ObjectInfo).MemoryInMB, Is.GreaterThan(0), $"Server physical memory should be greater than 0");
                Assert.That(((ServerInfo)result.ViewInfo.ObjectInfo).Platform, Is.Not.Null, $"Server platform should not be null");
                Assert.That(((ServerInfo)result.ViewInfo.ObjectInfo).OperatingSystem, Is.Not.Null, $"Server operating system should not be null");
                Assert.That(((ServerInfo)result.ViewInfo.ObjectInfo).Processors, Is.Not.Null, $"Server processors should not be null");
                Assert.That(((ServerInfo)result.ViewInfo.ObjectInfo).IsClustered, Is.Not.Null, $"Server isClustered property should not be null");
                Assert.That(((ServerInfo)result.ViewInfo.ObjectInfo).IsHadrEnabled, Is.Not.Null, $"Server isHadrEnabled property should not be null");
                Assert.That(((ServerInfo)result.ViewInfo.ObjectInfo).IsPolyBaseInstalled, Is.Not.Null, $"Server isPolyBaseInstalled property should not be null");
            }
        }
    }
}
