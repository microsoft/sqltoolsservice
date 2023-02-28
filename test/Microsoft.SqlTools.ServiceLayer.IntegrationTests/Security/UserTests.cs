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
    /// Tests for the User management component
    /// </summary>
    public class UserTests
    {       
        /// <summary>
        /// Test the basic Create User method handler
        /// </summary>
        [Test]
        public async Task TestHandleCreateUserWithLoginRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                SecurityService service = new SecurityService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var contextId = System.Guid.NewGuid().ToString();

                var login = await SecurityTestUtils.CreateLogin(service, connectionResult, contextId);

                var user = await SecurityTestUtils.CreateUser(service, connectionResult, contextId, login);

                await SecurityTestUtils.DeleteUser(service, connectionResult, user);

                await SecurityTestUtils.DeleteLogin(service, connectionResult, login);                
            }
        }

        /// <summary>
        /// Test the basic Update User method handler
        /// </summary>
        [Test]
        public async Task TestHandleUpdateUserWithLoginRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                SecurityService service = new SecurityService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var contextId = System.Guid.NewGuid().ToString();

                var login = await SecurityTestUtils.CreateLogin(service, connectionResult, contextId);

                var user = await SecurityTestUtils.CreateUser(service, connectionResult, contextId, login);

                await SecurityTestUtils.UpdateUser(service, connectionResult, contextId, user);

                await SecurityTestUtils.DeleteUser(service, connectionResult, user);

                await SecurityTestUtils.DeleteLogin(service, connectionResult, login);                
            }           
        }
    }
}
