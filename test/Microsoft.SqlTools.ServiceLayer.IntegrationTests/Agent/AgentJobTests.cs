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

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public class AgentJobTests
    {
        /// <summary>
        /// TestHandleCreateAgentJobRequest
        /// </summary>
        [Fact]
        public async Task TestHandleCreateAgentJobRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var createContext = new Mock<RequestContext<CreateAgentJobResult>>();
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                await service.HandleCreateAgentJobRequest(new CreateAgentJobParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                }, createContext.Object);
                createContext.VerifyAll();
            }
        }

        /// <summary>
        /// TestHandleCreateAgentJobStepRequest
        /// </summary>
        [Fact]
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
                        ScriptName = "Test Script",

                        
                    }
                }, createContext.Object);
                createContext.VerifyAll();
            }
        }
    }
}

