﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.Connection;

using NUnit.Framework;
using Microsoft.SqlTools.ServiceLayer.Test.Common;

using Server = Microsoft.SqlServer.Management.Smo.Server;
using Microsoft.SqlTools.ServiceLayer.ServerConfigurations;

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

        /// <summary>
        /// Test SetMemoryProperties for Sql Server
        /// </summary>
        [Test]
        public async Task SetMemoryProperties()
        {
            var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", serverType: TestServerType.OnPrem);
            using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo))
            {
                var server = new Server(new ServerConnection(sqlConn));
                var serverHandler = new ServerHandler(ConnectionService.Instance);
                var serverConfig = new ServerConfigService();

                var requestParams = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", true, SqlObjectType.Server, "", "");
                var result = (ServerInfo)(await serverHandler.InitializeObjectView(requestParams)).ViewInfo.ObjectInfo;
                ServerInfo serverInfo = new ServerInfo()
                {
                    Name = result.Name,
                    HardwareGeneration = result.HardwareGeneration,
                    Language = result.Language,
                    MemoryInMB = result.MemoryInMB,
                    OperatingSystem = result.OperatingSystem,
                    Platform = result.Platform,
                    Processors = result.Processors,
                    IsClustered = result.IsClustered,
                    IsHadrEnabled = result.IsHadrEnabled,
                    IsPolyBaseInstalled = result.IsPolyBaseInstalled,
                    IsXTPSupported = result.IsXTPSupported,
                    Product = result.Product,
                    ReservedStorageSizeMB = result.ReservedStorageSizeMB,
                    RootDirectory = result.RootDirectory,
                    ServerCollation = result.ServerCollation,
                    ServiceTier = result.ServiceTier,
                    StorageSpaceUsageInMB = result.StorageSpaceUsageInMB,
                    Version = result.Version,
                    MinServerMemory = result.MinServerMemory,
                    MaxServerMemory = result.MaxServerMemory
                };

                // Change memory settings
                serverInfo.MinServerMemory = 10;
                serverInfo.MaxServerMemory = 500;

                Assert.AreNotEqual(result.MinServerMemory, serverInfo.MinServerMemory, "Server property should not be equal after update");
                Assert.AreNotEqual(result.MaxServerMemory, serverInfo.MaxServerMemory, "Server property should not be equal after update");

                await ObjectManagementTestUtils.SaveObject(requestParams, serverInfo);
                result = (ServerInfo)(await serverHandler.InitializeObjectView(requestParams)).ViewInfo.ObjectInfo;
                Assert.IsNotNull(result);
                Assert.AreEqual(result.MinServerMemory, serverInfo.MinServerMemory, "Server property should not be different after update");
                Assert.AreEqual(result.MaxServerMemory, serverInfo.MaxServerMemory, "Server property should not be different after update");
            }
        }
    }
}
