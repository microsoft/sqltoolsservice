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
using Moq;
using Xunit;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public class AgentJobTests
    {
        public async Task<AgentJobInfo> CreateAgentJob(TestConnectionResult connectionResult)
        {
            var job = AgentTestUtils.GetTestJobInfo();
            var context = new Mock<RequestContext<CreateAgentJobResult>>();     
            await new AgentService().HandleCreateAgentJobRequest(new CreateAgentJobParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Job = job
            }, context.Object);
            context.VerifyAll();
            return job;
        }

        public async Task<AgentJobInfo> UpdateAgentJob(TestConnectionResult connectionResult, AgentJobInfo job)
        {
            job.Description = "Update job description";
            var context = new Mock<RequestContext<UpdateAgentJobResult>>();     
            await new AgentService().HandleUpdateAgentJobRequest(new UpdateAgentJobParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Job = job
            }, context.Object);
            context.VerifyAll();
            return job;
        }

        /// <summary>
        /// TestHandleCreateAgentJobRequest
        /// </summary>
        [Fact]
        public async Task TestHandleCreateAgentJobRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                await CreateAgentJob(connectionResult);
            }
        }

        /// <summary>
        /// TestHandleUpdateAgentJobRequest
        /// </summary>
        [Fact]
        public async Task TestHandleUpdateAgentJobRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var job = await CreateAgentJob(connectionResult);
                await UpdateAgentJob(connectionResult, job);
            }
        }

        /// <summary>
        /// TestHandleCreateAgentJobStepRequest
        /// </summary>
        //[Fact]
        public async Task TestHandleCreateAgentJobStepRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var createContext = new Mock<RequestContext<CreateAgentJobStepResult>>();
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                await service.HandleCreateAgentJobStepRequest(new CreateAgentJobStepParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Step = new AgentJobStepInfo()
                    {
                        // JobId = Guid.NewGuid().ToString(),
                        Script = @"c:\xplat\test.sql",
                        ScriptName = "Test Script"
                    }
                }, createContext.Object);
                createContext.VerifyAll();
            }
        }
    }
}
