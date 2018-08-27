//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests
{
    public class ObjectExplorerTests
    {
        [Fact]
        [CreateTestDb(TestServerType.Azure)]
        public async Task CreateSessionAzure()
        {
            TestServerType serverType = TestServerType.Azure;
            await VerifyCreateSession(serverType);
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task CreateSessionOnPrem()
        {
            TestServerType serverType = TestServerType.OnPrem;
            await VerifyCreateSession(serverType);
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task ExpandDatabasesOnPrem()
        {
            TestServerType serverType = TestServerType.OnPrem;
            await VerifyExpand(serverType, SqlTestDb.MasterDatabaseName);
        }

        [Fact]
        [CreateTestDb(TestServerType.OnPrem)]
        public async Task ExpandOneDatabaseOnPrem()
        {
            TestServerType serverType = TestServerType.OnPrem;
            await VerifyExpand(serverType, Common.PerfTestDatabaseName);
        }

        [Fact]
        [CreateTestDb(TestServerType.Azure)]
        public async Task ExpandDatabasesAzure()
        {
            TestServerType serverType = TestServerType.Azure;
            await VerifyExpand(serverType, SqlTestDb.MasterDatabaseName);
        }

        [Fact]
        [CreateTestDb(TestServerType.Azure)]
        public async Task ExpandOneDatabaseAzure()
        {
            TestServerType serverType = TestServerType.Azure;
            await VerifyExpand(serverType, Common.PerfTestDatabaseName);
        }

        private async Task VerifyCreateSession(TestServerType serverType, [CallerMemberName] string testName = "")
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(serverType, SqlTestDb.MasterDatabaseName);
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    var result = await testService.CalculateRunTime(() => testService.RequestObjectExplorerCreateSession(connectParams.Connection), timer);

                    Assert.NotNull(result);
                    Assert.True(result.Success);
                    Assert.False(string.IsNullOrEmpty(result.SessionId), "Session id cannot be empty");

                    await testService.RequestObjectExplorerCloseSession(new ObjectExplorer.Contracts.CloseSessionParams
                    {
                        SessionId = result.SessionId
                    });
                    await testService.Disconnect(queryTempFile.FilePath);
                }
            }, testName);
        }


        private async Task VerifyExpand(TestServerType serverType, string databaseName, [CallerMemberName] string testName = "")
        {
            await TestServiceDriverProvider.RunTestIterations(async (timer) =>
            {
                ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(serverType, databaseName);
                using (TestServiceDriverProvider testService = new TestServiceDriverProvider())
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    var result = await testService.CalculateRunTime(() => testService.RequestObjectExplorerCreateSession(connectParams.Connection), null);

                    Assert.NotNull(result);
                    Assert.True(result.Success);
                    Assert.False(string.IsNullOrEmpty(result.SessionId), "Session id cannot be empty");

                    await ExpandDatabase(testService, result.SessionId, result.RootNode, timer);

                    await testService.RequestObjectExplorerCloseSession(new ObjectExplorer.Contracts.CloseSessionParams
                    {
                        SessionId = result.SessionId
                    });
                    await testService.Disconnect(queryTempFile.FilePath);
                }
            }, testName);
        }

        private async Task<bool> ExpandDatabase(TestServiceDriverProvider testService, string sessionId, NodeInfo nodeInfo, TestTimer timer)
        {
            if (nodeInfo == null) return false;
            bool foundNode = nodeInfo.NodePath.Contains("Database") || nodeInfo.NodeType == "Database";
            var expandResult = await testService.CalculateRunTime(() => testService.RequestObjectExplorerExpand(new ObjectExplorer.Contracts.ExpandParams
            {
                SessionId = sessionId,
                NodePath = nodeInfo.NodePath
            }, 50000), foundNode ? timer : null);

            Assert.NotNull(expandResult);
            Assert.NotNull(expandResult.Nodes);
            Assert.False(expandResult.Nodes == null, "Nodes are not valid");
            if (!foundNode)
            {
                foreach (var node in expandResult.Nodes)
                {
                    if (await ExpandDatabase(testService, sessionId, node, timer))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                Console.WriteLine("Node Expanded " + nodeInfo.NodePath);
                return true;
            }
        }
    }
}
