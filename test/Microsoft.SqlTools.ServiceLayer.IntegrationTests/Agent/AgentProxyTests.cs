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
using Microsoft.SqlTools.ServiceLayer.Security;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Moq;
using Xunit;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public class AgentProxyTests
    {
        /// <summary>
        /// TestHandleCreateAgentProxyRequest
        /// </summary>
        [Fact]
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
        [Fact]
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
        [Fact]
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
