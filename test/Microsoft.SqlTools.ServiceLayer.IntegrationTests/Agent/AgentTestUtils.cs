//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Moq;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public static class AgentTestUtils
    {
        internal static AgentJobInfo GetTestJobInfo()
        {
            return new AgentJobInfo()
            {
                Name = "Test Job",
                Owner = "sa",
                Description = "Test job description",
                CurrentExecutionStatus = 1,
                LastRunOutcome = 1,
                CurrentExecutionStep = "Step 1",
                Enabled = false,
                HasTarget = false,
                HasSchedule = false,
                HasStep = false,
                Runnable = true,
                Category = "Cateory 1",
                CategoryId = 1,
                CategoryType = 1,
                LastRun = "today",
                NextRun = "tomorrow",
                JobId = Guid.NewGuid().ToString()
            };
        }

        internal static async Task<AgentAlertInfo[]> GetAgentAlerts(string connectionUri)
        {
            var requestParams = new AgentAlertsParams()
            {
                OwnerUri = connectionUri
            };

            var requestContext = new Mock<RequestContext<AgentAlertsResult>>();

            AgentAlertInfo[] agentAlerts = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<AgentAlertsResult>()))
                 .Callback<AgentAlertsResult>(r => agentAlerts = r.Alerts);

            AgentService service = new AgentService();
            await service.HandleAgentAlertsRequest(requestParams, requestContext.Object);
            return agentAlerts;
        }
    }
}
