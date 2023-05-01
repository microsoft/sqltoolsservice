//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectManagement
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
        public async Task TestHandleSaveUserWithLoginRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var connectionUri = connectionResult.ConnectionInfo.OwnerUri;
                var login = await ObjectManagementTestUtils.CreateTestLogin(connectionUri);
                var user = await ObjectManagementTestUtils.CreateTestUser(connectionUri, DatabaseUserType.LoginMapped, null, login.Name);
                var userUrn = ObjectManagementTestUtils.GetUserURN(connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName, user.Name);
                var parameters = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionUri, "master", false, SqlObjectType.User, "", userUrn);
                await ObjectManagementTestUtils.SaveObject(parameters, user);

                await ObjectManagementTestUtils.DropObject(connectionUri, userUrn);
                await ObjectManagementTestUtils.DropObject(connectionUri, ObjectManagementTestUtils.GetLoginURN(login.Name));
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
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var connectionUri = connectionResult.ConnectionInfo.OwnerUri;
                var user = await ObjectManagementTestUtils.CreateTestUser(connectionUri, DatabaseUserType.WindowsUser, $"{Environment.MachineName}\\Administrator");
                await ObjectManagementTestUtils.DropObject(connectionUri, ObjectManagementTestUtils.GetUserURN(connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName, user.Name));
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
                string databaseName = "CRM";
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(databaseName, queryTempFile.FilePath);
                var connectionUri = connectionResult.ConnectionInfo.OwnerUri;
                var user = await ObjectManagementTestUtils.CreateTestUser(connectionUri, DatabaseUserType.SqlAuthentication,
                    userName: null,
                    loginName: null,
                    databaseName: connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName);

                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, ObjectManagementTestUtils.GetUserURN(connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName, user.Name));
            }
        }

        /// <summary>
        /// Test the basic Create User method handler
        /// </summary>
        [Test]
        public async Task TestScriptUserWithLogin()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var connectionUri = connectionResult.ConnectionInfo.OwnerUri;
                var login = await ObjectManagementTestUtils.CreateTestLogin(connectionUri);
                var user = await ObjectManagementTestUtils.CreateTestUser(connectionUri, DatabaseUserType.LoginMapped, null, login.Name);
                var userUrn = ObjectManagementTestUtils.GetUserURN(connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName, user.Name);
                var parameters = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionUri, "master", false, SqlObjectType.User, "", userUrn);
                await ObjectManagementTestUtils.ScriptObject(parameters, user);

                await ObjectManagementTestUtils.DropObject(connectionUri, userUrn);
                await ObjectManagementTestUtils.DropObject(connectionUri, ObjectManagementTestUtils.GetLoginURN(login.Name));
            }
        }
    }
}
