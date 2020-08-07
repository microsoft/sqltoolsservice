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
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public class AgentOperatorTests
    {
        /// <summary>
        /// Verify default agent/operators handlers
        /// </summary>
        [Test]
        public async Task TestHandleAgentOperatorsRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                var requestParams = new AgentOperatorsParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                };

                var requestContext = new Mock<RequestContext<AgentOperatorsResult>>();
                AgentService service = new AgentService();
                await service.HandleAgentOperatorsRequest(requestParams, requestContext.Object);
                requestContext.VerifyAll();
            }
        }

        /// <summary>
        /// Verify the default "create agent alert" request handler with valid parameters
        /// </summary>
        [Test]
        public async Task TestHandleCreateAgentOperatorRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var service = new AgentService();
                var operatorInfo = AgentTestUtils.GetTestOperatorInfo();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                await AgentTestUtils.DeleteAgentOperator(service, connectionResult, operatorInfo);

                 // test
                await AgentTestUtils.CreateAgentOperator(service, connectionResult, operatorInfo);

                // cleanup
                await AgentTestUtils.DeleteAgentOperator(service, connectionResult, operatorInfo);
            }
        }

        /// <summary>
        /// TestHandleUpdateAgentOperatorRequest
        /// </summary>
        [Test]
        public async Task TestHandleUpdateAgentOperatorRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var service = new AgentService();
                var operatorInfo = AgentTestUtils.GetTestOperatorInfo();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                await AgentTestUtils.DeleteAgentOperator(service, connectionResult, operatorInfo);
                await AgentTestUtils.CreateAgentOperator(service, connectionResult, operatorInfo);

                // test
                operatorInfo.EmailAddress = "updated@email.com";
                await AgentTestUtils.UpdateAgentOperator(service, connectionResult, operatorInfo);

                // cleanup
                await AgentTestUtils.DeleteAgentOperator(service, connectionResult, operatorInfo);
            }
        }

        /// <summary>
        /// TestHandleDeleteAgentOperatorRequest
        /// </summary>
        [Test]
        public async Task TestHandleDeleteAgentOperatorRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var service = new AgentService();
                var operatorInfo = AgentTestUtils.GetTestOperatorInfo();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                await AgentTestUtils.DeleteAgentOperator(service, connectionResult, operatorInfo);
                await AgentTestUtils.CreateAgentOperator(service, connectionResult, operatorInfo);

                 // test
                await AgentTestUtils.DeleteAgentOperator(service, connectionResult, operatorInfo);
            }
        }
    }
}
