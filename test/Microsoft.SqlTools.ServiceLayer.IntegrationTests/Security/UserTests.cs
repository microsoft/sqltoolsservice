//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Security;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;
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
                UserServiceHandlerImpl userService = new UserServiceHandlerImpl();
                LoginServiceHandlerImpl loginService = new LoginServiceHandlerImpl();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                var login = await SecurityTestUtils.CreateLogin(loginService, connectionResult);

                var user = await SecurityTestUtils.CreateUser(userService, connectionResult, DatabaseUserType.WithLogin, null, login.Name);

                await SecurityTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, SecurityTestUtils.GetUserURN(connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName, user.Name));

                await SecurityTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, SecurityTestUtils.GetLoginURN(login.Name));
            }
        }

        /// <summary>
        /// Test the basic Create User method handler
        /// </summary>
        // [Test] - Windows-only
        public async Task TestHandleCreateUserWithWindowsGroup()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                UserServiceHandlerImpl userService = new UserServiceHandlerImpl();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
 
                var user = await SecurityTestUtils.CreateUser(
                    userService, 
                    connectionResult, 
                    DatabaseUserType.WithWindowsGroupLogin,
                    $"{Environment.MachineName}\\Administrator");

                await SecurityTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, SecurityTestUtils.GetUserURN(connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName, user.Name));  
            }
        }

        /// <summary>
        /// Test the basic Create User method handler
        /// </summary>
        // [Test] - needs contained DB
        public async Task TestHandleCreateUserWithContainedSqlPassword()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup
                UserServiceHandlerImpl userService = new UserServiceHandlerImpl();
                string databaseName = "CRM";
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(databaseName, queryTempFile.FilePath);

                var user = await SecurityTestUtils.CreateUser(
                    userService,
                    connectionResult,
                    DatabaseUserType.Contained,
                    userName: null,
                    loginName: null,
                    databaseName: connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName);

                await SecurityTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, SecurityTestUtils.GetUserURN(connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName, user.Name));
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
                UserServiceHandlerImpl userService = new UserServiceHandlerImpl();
                LoginServiceHandlerImpl loginService = new LoginServiceHandlerImpl();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                var login = await SecurityTestUtils.CreateLogin(loginService, connectionResult);

                var user = await SecurityTestUtils.CreateUser(userService, connectionResult, DatabaseUserType.WithLogin, null, login.Name);

                await SecurityTestUtils.UpdateUser(userService, connectionResult, user);

                await SecurityTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, SecurityTestUtils.GetUserURN(connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName, user.Name));

                await SecurityTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, SecurityTestUtils.GetLoginURN(login.Name));
            }
        }
    }
}
