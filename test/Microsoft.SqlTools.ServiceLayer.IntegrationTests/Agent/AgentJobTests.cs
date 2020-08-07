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
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public class AgentJobTests
    {
        /// <summary>
        /// TestHandleCreateAgentJobRequest
        /// </summary>
        [Test]
        public async Task TestHandleCreateAgentJobRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var job = AgentTestUtils.GetTestJobInfo();
                await AgentTestUtils.DeleteAgentJob(service, connectionResult, job, verify: false);

                // test
                await AgentTestUtils.CreateAgentJob(service, connectionResult, job);

                // cleanup
                await AgentTestUtils.DeleteAgentJob(service, connectionResult, job, verify: false);
            }
        }

        /// <summary>
        /// TestHandleUpdateAgentJobRequest
        /// </summary>
        [Test]
        public async Task TestHandleUpdateAgentJobRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var job = AgentTestUtils.GetTestJobInfo();
                await AgentTestUtils.DeleteAgentJob(service, connectionResult, job, verify: false);
                await AgentTestUtils.CreateAgentJob(service, connectionResult, job);

                // test
                await AgentTestUtils.UpdateAgentJob(service, connectionResult, job);

                // cleanup
                await AgentTestUtils.DeleteAgentJob(service, connectionResult, job, verify: false);
            }
        }

        /// <summary>
        /// TestHandleDeleteAgentJobRequest
        /// </summary>
        [Test]
        public async Task TestHandleDeleteAgentJobRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var job = AgentTestUtils.GetTestJobInfo();
                await AgentTestUtils.DeleteAgentJob(service, connectionResult, job, verify: false);
                await AgentTestUtils.CreateAgentJob(service, connectionResult, job);

                // test
                await AgentTestUtils.DeleteAgentJob(service, connectionResult, job, verify: false);
            }
        }

        /// <summary>
        /// TestAgentJobDefaultsRequest
        /// </summary>
        [Test]
        public async Task TestAgentJobDefaultsRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                // test
                var context = new Mock<RequestContext<AgentJobDefaultsResult>>();     
                await service.HandleAgentJobDefaultsRequest(new AgentJobDefaultsParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                }, context.Object);
                context.VerifyAll();
            }
        }
    }
}
