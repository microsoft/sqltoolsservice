//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public class AgentAlertTests
    {
        /// <summary>
        /// Verify default agent/alerts handlers
        /// </summary>
        //[Fact]
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
            }

        }

        /// <summary>
        /// Verify the default "create agent alert" request handler with valid parameters
        /// </summary>
        // TODO: Fix flaky test. See https://github.com/Microsoft/sqltoolsservice/issues/630
        // [Fact]
        public async Task TestHandleCreateAgentAlertsRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                var requestParams = new CreateAgentAlertParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = new AgentAlertInfo()
                    {
                        JobName = "Test Job Name"
                    }
                };

                var requestContext = new Mock<RequestContext<CreateAgentAlertResult>>();

                AgentService service = new AgentService();
                await service.HandleCreateAgentAlertRequest(requestParams, requestContext.Object);

                // var agentAlerts = await AgentTestUtils.GetAgentAlerts(connectionResult.ConnectionInfo.OwnerUri);
                // if (agentAlerts != null && agentAlerts.Length > 0)
                // {
                //     foreach (var agentAlert in agentAlerts)
                //     {
                //         if (agentAlert.JobName.Equals(alert.JobName))
                //         {
                //             await service.HandleDeleteAgentAlertRequest(new DeleteAgentAlertParams()
                //             {
                //                 OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                //                 Alert = alert
                //             }, deleteContext.Object);
                //         }
                //     }
                // }
            }
        }

        /// <summary>
        /// Verify the default "update agent alert" request handler with valid parameters
        /// </summary>
        [Fact]
        public async Task TestHandleUpdateAgentAlertsRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var createContext = new Mock<RequestContext<CreateAgentAlertResult>>();
                var updateContext = new Mock<RequestContext<UpdateAgentAlertResult>>();
                var deleteContext = new Mock<RequestContext<DeleteAgentAlertResult>>();

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

                await service.HandleUpdateAgentAlertRequest(new UpdateAgentAlertParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = alert
                }, updateContext.Object);

                await service.HandleDeleteAgentAlertRequest(new DeleteAgentAlertParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = alert
                }, deleteContext.Object);

                createContext.VerifyAll();
                updateContext.VerifyAll();
                deleteContext.VerifyAll();
            }
        }
    }
}
