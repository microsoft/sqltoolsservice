//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.Test.Common;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectManagement
{
    /// <summary>
    /// Tests for the Login management component
    /// </summary>
    public class LoginTests
    {
        /// <summary>
        /// Test the basic Create Login method handler
        /// </summary>
        // [Test]
        public async Task TestHandleCreateLoginRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                // setup, drop credential if exists.
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var testLogin = ObjectManagementTestUtils.GetTestLoginInfo();
                var objUrn = ObjectManagementTestUtils.GetLoginURN(testLogin.Name);
                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn);

                // create and update
                var parametersForCreation = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", true, SqlObjectType.ServerLevelLogin, "", "");
                await ObjectManagementTestUtils.SaveObject(parametersForCreation, testLogin);

                var parametersForUpdate = ObjectManagementTestUtils.GetInitializeViewRequestParams(connectionResult.ConnectionInfo.OwnerUri, "master", false, SqlObjectType.ServerLevelLogin, "", objUrn);
                await ObjectManagementTestUtils.SaveObject(parametersForUpdate, testLogin);

                // cleanup
                await ObjectManagementTestUtils.DropObject(connectionResult.ConnectionInfo.OwnerUri, objUrn);
            }
        }
    }
}
