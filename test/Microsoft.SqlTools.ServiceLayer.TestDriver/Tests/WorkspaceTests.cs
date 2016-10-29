//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    /// <summary>
    /// Language Service end-to-end integration tests
    /// </summary>
    public class WorkspaceTests : TestBase
    {
        /// <summary>
        /// Validate workspace lifecycle events
        /// </summary>
        [Fact]
        public async Task InitializeRequestTest()
        {
            try
            {
                InitializeRequest initializeRequest = new InitializeRequest()
                {
                    RootPath = Path.GetTempPath(),
                    Capabilities = new ClientCapabilities()
                };                   
                       
                InitializeResult result = await Driver.SendRequest(InitializeRequest.Type, initializeRequest);
                Assert.NotNull(result);
            }
            finally
            {
                WaitForExit();
            }
        }
    }
}
