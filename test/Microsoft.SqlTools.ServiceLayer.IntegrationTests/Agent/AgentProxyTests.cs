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
    public class AgentProxyTests
    {
        /// <summary>
        /// Verify the default "update agent alert" request handler with valid parameters
        /// </summary>
        [Fact]
        public async Task TestHandleUpdateAgentProxyRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var createContext = new Mock<RequestContext<CreateAgentProxyResult>>();
                var updateContext = new Mock<RequestContext<UpdateAgentProxyResult>>();
                var deleteContext = new Mock<RequestContext<DeleteAgentProxyResult>>();

                var service = new AgentService();
                var proxy = new AgentProxyInfo()
                {
                    Id = 10,                    
                    AccountName = "Test Proxy 2",
                    CredentialName = "User",
                    Description = "",
                    IsEnabled = true                    
                };

                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                await service.HandleDeleteAgentProxyRequest(new DeleteAgentProxyParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Proxy = proxy
                }, deleteContext.Object);

                deleteContext.VerifyAll();

                await service.HandleCreateAgentProxyRequest(new CreateAgentProxyParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Proxy = proxy
                }, createContext.Object);

                createContext.VerifyAll();

                string originalProxyName = proxy.AccountName;
                proxy.AccountName = proxy.AccountName + " Updated";
                await service.HandleUpdateAgentProxyRequest(new UpdateAgentProxyParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    OriginalProxyName = originalProxyName,
                    Proxy = proxy
                }, updateContext.Object);

                updateContext.VerifyAll();

                await service.HandleDeleteAgentProxyRequest(new DeleteAgentProxyParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Proxy = proxy
                }, deleteContext.Object);

                deleteContext.VerifyAll();
            }
        }   
    }
}
