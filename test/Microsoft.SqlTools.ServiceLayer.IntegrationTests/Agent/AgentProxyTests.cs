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

                await service.HandleCreateAgentProxyRequest(new CreateAgentProxyParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Proxy = proxy
                }, createContext.Object);
    
                createContext.VerifyAll();
            }
        }
    }
}
