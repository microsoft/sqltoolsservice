//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Agent
{
    /// <summary>
    /// Unit tests for ProfilerService
    /// </summary>
    public class AgentServiceTests
    {
        [Fact]
        public async Task TestGetActivityRequest()
        {
            var service = new AgentService();
            await service.HandleGetAgentJobActivityRequest(null, null);

            // profilerService.SessionMonitor.AddSessionListener(sessionListener);
            // profilerService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            // ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            // profilerService.ConnectionServiceInstance.OwnerToConnectionMap.Add(testUri, connectionInfo);
            // profilerService.XEventSessionFactory = new TestXEventSessionFactory();

            // var requestParams = new StartProfilingParams();
            // requestParams.OwnerUri = testUri;
            // requestParams.TemplateName = "Standard";

            // await profilerService.HandleStartProfilingRequest(requestParams, requestContext.Object);

            // // wait a bit for profile sessions to be polled
            // Thread.Sleep(500);

            // requestContext.VerifyAll();

            // Assert.Equal(sessionListener.PreviousSessionId, sessionId);
            // Assert.Equal(sessionListener.PreviousEvents.Count, 1);
        }
    }
}
