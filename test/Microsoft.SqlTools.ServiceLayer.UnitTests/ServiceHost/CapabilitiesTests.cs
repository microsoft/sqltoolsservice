//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Protocol;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Capabilities
{
    /// <summary>
    /// Test cases for the capabilities discovery messages
    /// </summary>
    public class Capabilities
    {
        [Fact]
        public async Task TestCapabilities()
        {
            Hosting.ServiceHost host = Hosting.ServiceHost.Instance;
            var requestContext = new Mock<RequestContext<CapabilitiesResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<CapabilitiesResult>())).Returns(Task.FromResult(new object()));
            
            await host.HandleCapabilitiesRequest(new CapabilitiesRequest
            {
                HostName = "Test Host", HostVersion = "1.0"
            }, requestContext.Object);

            requestContext.Verify(x => x.SendResult(It.Is<CapabilitiesResult>(
                 i => i.Capabilities.ConnectionProvider.Options != null)));         
        }
    }
}
