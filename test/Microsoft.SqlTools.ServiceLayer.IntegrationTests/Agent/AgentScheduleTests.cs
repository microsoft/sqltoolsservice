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
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public class AgentScheduleTests
    {
        /// <summary>
        /// HandleAgentSchedulesRequest
        /// </summary>
        [Test]
        public async Task HandleAgentSchedulesRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var service = new AgentService();

                // test
                var context = new Mock<RequestContext<AgentSchedulesResult>>();
                await service.HandleAgentSchedulesRequest(new AgentSchedulesParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                }, context.Object);
                context.VerifyAll();
            }
        }



        /// <summary>
        /// TestHandleCreateAgentScheduleRequest
        /// </summary>
        [Test]
        public async Task TestHandleCreateAgentScheduleRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var service = new AgentService();
                var job = await AgentTestUtils.SetupJob(connectionResult);
                var schedule = AgentTestUtils.GetTestScheduleInfo();
                await AgentTestUtils.DeleteAgentSchedule(service, connectionResult, schedule);

                // test   
                await AgentTestUtils.CreateAgentSchedule(service, connectionResult, schedule);

                // cleanup
                await AgentTestUtils.DeleteAgentSchedule(service, connectionResult, schedule);
                await AgentTestUtils.CleanupJob(connectionResult, job);
            }
        }

        /// <summary>
        /// TestHandleUpdateAgentScheduleRequest
        /// </summary>
        [Test]
        public async Task TestHandleUpdateAgentScheduleRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var service = new AgentService();
                var job = await AgentTestUtils.SetupJob(connectionResult);
                var schedule = AgentTestUtils.GetTestScheduleInfo();
                await AgentTestUtils.DeleteAgentSchedule(service, connectionResult, schedule);
                await AgentTestUtils.CreateAgentSchedule(service, connectionResult, schedule);

                // test
                schedule.IsEnabled = !schedule.IsEnabled;
                await AgentTestUtils.UpdateAgentSchedule(service, connectionResult, schedule.Name, schedule);

                // cleanup
                await AgentTestUtils.DeleteAgentSchedule(service, connectionResult, schedule);
                await AgentTestUtils.CleanupJob(connectionResult, job);
            }
        }

        /// <summary>
        /// TestHandleDeleteAgentScheduleRequest
        /// </summary>
        [Test]
        public async Task TestHandleDeleteAgentScheduleRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var service = new AgentService();
                var job = await AgentTestUtils.SetupJob(connectionResult);
                var schedule = AgentTestUtils.GetTestScheduleInfo();                
                await AgentTestUtils.DeleteAgentSchedule(service, connectionResult, schedule);
                await AgentTestUtils.CreateAgentSchedule(service, connectionResult, schedule);

                // test   
                await AgentTestUtils.DeleteAgentSchedule(service, connectionResult, schedule);

                // cleanup
                await AgentTestUtils.CleanupJob(connectionResult, job);
            }
        }
    }
}
