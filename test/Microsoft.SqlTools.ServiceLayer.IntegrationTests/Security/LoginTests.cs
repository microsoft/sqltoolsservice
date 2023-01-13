//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Security;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Security
{
    /// <summary>
    /// Tests for the Credential management component
    /// </summary>
    public class LoginTests
    {
        /// <summary>
        /// TestHandleCreateCredentialRequest
        /// </summary>
        [Test]
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
    }
}
