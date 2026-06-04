//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ServiceHost
{
    public class ServiceHostTests
    {
        [Test]
        public async Task InitializeResultShouldIncludeTheCharactersThatWouldTriggerTheCompletion()
        {
            Hosting.ServiceHost host = Hosting.ServiceHost.Instance;
            InitializeResult result = await host.HandleInitializeRequest(new InitializeRequest());
            Assert.That(result.Capabilities.CompletionProvider.TriggerCharacters.All(
                t => Hosting.ServiceHost.CompletionTriggerCharacters.Contains(t)));
        }
    }
}
