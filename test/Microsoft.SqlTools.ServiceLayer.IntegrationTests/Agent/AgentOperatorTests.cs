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
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public class AgentOperatorTests
    {

        /// <summary>
        /// Verify the default "update agent alert" request handler with valid parameters
        /// </summary>
        //[Fact]
        public async Task TestHandleUpdateAgentOperatorRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var createContext = new Mock<RequestContext<CreateAgentOperatorResult>>();

                var service = new AgentService();
                var operatorInfo = new AgentOperatorInfo()
                {
                    Id = 10,
                    Name = "Joe DBA",
                    EmailAddress = "test@aol.com"
                };

                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                await service.HandleCreateAgentOperatorRequest(new CreateAgentOperatorParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Operator = operatorInfo
                }, createContext.Object);

                createContext.VerifyAll();
            }
        }
    }
}
