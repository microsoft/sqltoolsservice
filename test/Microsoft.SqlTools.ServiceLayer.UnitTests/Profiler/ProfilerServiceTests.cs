//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Profiler
{
    public class ProfilerServiceTests
    {   
        [Fact]
        public async Task TestStartProfilingRequest()
        {
            var requestContext = new Mock<RequestContext<StartProfilingResult>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<StartProfilingResult>()))
                .Returns(Task.FromResult(0));

            var profilerService = new ProfilerService();

            var requestParams = new StartProfilingParams();
            requestParams.TemplateName = "Standard";

            await profilerService.HandleStartProfilingRequest(requestParams, requestContext.Object);

            requestContext.VerifyAll();
        }    
  
    }
}
