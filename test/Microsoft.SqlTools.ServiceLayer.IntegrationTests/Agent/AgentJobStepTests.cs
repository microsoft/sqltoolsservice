//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public class AgentJobStepTests
    {
        /// <summary>
        /// TestHandleCreateAgentJobStepRequest
        /// </summary>
        [Fact]
        public async Task TestHandleCreateAgentJobStepRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                 // setup
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var job = AgentTestUtils.GetTestJobInfo();
                await AgentTestUtils.DeleteAgentJob(service, connectionResult, job, verify: false);
                await AgentTestUtils.CreateAgentJob(service, connectionResult, job);

                var stepInfo =  AgentTestUtils.GetTestJobStepInfo(connectionResult, job);

                // test
                var createContext = new Mock<RequestContext<CreateAgentJobStepResult>>();
                await service.HandleCreateAgentJobStepRequest(new CreateAgentJobStepParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Step = stepInfo
                }, createContext.Object);
                createContext.VerifyAll();

                // cleanup
                await AgentTestUtils.DeleteAgentJob(service, connectionResult, job, verify: false);                
            }
        }
    }
}
