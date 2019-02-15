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
        [Fact]
        private async void TestAddRemoveRegisteredServer()
        {
            string name = "TestAddRemoveRegisteredServer" + DateTime.Now.ToString();

            var requestContext1 = new Mock<RequestContext<bool>>();
            requestContext1.Setup((RequestContext<bool> x) => x.SendResult(It.Is<bool>((result) => result == true))).Returns(Task.FromResult(new object()));

            var requestContext2 = new Mock<RequestContext<ListRegisteredServersResult>>();
            requestContext2.Setup((RequestContext<ListRegisteredServersResult> x) => x.SendResult(It.Is<ListRegisteredServersResult>((listCmsServersResult) => listCmsServersResult.RegisteredServersList.Find(p => p.Name.Contains(name)) != null))).Returns(Task.FromResult(new object()));

            var requestContext3 = new Mock<RequestContext<bool>>();
            requestContext1.Setup((RequestContext<bool> x) => x.SendResult(It.Is<bool>((result) => result == true))).Returns(Task.FromResult(new object()));

            ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem, "master");
            connectParams.OwnerUri = LiveConnectionHelper.GetTestSqlFile();
            connectParams.Connection.DatabaseName = null;
            connectParams.Connection.DatabaseDisplayName = null;

            ConnectionService connService = ConnectionService.Instance;
            await connService.Connect(connectParams);
            
            AddRegisteredServerParams addRegServerParams = new AddRegisteredServerParams
            {
                RegisteredServerName = name,
                RegisterdServerDescription = "My Registered Test Server",
                ParentOwnerUri = connectParams.OwnerUri
            };

            ListRegisteredServerParams listServersParams = new ListRegisteredServerParams
            {
                ParentOwnerUri = connectParams.OwnerUri,
                RelativePath = null
            };

            RemoveRegisteredServerParams removeRegServerParams = new RemoveRegisteredServerParams
            {
                ParentOwnerUri = connectParams.OwnerUri,
                RegisteredServerName = name
            };

            CmsService cmsService = CmsService.Instance;

            await cmsService.HandleAddRegisteredServerRequest(addRegServerParams, requestContext1.Object);
            requestContext1.VerifyAll();

            await cmsService.HandleListRegisteredServersRequest(listServersParams, requestContext2.Object);
            requestContext2.VerifyAll();

            await cmsService.HandleRemoveRegisteredServerRequest(removeRegServerParams, requestContext3.Object);
            requestContext3.VerifyAll();
        }

        [Fact]
        private async void TestAddRemoveServerGroup()
        {
            string name = "TestAddRemoveServerGroup" + DateTime.Now.ToString();

            var requestContext1 = new Mock<RequestContext<bool>>();
            requestContext1.Setup((RequestContext<bool> x) => x.SendResult(It.Is<bool>((result) => result == true))).Returns(Task.FromResult(new object()));

            var requestContext2 = new Mock<RequestContext<ListRegisteredServersResult>>();
            requestContext2.Setup((RequestContext<ListRegisteredServersResult> x) => x.SendResult(It.Is<ListRegisteredServersResult>((listCmsServersResult) => listCmsServersResult.RegisteredServerGroups.Find(p => p.Name.Contains(name)) != null))).Returns(Task.FromResult(new object()));

            var requestContext3 = new Mock<RequestContext<bool>>();
            requestContext1.Setup((RequestContext<bool> x) => x.SendResult(It.Is<bool>((result) => result == true))).Returns(Task.FromResult(new object()));

            ConnectParams connectParams = TestServiceProvider.Instance.ConnectionProfileService.GetConnectionParameters(TestServerType.OnPrem, "master");
            connectParams.OwnerUri = LiveConnectionHelper.GetTestSqlFile();
            connectParams.Connection.DatabaseName = null;
            connectParams.Connection.DatabaseDisplayName = null;

            ConnectionService connService = ConnectionService.Instance;
            await connService.Connect(connectParams);

            AddServerGroupParams addRegServerParams = new AddServerGroupParams
            {
                GroupName = name,
                GroupDescription = "My Registered Test Server Group",
                ParentOwnerUri = connectParams.OwnerUri,
                RelativePath = null,
            };

            ListRegisteredServerParams listServersParams = new ListRegisteredServerParams
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

            CmsService cmsService = CmsService.Instance;

            await cmsService.HandleAddServerGroupRequest(addRegServerParams, requestContext1.Object);
            requestContext1.VerifyAll();

            await cmsService.HandleListRegisteredServersRequest(listServersParams, requestContext2.Object);
            requestContext2.VerifyAll();

            await cmsService.HandleRemoveServerGroupRequest(removeRegServerParams, requestContext3.Object);
            requestContext3.VerifyAll();
        }

    }
}
