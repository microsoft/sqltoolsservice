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
                await AgentTestUtils.CreateAgentJobStep(service, connectionResult, stepInfo);

                // cleanup
                await AgentTestUtils.DeleteAgentJob(service, connectionResult, job, verify: false);                
            }
        }

        /// <summary>
        /// TestHandleUpdateAgentJobStepRequest
        /// </summary>
        [Fact]
        public async Task TestHandleUpdateAgentJobStepRequest()
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
                await AgentTestUtils.CreateAgentJobStep(service, connectionResult, stepInfo);

                // test
                stepInfo.Script = "SELECT * FROM sys.objects";
                await AgentTestUtils.UpdateAgentJobStep(service, connectionResult, stepInfo);

                // cleanup
                await AgentTestUtils.DeleteAgentJob(service, connectionResult, job, verify: false);
            }
        }

        /// <summary>
        /// TestHandleDeleteAgentJobRequest
        /// </summary>
        [Fact]
        public async Task TestHandleDeleteAgentJobStepRequest()
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
                await AgentTestUtils.CreateAgentJobStep(service, connectionResult, stepInfo);

                 // test
                await AgentTestUtils.DeleteAgentJobStep(service, connectionResult, stepInfo);

                // cleanup
                await AgentTestUtils.DeleteAgentJob(service, connectionResult, job, verify: false);             
            }
        }          
    }
}
