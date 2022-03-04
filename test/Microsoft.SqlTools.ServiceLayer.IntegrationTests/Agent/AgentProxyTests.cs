//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Security;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public class AgentProxyTests
    {
        /// <summary>
        /// Verify default agent/proxies handlers
        /// </summary>
        [Test]
        public async Task TestHandleAgentProxiesRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var requestParams = new AgentProxiesParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                };

                var requestContext = new Mock<RequestContext<AgentProxiesResult>>();
                AgentService service = new AgentService();
                await service.HandleAgentProxiesRequest(requestParams, requestContext.Object);
                requestContext.VerifyAll();
            }
        }

        /// <summary>
        /// TestHandleCreateAgentProxyRequest
        /// </summary>
        [Test]
        public async Task TestHandleCreateAgentProxyRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var credential = await SecurityTestUtils.SetupCredential(connectionResult);
                var service = new AgentService();
                var proxy = AgentTestUtils.GetTestProxyInfo();
                await AgentTestUtils.DeleteAgentProxy(service, connectionResult, proxy);                

                // test
                await AgentTestUtils.CreateAgentProxy(service, connectionResult, proxy);

                // cleanup
                await AgentTestUtils.DeleteAgentProxy(service, connectionResult, proxy);
                await SecurityTestUtils.CleanupCredential(connectionResult, credential);
            }
        }

        /// <summary>
        /// Verify the default "update agent alert" request handler with valid parameters
        /// </summary>
        [Test]
        public async Task TestHandleUpdateAgentProxyRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var credential = await SecurityTestUtils.SetupCredential(connectionResult);
                var service = new AgentService();
                var proxy = AgentTestUtils.GetTestProxyInfo();
                await AgentTestUtils.DeleteAgentProxy(service, connectionResult, proxy);    
                await AgentTestUtils.CreateAgentProxy(service, connectionResult, proxy);

                // test
                string originalProxyName = proxy.AccountName;
                proxy.AccountName = proxy.AccountName + " Updated";
                await AgentTestUtils.UpdateAgentProxy(service, connectionResult, originalProxyName, proxy);                

                // cleanup
                await AgentTestUtils.DeleteAgentProxy(service, connectionResult, proxy);
                await SecurityTestUtils.CleanupCredential(connectionResult, credential);
            }
        }

        /// <summary>
        /// TestHandleDeleteAgentProxyRequest
        /// </summary>
        [Test]
        public async Task TestHandleDeleteAgentProxyRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var credential = await SecurityTestUtils.SetupCredential(connectionResult);
                var service = new AgentService();
                var proxy = AgentTestUtils.GetTestProxyInfo();

                // test
                await AgentTestUtils.DeleteAgentProxy(service, connectionResult, proxy);
                await SecurityTestUtils.CleanupCredential(connectionResult, credential); 
            }
        }
    }
}
