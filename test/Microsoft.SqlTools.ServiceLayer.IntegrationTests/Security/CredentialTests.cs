//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Security;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using Xunit;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Security
{
    /// <summary>
    /// Tests for the Credential management component
    /// </summary>
    public class CredentialTests
    {
        /// <summary>
        /// TestHandleCreateCredentialRequest
        /// </summary>
        [Fact]
        public async Task TestHandleCreateCredentialRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var service = new SecurityService();
                var credential = SecurityTestUtils.GetTestCredentialInfo();
                await SecurityTestUtils.DeleteCredential(service, connectionResult, credential);

                // test
                await SecurityTestUtils.CreateCredential(service, connectionResult, credential);

                // cleanup
                await SecurityTestUtils.DeleteCredential(service, connectionResult, credential);
            }
        }

        /// <summary>
        /// TestHandleUpdateCredentialRequest
        /// </summary>
        [Fact]
        public async Task TestHandleUpdateCredentialRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var service = new SecurityService();
                var credential = SecurityTestUtils.GetTestCredentialInfo();
                await SecurityTestUtils.DeleteCredential(service, connectionResult, credential);
                await SecurityTestUtils.CreateCredential(service, connectionResult, credential);

                // test
                await SecurityTestUtils.UpdateCredential(service, connectionResult, credential);

                // cleanup
                await SecurityTestUtils.DeleteCredential(service, connectionResult, credential);
            }
        }

        /// <summary>
        /// TestHandleDeleteCredentialRequest
        /// </summary>
        [Fact]
        public async Task TestHandleDeleteCredentialRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var service = new SecurityService();
                var credential = SecurityTestUtils.GetTestCredentialInfo();
                await SecurityTestUtils.DeleteCredential(service, connectionResult, credential);
                await SecurityTestUtils.CreateCredential(service, connectionResult, credential);

                // test
                await SecurityTestUtils.DeleteCredential(service, connectionResult, credential);
            }
        }
    }
}
