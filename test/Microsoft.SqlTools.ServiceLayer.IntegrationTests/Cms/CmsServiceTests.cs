//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Cms;
using Microsoft.SqlTools.ServiceLayer.Cms.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Cms
{
    public class CmsServiceTests
    {
        private async Task<ConnectParams> CreateAndConnectWithConnectParams()
        {
            ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem, "master");
            connectParams.OwnerUri = LiveConnectionHelper.GetTestSqlFile();
            connectParams.Connection.DatabaseName = null;
            connectParams.Connection.DatabaseDisplayName = null;

            ConnectionService connService = ConnectionService.Instance;
            await connService.Connect(connectParams);

            return connectParams;
        }

        [Fact]
        private async void TestAddRemoveRegisteredServer()
        {
            string name = "TestAddRemoveRegisteredServer" + DateTime.Now.ToString();
            ConnectParams connectParams = await CreateAndConnectWithConnectParams();

            // Prepare for Add Reg Server
            var requestContext1 = new Mock<RequestContext<bool>>();
            requestContext1.Setup((RequestContext<bool> x) => x.SendResult(It.Is<bool>((result) => result == true))).Returns(Task.FromResult(new object()));

            AddRegisteredServerParams addRegServerParams = new AddRegisteredServerParams
            {
                RegisteredServerName = name,
                RegisterdServerDescription = "My Registered Test Server",
                ParentOwnerUri = connectParams.OwnerUri,
                RelativePath = "RegisteredServersStore/ServerGroup[@Name='DatabaseEngineServerGroup']" //Top level
            };

            // Prepare for list servers
            var requestContext2 = new Mock<RequestContext<ListRegisteredServersResult>>();
            requestContext2.Setup((RequestContext<ListRegisteredServersResult> x) => x.SendResult(It.Is<ListRegisteredServersResult>((listCmsServersResult) => listCmsServersResult.RegisteredServersList.Find(p => p.Name.Contains(name)) != null))).Returns(Task.FromResult(new object()));

            ListRegisteredServerParams listServersParams = new ListRegisteredServerParams
            {
                ParentOwnerUri = connectParams.OwnerUri,
                RelativePath = "RegisteredServersStore/ServerGroup[@Name='DatabaseEngineServerGroup']"
            };

            // Prepare for remove Server
            var requestContext3 = new Mock<RequestContext<bool>>();
            requestContext1.Setup((RequestContext<bool> x) => x.SendResult(It.Is<bool>((result) => result == true))).Returns(Task.FromResult(new object()));
            
            RemoveRegisteredServerParams removeRegServerParams = new RemoveRegisteredServerParams
            {
                ParentOwnerUri = connectParams.OwnerUri,
                RegisteredServerName = name,
                RelativePath = string.Format("RegisteredServersStore/ServerGroup[@Name='DatabaseEngineServerGroup']/RegisteredServer[@Name='{0}']", name)
            };

            // Actual test after preparation start here
            CmsService cmsService = CmsService.Instance;

            // Add Reg Server
            await cmsService.HandleAddRegisteredServerRequest(addRegServerParams, requestContext1.Object);
            requestContext1.VerifyAll();

            // List to validate
            await cmsService.HandleListRegisteredServersRequest(listServersParams, requestContext2.Object);
            requestContext2.VerifyAll();

            // Clean up 
            await cmsService.HandleRemoveRegisteredServerRequest(removeRegServerParams, requestContext3.Object);
            requestContext3.VerifyAll();
        }

        [Fact]
        private async void TestAddRemoveServerGroup()
        {
            string name = "TestAddRemoveServerGroup" + DateTime.Now.ToString();
            ConnectParams connectParams = await CreateAndConnectWithConnectParams();

            // Prepare for Server group add
            var requestContext1 = new Mock<RequestContext<bool>>();
            requestContext1.Setup((RequestContext<bool> x) => x.SendResult(It.Is<bool>((result) => result == true))).Returns(Task.FromResult(new object()));
            AddServerGroupParams addRegServerParams = new AddServerGroupParams
            {
                GroupName = name,
                GroupDescription = "My Registered Test Server Group",
                ParentOwnerUri = connectParams.OwnerUri,
                RelativePath = null,
            };

            // prepare for Server group list
            var requestContext2 = new Mock<RequestContext<ListRegisteredServersResult>>();
            requestContext2.Setup((RequestContext<ListRegisteredServersResult> x) => x.SendResult(It.Is<ListRegisteredServersResult>((listCmsServersResult) => listCmsServersResult.RegisteredServerGroups.Find(p => p.Name.Contains(name)) != null))).Returns(Task.FromResult(new object()));
            ListRegisteredServerParams listServersParams = new ListRegisteredServerParams
            {
                ParentOwnerUri = connectParams.OwnerUri,
                RelativePath = null
            };

            // prepare for server group remove
            var requestContext3 = new Mock<RequestContext<bool>>();
            requestContext1.Setup((RequestContext<bool> x) => x.SendResult(It.Is<bool>((result) => result == true))).Returns(Task.FromResult(new object()));
            RemoveServerGroupParams removeRegServerParams = new RemoveServerGroupParams
            {
                ParentOwnerUri = connectParams.OwnerUri,
                GroupName = name,
                RelativePath = null
            };
            
            // Actual test start here
            CmsService cmsService = CmsService.Instance;

            await cmsService.HandleAddServerGroupRequest(addRegServerParams, requestContext1.Object);
            requestContext1.VerifyAll();

            await cmsService.HandleListRegisteredServersRequest(listServersParams, requestContext2.Object);
            requestContext2.VerifyAll();

            await cmsService.HandleRemoveServerGroupRequest(removeRegServerParams, requestContext3.Object);
            requestContext3.VerifyAll();
        }

        [Fact]
        private async void TestAddRemoveNestedGroup()
        {
            string name = "TestAddRemoveNestedGroup" + DateTime.Now.ToString();
            ConnectParams connectParams = await CreateAndConnectWithConnectParams();

            // prepare for multi level server group add
            var requestContextAdd = new Mock<RequestContext<bool>>();
            requestContextAdd.Setup((RequestContext<bool> x) => x.SendResult(It.Is<bool>((result) => result == true))).Returns(Task.FromResult(new object()));

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

            var requestContextList1 = new Mock<RequestContext<ListRegisteredServersResult>>();
            requestContextList1.Setup((RequestContext<ListRegisteredServersResult> x) => x.SendResult(It.Is<ListRegisteredServersResult>((listCmsServersResult) => listCmsServersResult.RegisteredServerGroups.Find(p => p.Name.Contains(name + "_level3")) != null))).Returns(Task.FromResult(new object()));

            var requestContextList2 = new Mock<RequestContext<ListRegisteredServersResult>>();
            requestContextList2.Setup((RequestContext<ListRegisteredServersResult> x) => x.SendResult(It.Is<ListRegisteredServersResult>((listCmsServersResult) => listCmsServersResult.RegisteredServerGroups.Find(p => p.Name.Contains(name + "_level3")) == null))).Returns(Task.FromResult(new object()));

            ListRegisteredServerParams listServersParams = new ListRegisteredServerParams
            {
                ParentOwnerUri = connectParams.OwnerUri,
                RelativePath = string.Format("RegisteredServersStore/ServerGroup[@Name='DatabaseEngineServerGroup']/ServerGroup[@Name='{0}']/ServerGroup[@Name='{1}']", name + "_level1", name + "_level2") // parent URN
            };

            // prepare for multi level server group remove at level 3  and then at level 1
            var requestContextRemove = new Mock<RequestContext<bool>>();
            requestContextRemove.Setup((RequestContext<bool> x) => x.SendResult(It.Is<bool>((result) => result == true))).Returns(Task.FromResult(new object()));
            
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
            await cmsService.HandleAddServerGroupRequest(addRegServerParams1, requestContextAdd.Object);
            requestContextAdd.VerifyAll();
            await cmsService.HandleAddServerGroupRequest(addRegServerParams2, requestContextAdd.Object);
            requestContextAdd.VerifyAll();
            await cmsService.HandleAddServerGroupRequest(addRegServerParams3, requestContextAdd.Object);
            requestContextAdd.VerifyAll();

            // List Level 2 to find level three
            await cmsService.HandleListRegisteredServersRequest(listServersParams, requestContextList1.Object);
            requestContextList1.VerifyAll();

            // Remove level 3
            await cmsService.HandleRemoveServerGroupRequest(removeRegServerParams, requestContextRemove.Object);
            requestContextRemove.VerifyAll();

            // List Level 2 to validate Level 3 removal
            await cmsService.HandleListRegisteredServersRequest(listServersParams, requestContextList2.Object);
            requestContextList2.VerifyAll();

            // Clean up - Remove Level 1
            await cmsService.HandleRemoveServerGroupRequest(removeRegServerParamsCleanup, requestContextRemove.Object);
            requestContextRemove.VerifyAll();
        }

    }
}
