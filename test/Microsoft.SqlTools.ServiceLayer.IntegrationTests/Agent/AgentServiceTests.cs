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
    public class AgentServiceTests
    {
        /// <summary>
        /// Verify that a start profiling request starts a profiling session
        /// </summary>
        [Fact]
        public async Task TestHandleAgentJobsRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                var requestParams = new AgentJobsParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                };

                var requestContext = new Mock<RequestContext<AgentJobsResult>>();

                AgentService service = new AgentService();
                await service.HandleAgentJobsRequest(requestParams, requestContext.Object);
                requestContext.VerifyAll();
            }           
        }

        /// <summary>
        /// Verify that a job history request returns the job history
        /// </summary>
        [Fact]
        public async Task TestHandleJobHistoryRequests() 
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                var requestParams = new AgentJobHistoryParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    JobId = "e9420919-b8c2-4a3d-a26c-b7ffde5342cf"
                };

                var requestContext = new Mock<RequestContext<AgentJobHistoryResult>>();

                AgentService service = new AgentService();
                await service.HandleJobHistoryRequest(requestParams, requestContext.Object);
                requestContext.VerifyAll();
            }       
        }
    }
}