//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Capabilities
{
    /// <summary>
    /// Test cases for the capabilities discovery messages
    /// </summary>
    public class Capabilities
    {
        [Test]
        public async Task TestCapabilities()
        {
            Hosting.ServiceHost host = Hosting.ServiceHost.Instance;
            
            CapabilitiesResult result = await host.HandleCapabilitiesRequest(new CapabilitiesRequest
            {
                HostName = "Test Host", HostVersion = "1.0"
            });

            Assert.That(result.Capabilities.ConnectionProvider.Options, Is.Not.Null);
        }
    }
}
