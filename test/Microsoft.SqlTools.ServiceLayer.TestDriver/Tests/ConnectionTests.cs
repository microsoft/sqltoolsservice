//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    /// <summary>
    /// Language Service end-to-end integration tests
    /// </summary>
    public class ConnectionTests : TestBase
    {
        /// <summary>
        /// Try to connect with invalid credentials
        /// </summary>
        //[Fact]
        public async Task InvalidConnection()
        {
            try
            {            
                string ownerUri = System.IO.Path.GetTempFileName();
                bool connected = await Connect(ownerUri, ConnectionTestUtils.InvalidConnection);
                Assert.False(connected, "Invalid connection is failed to connect");

                await Disconnect(ownerUri);
            }
            finally
            {
                WaitForExit();
            }
        }

    }
}
