//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.ServiceLayer.Cms;
using Microsoft.SqlTools.ServiceLayer.Cms.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Cms
{
    public class CmsServiceTests
    {
        private ConnectParams CreateConnectParams()
        {
            ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem, "master");
            connectParams.OwnerUri = LiveConnectionHelper.GetTestSqlFile();
            connectParams.Connection.DatabaseName = null;
            connectParams.Connection.DatabaseDisplayName = null;

            return connectParams;
        }

        private async Task<ConnectParams> CreateAndConnectWithConnectParams()
        {
            ConnectParams connectParams = CreateConnectParams();
            ConnectionService connService = ConnectionService.Instance;
            await connService.Connect(connectParams);

            return connectParams;
        }

        [Test]
        public async Task TestAddCMS()
        {
            string name = "TestAddCMS" + DateTime.Now.ToString();
            ConnectParams connectParams = CreateConnectParams();

            CreateCentralManagementServerParams connectToCMS = new CreateCentralManagementServerParams
            {
                RegisteredServerName = name,
                RegisteredServerDescription = "My Registered Test Server",
                ConnectParams = connectParams
            };

            // Actual test after preparation start here
            CmsService cmsService = CmsService.Instance;

            // Connect to CMS
            ListRegisteredServersResult result = await cmsService.HandleCreateCentralManagementServerRequest(connectToCMS);
            Assert.NotNull(result.RegisteredServersList);
        }

        [Test]
        public async Task TestAddRemoveRegisteredServer()
        {
            string name = "TestAddRemoveRegisteredServer" + DateTime.Now.ToString();
            ConnectParams connectParams = await CreateAndConnectWithConnectParams();

            AddRegisteredServerParams addRegServerParams = new AddRegisteredServerParams
            {
                RegisteredServerName = name,
                RegisteredServerDescription = "My Registered Test Server",
                ParentOwnerUri = connectParams.OwnerUri,
                RegisteredServerConnectionDetails = new ConnectionDetails { ServerName = name},
                RelativePath = "RegisteredServersStore/ServerGroup[@Name='DatabaseEngineServerGroup']" //Top level
            };

            ListRegisteredServersParams listServersParams = new ListRegisteredServersParams
            {
                ParentOwnerUri = connectParams.OwnerUri,
                RelativePath = "RegisteredServersStore/ServerGroup[@Name='DatabaseEngineServerGroup']"
            };

            RemoveRegisteredServerParams removeRegServerParams = new RemoveRegisteredServerParams
            {
                ParentOwnerUri = connectParams.OwnerUri,
                RegisteredServerName = name,
                RelativePath = string.Format("RegisteredServersStore/ServerGroup[@Name='DatabaseEngineServerGroup']/RegisteredServer[@Name='{0}']", name)
            };

            // Actual test after preparation start here
            CmsService cmsService = CmsService.Instance;

            // Add Reg Server
            bool addResult = await cmsService.HandleAddRegisteredServerRequest(addRegServerParams);
            Assert.True(addResult);

            // List to validate
            ListRegisteredServersResult listResult = await cmsService.HandleListRegisteredServersRequest(listServersParams);
            Assert.NotNull(listResult.RegisteredServersList.Find(p => p.Name.Contains(name)));

            // Clean up 
            bool removeResult = await cmsService.HandleRemoveRegisteredServerRequest(removeRegServerParams);
            Assert.True(removeResult);
        }

        [Test]
        public async Task TestAddRemoveServerGroup()
        {
            string name = "TestAddRemoveServerGroup" + DateTime.Now.ToString();
            ConnectParams connectParams = await CreateAndConnectWithConnectParams();

            AddServerGroupParams addRegServerParams = new AddServerGroupParams
            {
                GroupName = name,
                GroupDescription = "My Registered Test Server Group",
                ParentOwnerUri = connectParams.OwnerUri,
                RelativePath = null,
            };

            ListRegisteredServersParams listServersParams = new ListRegisteredServersParams
            {
                ParentOwnerUri = connectParams.OwnerUri,
                RelativePath = null
            };

            RemoveServerGroupParams removeRegServerParams = new RemoveServerGroupParams
            {
                ParentOwnerUri = connectParams.OwnerUri,
                GroupName = name,
                RelativePath = null
            };
            
            // Actual test start here
            CmsService cmsService = CmsService.Instance;

            bool addResult = await cmsService.HandleAddServerGroupRequest(addRegServerParams);
            Assert.True(addResult);

            ListRegisteredServersResult listResult = await cmsService.HandleListRegisteredServersRequest(listServersParams);
            Assert.NotNull(listResult.RegisteredServerGroups.Find(p => p.Name.Contains(name)));

            bool removeResult = await cmsService.HandleRemoveServerGroupRequest(removeRegServerParams);
            Assert.True(removeResult);
        }

        [Test]
        public async Task TestAddRemoveNestedGroup()
        {
            string name = "TestAddRemoveNestedGroup" + DateTime.Now.ToString();
            ConnectParams connectParams = await CreateAndConnectWithConnectParams();

            AddServerGroupParams addRegServerParams1 = new AddServerGroupParams
            {
                GroupName = name + "_level1",
                GroupDescription = "My Registered Test Server Group Level 1",
                ParentOwnerUri = connectParams.OwnerUri,
                RelativePath = null, // can do with null on level 1
            };

            AddServerGroupParams addRegServerParams2 = new AddServerGroupParams
            {
                GroupName = name + "_level2",
                GroupDescription = "My Registered Test Server Group Level 2",
                ParentOwnerUri = connectParams.OwnerUri,
                RelativePath = string.Format("RegisteredServersStore/ServerGroup[@Name='DatabaseEngineServerGroup']/ServerGroup[@Name='{0}']", name + "_level1") // parent URN
            };

            AddServerGroupParams addRegServerParams3 = new AddServerGroupParams
            {
                GroupName = name + "_level3",
                GroupDescription = "My Registered Test Server Group Level 3",
                ParentOwnerUri = connectParams.OwnerUri,
                RelativePath = string.Format("RegisteredServersStore/ServerGroup[@Name='DatabaseEngineServerGroup']/ServerGroup[@Name='{0}']/ServerGroup[@Name='{1}']", name + "_level1", name + "_level2") // parent URN
            };

            // prepare for multi level server group list

            ListRegisteredServersParams listServersParams = new ListRegisteredServersParams
            {
                ParentOwnerUri = connectParams.OwnerUri,
                RelativePath = string.Format("RegisteredServersStore/ServerGroup[@Name='DatabaseEngineServerGroup']/ServerGroup[@Name='{0}']/ServerGroup[@Name='{1}']", name + "_level1", name + "_level2") // parent URN
            };

            RemoveServerGroupParams removeRegServerParams = new RemoveServerGroupParams
            {
                ParentOwnerUri = connectParams.OwnerUri,
                GroupName = name + "_level3",
                RelativePath = string.Format("RegisteredServersStore/ServerGroup[@Name='DatabaseEngineServerGroup']/ServerGroup[@Name='{0}']/ServerGroup[@Name='{1}']/ServerGroup[@Name='{2}']", name + "_level1", name + "_level2", name + "_level3") // own URN
            };

            RemoveServerGroupParams removeRegServerParamsCleanup = new RemoveServerGroupParams
            {
                ParentOwnerUri = connectParams.OwnerUri,
                GroupName = name + "_level1",
                RelativePath = string.Format("RegisteredServersStore/ServerGroup[@Name='DatabaseEngineServerGroup']/ServerGroup[@Name='{0}']", name + "_level1") // own URN
            };

            // Actual test starts here
            CmsService cmsService = CmsService.Instance;

            // Add three levels
            bool addResult = await cmsService.HandleAddServerGroupRequest(addRegServerParams1);
            Assert.True(addResult);

            addResult = await cmsService.HandleAddServerGroupRequest(addRegServerParams2);
            Assert.True(addResult);

            addResult = await cmsService.HandleAddServerGroupRequest(addRegServerParams3);
            Assert.True(addResult);

            // List Level 2 to find level three
            ListRegisteredServersResult listResult = await cmsService.HandleListRegisteredServersRequest(listServersParams);
            Assert.NotNull(listResult.RegisteredServerGroups.Find(p => p.Name.Contains(name + "_level3")));

            // Remove level 3
            bool removeResult = await cmsService.HandleRemoveServerGroupRequest(removeRegServerParams);
            Assert.True(removeResult);

            // List Level 2 to validate Level 3 removal
            listResult = await cmsService.HandleListRegisteredServersRequest(listServersParams);
            Assert.Null(listResult.RegisteredServerGroups.Find(p => p.Name.Contains(name + "_level3")));

            // Clean up - Remove Level 1
            removeResult = await cmsService.HandleRemoveServerGroupRequest(removeRegServerParamsCleanup);
            Assert.True(removeResult);
        }

    }
}
