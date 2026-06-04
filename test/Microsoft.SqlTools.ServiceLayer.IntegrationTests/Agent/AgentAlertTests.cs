//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
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

                AgentService service = new AgentService();
                AgentAlertsResult result = await service.HandleAgentAlertsRequest(requestParams);
                Assert.True(result.Success);
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
                });

                // test
                CreateAgentAlertResult result = await service.HandleCreateAgentAlertRequest(new CreateAgentAlertParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = alert
                });
                Assert.True(result.Success);

                // cleanup
                await service.HandleDeleteAgentAlertRequest(new DeleteAgentAlertParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = alert
                });
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
                });

                await service.HandleCreateAgentAlertRequest(new CreateAgentAlertParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = alert
                });

                // test
                alert.Severity = 2;
                UpdateAgentAlertResult result = await service.HandleUpdateAgentAlertRequest(new UpdateAgentAlertParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = alert
                });
                Assert.True(result.Success);

                // cleanup
                await service.HandleDeleteAgentAlertRequest(new DeleteAgentAlertParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Alert = alert
                });
            }
        }
    }
}
