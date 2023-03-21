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
                UserServiceHandlerImpl userService = new UserServiceHandlerImpl();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                var login = await SecurityTestUtils.CreateLogin(service, connectionResult);

                var user = await SecurityTestUtils.CreateUser(userService, connectionResult, login);

                await SecurityTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, SecurityTestUtils.GetUserURN(connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName, user.Name));

                await SecurityTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, SecurityTestUtils.GetLoginURN(login.Name));
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
                UserServiceHandlerImpl userService = new UserServiceHandlerImpl();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                var login = await SecurityTestUtils.CreateLogin(service, connectionResult);

                var user = await SecurityTestUtils.CreateUser(userService, connectionResult, login);

                await SecurityTestUtils.UpdateUser(userService, connectionResult, user);

                await SecurityTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, SecurityTestUtils.GetUserURN(connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName, user.Name));

                await SecurityTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, SecurityTestUtils.GetLoginURN(login.Name));
            }
        }
    }
}
