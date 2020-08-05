//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public class AgentAlertTests
    {
        /// <summary>
        /// Verify default agent/alerts handlers
        /// </summary>
        [Test]
        public async Task TestHandleAgentAlertsRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                var requestParams = new AgentAlertsParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                };

                var requestContext = new Mock<RequestContext<AgentAlertsResult>>();
                AgentService service = new AgentService();
                await service.HandleAgentAlertsRequest(requestParams, requestContext.Object);
                requestContext.VerifyAll();
            }
        }

        /// <summary>
        /// Verify the default "create agent alert" request handler with valid parameters
        /// </summary>
        [Test]
        public async Task TestHandleCreateAgentAlertsRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var createContext = new Mock<RequestContext<CreateAgentAlertResult>>();
                var deleteContext = new Mock<RequestContext<ResultStatus>>();

                var service = new AgentService();
                var alert = new AgentAlertInfo()
                {
                    JobName = "test_update_job",
                    AlertType = AlertType.SqlServerEvent,
                    Severity = 1
                };

                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                await service.HandleDeleteAgentAlertRequest(new DeleteAgentAlertParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = alert
                }, deleteContext.Object);

                // test
                await service.HandleCreateAgentAlertRequest(new CreateAgentAlertParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = alert
                }, createContext.Object);
                createContext.VerifyAll();

                // cleanup
                await service.HandleDeleteAgentAlertRequest(new DeleteAgentAlertParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = alert
                }, deleteContext.Object);                
            }
        }

        /// <summary>
        /// Verify the default "update agent alert" request handler with valid parameters
        /// </summary>
        [Test]
        public async Task TestHandleUpdateAgentAlertsRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var createContext = new Mock<RequestContext<CreateAgentAlertResult>>();
                var updateContext = new Mock<RequestContext<UpdateAgentAlertResult>>();
                var deleteContext = new Mock<RequestContext<ResultStatus>>();

                var service = new AgentService();
                var alert = new AgentAlertInfo()
                {
                    JobName = "test_update_job",
                    AlertType = AlertType.SqlServerEvent,
                    Severity = 1
                };

                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                await service.HandleDeleteAgentAlertRequest(new DeleteAgentAlertParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = alert
                }, deleteContext.Object);

                await service.HandleCreateAgentAlertRequest(new CreateAgentAlertParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = alert
                }, createContext.Object);

                // test
                alert.Severity = 2;
                await service.HandleUpdateAgentAlertRequest(new UpdateAgentAlertParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = alert
                }, updateContext.Object);
                updateContext.VerifyAll();

                // cleanup
                await service.HandleDeleteAgentAlertRequest(new DeleteAgentAlertParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = alert
                }, deleteContext.Object);
            }
        }
    }
}
